using BigFourApp.Models;
using BigFourApp.Models.ViewModels;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Text.RegularExpressions;

namespace BigFourApp.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly BaseDatos _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeSettings _stripe;

        public CheckoutController(
            BaseDatos context,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            IOptions<StripeSettings> stripeOptions)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _stripe = stripeOptions.Value;

            StripeConfiguration.ApiKey = _stripe.SecretKey;
        }

        // PAYMENT (Resumen)
        [HttpGet]
        public IActionResult Payment(string id, [FromQuery] List<string> seatIds)
        {
            if (string.IsNullOrWhiteSpace(id) || seatIds == null || seatIds.Count == 0)
                return RedirectToAction("Index", "Home");

            var json = TempData["CheckoutVM"] as string;
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Index", "Seat", new { id });

            TempData.Keep("CheckoutVM");

            var vmFromCart = JsonConvert.DeserializeObject<SeatSelectionViewModel>(json);

            if (!string.Equals(vmFromCart?.EventId, id, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Seat", new { id });

            var filteredItems = vmFromCart.CartItems
                .Where(ci => seatIds.Contains(ci.SeatId))
                .ToList();

            if (!filteredItems.Any())
                return RedirectToAction("Index", "Seat", new { id });

            vmFromCart.CartItems = filteredItems;
            vmFromCart.Subtotal = filteredItems.Sum(i => i.Price);

            TempData["CheckoutVM"] = JsonConvert.SerializeObject(vmFromCart);
            TempData["seatIds"] = JsonConvert.SerializeObject(seatIds);
            TempData["eventId"] = id;

            return View("Payment", vmFromCart);
        }

        // CREATE CHECKOUT SESSION (Stripe)
        [HttpPost]
        public IActionResult CreateCheckoutSession(string eventId, List<string> seatIds, string nombre, string apellido, string metodoPago)
        {
            var json = TempData["CheckoutVM"] as string;
            if (string.IsNullOrWhiteSpace(json))
                return BadRequest("No se pudo recuperar la información del carrito.");

            var vm = JsonConvert.DeserializeObject<SeatSelectionViewModel>(json);

            var items = vm.CartItems.Where(ci => seatIds.Contains(ci.SeatId)).ToList();
            if (!items.Any())
                return BadRequest("No hay asientos para procesar.");

            decimal total = items.Sum(i => i.Price);
            long stripeAmount = (long)(total * 100);

            TempData["Nombre"] = nombre;
            TempData["Apellido"] = apellido;
            TempData["MetodoPago"] = metodoPago;
            TempData["seatIds"] = JsonConvert.SerializeObject(seatIds);
            TempData["eventId"] = eventId;
            TempData["CheckoutVM"] = JsonConvert.SerializeObject(vm);

            TempData.Keep();

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = Url.Action("StripeSuccess", "Checkout", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = Url.Action("StripeCancel", "Checkout", null, Request.Scheme),

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = stripeAmount,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Boletos para evento"
                            }
                        }
                    }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }

        // STRIPE SUCCESS
        [HttpGet]
        public IActionResult StripeSuccess(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return RedirectToAction("Index", "Home");

            var session = new SessionService().Get(session_id);
            var paymentIntent = new PaymentIntentService().Get(session.PaymentIntentId);

            if (paymentIntent.Status != "succeeded")
                return RedirectToAction("StripeCancel");

            var json = TempData["CheckoutVM"] as string;
            if (string.IsNullOrWhiteSpace(json))
                return RedirectToAction("Index", "Home");

            var vm = JsonConvert.DeserializeObject<SeatSelectionViewModel>(json);

            var seatIds = JsonConvert.DeserializeObject<List<string>>(TempData["seatIds"] as string);
            string eventId = TempData["eventId"] as string;

            string nombre = TempData["Nombre"]?.ToString() ?? "";
            string apellido = TempData["Apellido"]?.ToString() ?? "";
            string metodoPago = TempData["MetodoPago"]?.ToString() ?? "Stripe";

            var evento = _context.Events
                .Include(e => e.Asientos)
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == eventId);

            var ids = seatIds.Select(int.Parse).ToHashSet();

            var selectedSeats = evento.Asientos
                .Where(a => ids.Contains(a.Id_Asiento))
                .OrderBy(a => a.Numero)
                .ToList();

            foreach (var s in selectedSeats)
                s.Estado = EstadoAsiento.Ocupado;

            var user = _userManager.GetUserAsync(User).Result;

            var items = vm.CartItems.Where(ci => seatIds.Contains(ci.SeatId)).ToList();

            var venta = new Venta
            {
                Id_Usuario = user.Id,
                Fecha = DateTime.Now,
                MetodoPago = metodoPago,
                Total = items.Sum(i => i.Price)
            };

            _context.Ventas.Add(venta);
            _context.SaveChanges();

            foreach (var item in items)
            {
                var asiento = selectedSeats.First(a => a.Id_Asiento.ToString() == item.SeatId);

                var boleto = new Boleto
                {
                    Notificar = true,
                    CodigoUnico = Guid.NewGuid().ToString("N")
                };
                _context.Boletos.Add(boleto);

                _context.DetallesVenta.Add(new DetalleVenta
                {
                    Id_Venta = venta.Id_Venta,
                    Boleto = boleto,
                    Id_Asiento = asiento.Id_Asiento,
                    Cantidad = 1,
                    PrecioUnitario = item.Price
                });
            }

            _context.SaveChanges();

            var venue = evento.Venues.FirstOrDefault();

            var receipt = new ReceiptViewModel
            {
                nombre = nombre,
                apellido = apellido,
                metodoPago = metodoPago,
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = venue?.Name,
                City = venue?.City,
                State = venue?.State,
                CartItems = items,
                Subtotal = items.Sum(i => i.Price)
            };

            TempData["ReceiptVM"] = JsonConvert.SerializeObject(receipt);

            return View("Receipt", receipt);
        }

        [HttpPost]
        public async Task<IActionResult> SendReceiptEmail(
            string eventId,
            List<string> seatIds,
            string? nombre,
            string? apellido,
            string? metodoPago)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("No se pudo obtener el correo del usuario.");

            var receiptJson = TempData["ReceiptVM"] as string;
            if (string.IsNullOrWhiteSpace(receiptJson))
                return BadRequest("No se pudo recuperar la información del recibo.");

            TempData.Keep("ReceiptVM");

            var vmFromReceipt = JsonConvert.DeserializeObject<ReceiptViewModel>(receiptJson);

            var evento = _context.Events
                .Include(e => e.Asientos)
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == eventId);

            var items = vmFromReceipt.CartItems;

            string subject = $"Recibo de compra — {vmFromReceipt.EventName}";

            string htmlSeatList = string.Join("", items.Select(i => $"<li>{i.Label}: {i.Price:C}</li>"));

            string body = $@"
                <p>Gracias por tu compra, <b>{vmFromReceipt.nombre} {vmFromReceipt.apellido}</b>.</p>
                <p><b>Método de pago:</b> {vmFromReceipt.metodoPago}</p>

                <h3>Asientos adquiridos</h3>
                <ul>{htmlSeatList}</ul>

                <p><b>Total pagado:</b> ${vmFromReceipt.Subtotal:0.00}</p>

                <p>Evento: <b>{vmFromReceipt.EventName}</b></p>
                <p>Lugar: {vmFromReceipt.VenueName}</p>
                <p>Fecha de compra: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
            ";

            string plainBody = Regex.Replace(body, "<.*?>", string.Empty).Trim();

            var notificacion = new Notificacion
            {
                Id_Usuario = user.Id,
                Mensaje = $"Subject:{subject} \n Body:{plainBody}",
                Tipo = "Recibo",
                fecha = DateTime.Now
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            await _emailService.SendEmail(user.Email, subject, body);

            TempData["Message"] = "Recibo enviado a tu correo.";

            return View("Receipt", vmFromReceipt);
        }

        // STRIPE CANCEL
        [HttpGet]
        public IActionResult StripeCancel()
        {
            TempData["SeatError"] = "El pago fue cancelado.";
            return RedirectToAction("Index", "Home");
        }
    }
}
