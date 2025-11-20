using BigFourApp.Models;
using BigFourApp.Models.ViewModels;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BigFourApp.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly BaseDatos _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;


        public CheckoutController(BaseDatos context, IEmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _emailService = emailService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Payment(string id, [FromQuery] List<string> seatIds)
        {
            if (string.IsNullOrWhiteSpace(id) || seatIds == null || seatIds.Count == 0)
                return RedirectToAction("Index", "Home");

            //lee el viewmodel mandado de seatController 
            var json = TempData["CheckoutVM"] as string;
            if(string.IsNullOrEmpty(json))
            {
                return RedirectToAction("Index", "Seat", new {id}); 
            }
            var vmFromCart = JsonConvert.DeserializeObject<SeatSelectionViewModel>(json);

            //validacion de que coincide el evento
            if(!string.Equals(vmFromCart?.EventId, id, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Seat", new {id});
            }

            //se toman los asientos seleccionados del viewmodel (form)
            var filteredItems = (vmFromCart?.CartItems ?? new List<CartItemVM>())
                .Where(ci => seatIds.Contains(ci.SeatId))
                .ToList();

            //validacion por si conflicto de asientos
            if (!filteredItems.Any())
            {
                return RedirectToAction("Index", "Seat", new {id});
            }

            vmFromCart.CartItems = filteredItems;
            vmFromCart.Subtotal = filteredItems.Sum(i => i.Price);

            //se guarda el viewmodel en tempdata para pasarlo a confirmpurchase
            TempData["CheckoutVM"] = JsonConvert.SerializeObject(vmFromCart);
            return View("Payment", vmFromCart);
        }

        [HttpGet]
        public IActionResult ConfirmPurchase(ReceiptViewModel? recipt)
        {
            return View("Receipt", recipt);
        }


        [HttpPost]
        public IActionResult ConfirmPurchase(
        [FromForm] string eventId,
        [FromForm] List<string> seatIds,
        [FromForm] string? nombre,
        [FromForm] string? apellido,
        [FromForm] string? metodoPago,
        // Los datos de tarjeta se postean pero aquí no se usan/persisten (solo se validan en UI)
        [FromForm] string? cardNumber,
        [FromForm] string? cardName,
        [FromForm] string? expiryMonth,
        [FromForm] string? expiryYear,
        [FromForm] string? cvv
    )
        {
            if (string.IsNullOrWhiteSpace(eventId) || seatIds == null || seatIds.Count == 0)
                return BadRequest("Datos inválidos.");

            // Carga evento y asientos (solo lectura)
            var evento = _context.Events
                .Include(e => e.Asientos)
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == eventId);

            if (evento == null)
                return NotFound();

            // Filtra seleccionados
            var ids = seatIds
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToHashSet();

            var selectedSeats = evento.Asientos
                .Where(a => ids.Contains(a.Id_Asiento))
                .OrderBy(a => a.Numero)
                .ToList();

            if(!selectedSeats.Any()) return BadRequest("No se encontraron asientos seleccionados.");

            //recupera los precios del carrito con tempdata
            var json = TempData["CheckoutVM"] as string;
            if (string.IsNullOrWhiteSpace(json))
            {
                return BadRequest("No se pudo recuperar la información del carrito.");
            }

            var checkoutVm = JsonConvert.DeserializeObject<SeatSelectionViewModel>(json);

            //validacion de que la informacion coincida
            if (!string.Equals(checkoutVm?.EventId, eventId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("La información del carrito no coincide con el evento.");
            }

            //se toman los asientos confirmados por el usuario, con sus precios
            var items = (checkoutVm.CartItems ?? new List<CartItemVM>())
                .Where(ci => seatIds.Contains(ci.SeatId))
                .ToList();

            if (!items.Any())
            {
                return BadRequest("No se encontraron asientos en el carrito para generar el recibo.");
            }

            // ==========================================
            // PERSISTENCIA
            // =========================================
            // se actualiza el estado de los asientos a ocupado
            foreach (var seat in selectedSeats)
            {
                seat.Estado = EstadoAsiento.Ocupado;
            }

            var userTask = _userManager.GetUserAsync(User);
            userTask.Wait(); // para evitar deadlocks en contextos sincrónicos
            var user = userTask.Result;

            if (user== null)
            {
                return BadRequest("No se pudo obtener el usuario.");
            }

            var venta = new Venta
            {
                Id_Usuario = user.Id,
                Fecha = DateTime.Now,
                MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "No especificado" : metodoPago,
                Total = items.Sum(i => i.Price)
            };

            _context.Ventas.Add(venta);
            _context.SaveChanges();

            //creacion de boletos unicos y su detalle venta para conectar con la venta correspondiente
            var detalles = new List<DetalleVenta>();

            foreach (var item in items)
            {
                var asiento = selectedSeats.FirstOrDefault(a => a.Id_Asiento.ToString() == item.SeatId);
                if (asiento == null) continue;

                var boleto = new Boleto
                {
                    Notificar = true,
                    CodigoUnico = Guid.NewGuid().ToString("N")
                };

                _context.Boletos.Add(boleto);

                var detalleVenta = new DetalleVenta
                {
                    Id_Venta = venta.Id_Venta,
                    Boleto = boleto,
                    Id_Asiento = asiento.Id_Asiento,
                    Cantidad = 1,
                    PrecioUnitario = item.Price
                };
                _context.DetallesVenta.Add(detalleVenta);
            }

            _context.SaveChanges();


            // construccion del receipt viewmodel
            var venue = evento.Venues.FirstOrDefault();

            var vm = new ReceiptViewModel
            {
                nombre = nombre ?? "",
                apellido = apellido ?? "",
                metodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "No especificado" : metodoPago,
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = venue?.Name,
                City = venue?.City,
                State = venue?.State,
                CartItems = items,
                Subtotal = items.Sum(i => i.Price)
            };

            // Se pasan los datos de Payment al recibo
            ViewBag.Nombre = nombre;
            ViewBag.Apellido = apellido;
            ViewBag.MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "No especificado" : metodoPago;

            TempData["Nombre"] = nombre;
            TempData["Apellido"] = apellido;
            TempData["MetodoPago"] = metodoPago;

            TempData["ReceiptVM"] = JsonConvert.SerializeObject(vm);
            return View("Receipt", vm);
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

            if (string.IsNullOrWhiteSpace(eventId) || seatIds == null || seatIds.Count == 0)
                return BadRequest("Datos inválidos.");

            var evento = _context.Events
                .Include(e => e.Asientos)
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == eventId);

            if (evento == null)
                return NotFound();

            // FILTRAR ASIENTOS
            var ids = seatIds
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToHashSet();

            var selectedSeats = evento.Asientos
                .Where(a => ids.Contains(a.Id_Asiento))
                .OrderBy(a => a.Numero)
                .ToList();

            // OBTENCION DE PRECIOS
            //recuperam el receipt viewmodel de tempdata para precios
            var json = TempData["ReceiptVM"] as string;
            if (string.IsNullOrWhiteSpace(json))
            {
                return BadRequest("No se pudo recuperar la información del recibo para enviar el correo.");
            }

            var vmFromReceipt = JsonConvert.DeserializeObject<ReceiptViewModel>(json);

            // Validar evento
            if (!string.Equals(vmFromReceipt?.EventId, eventId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("La información del recibo no coincide con el evento.");
            }

            // Filtrar solo los asientos del POST
            var items = (vmFromReceipt.CartItems ?? new List<CartItemVM>())
                .Where(ci => seatIds.Contains(ci.SeatId))
                .ToList();

            if (!items.Any())
            {
                return BadRequest("No se encontraron asientos para el recibo.");
            }

            decimal subtotal = items.Sum(i => i.Price);

            // Construir HTML sin recalcular
            string htmlSeatList = string.Join("",
                items.Select(i => $"<li>{i.Label}: {i.Price:C}</li>")
            );

            // CONSTRUIR MODELO COMPLETO
            var vm = new ReceiptViewModel
            {
                EventId = eventId,
                EventName = evento.Name,
                VenueName = evento.Venues.FirstOrDefault()?.Name,
                City = evento.Venues.FirstOrDefault()?.City,
                State = evento.Venues.FirstOrDefault()?.State,
                CartItems = items,
                Subtotal = subtotal,
                nombre = nombre ?? vmFromReceipt.nombre ?? "",
                apellido = apellido ?? vmFromReceipt.apellido ?? "",
                metodoPago = string.IsNullOrWhiteSpace(metodoPago)
                    ? (string.IsNullOrWhiteSpace(vmFromReceipt.metodoPago) ? "No especificado" : vmFromReceipt.metodoPago)
                    : metodoPago
            };

            // HTML FINAL DEL CORREO 
            string subject = $"Recibo de compra — {evento.Name}";

            string body = $@"

        <p>Gracias por tu compra, <b>{vm.nombre} {vm.apellido}</b>.</p>
        <p><b>Método de pago:</b> {vm.metodoPago}</p>

        <h3>Asientos adquiridos</h3>
        <ul>{htmlSeatList}</ul>

        <p><b>Total pagado:</b> ${vm.Subtotal:0.00}</p>

        <p>Evento: <b>{vm.EventName}</b></p>
        <p>Lugar: {vm.VenueName}</p>
        <p>Fecha de compra: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
    ";
            string plainBody = Regex.Replace(body, "<.*?>", string.Empty).Trim();//Poder parsear el body de HTML a texto plano para que se lea mejor en la BD

            // GUARDAR NOTIFICACIÓN EN BD
            var notificacion = new Notificacion
            {
                Id_Usuario = user.Id,
                Mensaje = $"Subject:{subject} \n Body:{plainBody}", // puedes cambiar esto por otro código o texto si lo deseas
                Tipo = "Recibo",
                fecha = DateTime.Now
            };
            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            // ENVIAR EMAIL
            await _emailService.SendEmail(user.Email, subject, body);

            TempData["Message"] = "Recibo enviado a tu correo.";

            return View("Receipt", vm);
        }

    }
}
