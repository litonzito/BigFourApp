using BigFourApp.Models;
using BigFourApp.Models.ViewModels;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BigFourApp.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly BaseDatos _context;
        private const int SeatsPerRow = 10;
        private const decimal DefaultBasePrice = 85m;

        public CheckoutController(BaseDatos context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Payment(string id, [FromQuery] List<string> seatIds)
        {
            if (string.IsNullOrWhiteSpace(id) || seatIds == null || seatIds.Count == 0)
                return RedirectToAction("Index", "Home");

            var evento = _context.Events
                .Include(e => e.Asientos)
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == id);

            if (evento == null) return NotFound();

            var allSeats = evento.Asientos
                .Where(a => string.Equals(a.EventId, id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Numero)
                .ToList();

            var totalRows = Math.Max(1, (allSeats.Count + SeatsPerRow - 1) / SeatsPerRow);
            var rowBySeatId = new Dictionary<int, int>();
            for (int i = 0; i < allSeats.Count; i++)
            {
                var seat = allSeats[i];
                var rowNumber = (i / SeatsPerRow) + 1;
                rowBySeatId[seat.Id_Asiento] = rowNumber;
            }

            var selected = allSeats.Where(s =>
            {
                // seatIds vienen como string; Id_Asiento es int
                return seatIds.Contains(s.Id_Asiento.ToString());
            }).ToList();

            var items = selected.Select(s =>
            {
                var rowNumber = rowBySeatId[s.Id_Asiento];
                var price = CalculatePrice(rowNumber, totalRows, DefaultBasePrice);
                return new CartItemVM
                {
                    SeatId = s.Id_Asiento.ToString(),
                    Label = $"Asiento {s.Numero}",
                    Price = price
                };
            }).ToList();

            var v = evento.Venues.FirstOrDefault();

            var vm = new SeatSelectionViewModel
            {
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = v?.Name,
                City = v?.City,
                State = v?.State,
                CartItems = items,
                Subtotal = items.Sum(x => x.Price)
            };

            return View("Payment", vm);
        }

        private static decimal CalculatePrice(int rowNumber, int totalRows, decimal basePrice)
        {
            const decimal rowAdjustment = 7.5m;
            var effectiveRows = totalRows == 0 ? 1 : totalRows;
            var multiplier = effectiveRows - rowNumber;
            var price = basePrice + (multiplier * rowAdjustment);
            price = Math.Round(price, 2, MidpointRounding.AwayFromZero);
            return price < 25m ? 25m : price;
        }

        [HttpGet]
        public IActionResult ConfirmPurchase()
        {
            return View("Receipt");
        }

        [HttpPost]
        public IActionResult ConfirmPurchase([FromForm] string eventId, [FromForm] List<string> seatIds)
        {
            if (string.IsNullOrWhiteSpace(eventId) || seatIds == null || seatIds.Count == 0)
                return BadRequest("Datos inválidos.");

            // Carga evento y asientos rastreables
            var evento = _context.Events
                .Include(e => e.Asientos)
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == eventId);

            if (evento == null)
                return NotFound();

            // Convierte seatIds a int
            var ids = seatIds
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToHashSet();

            // Filtra los asientos seleccionados
            var selectedSeats = evento.Asientos
                .Where(a => ids.Contains(a.Id_Asiento))
                .ToList();

            // Marca como ocupados (asegura que EF lo rastree)
            foreach (var seat in selectedSeats)
            {
                seat.Estado = EstadoAsiento.Ocupado;
                _context.Asientos.Update(seat); // 🔸 fuerza a EF a rastrear el cambio
            }

            // Guarda en base
            _context.SaveChanges();

            // Prepara modelo para el recibo
            var vm = new SeatSelectionViewModel
            {
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = evento.Venues.FirstOrDefault()?.Name,
                City = evento.Venues.FirstOrDefault()?.City,
                State = evento.Venues.FirstOrDefault()?.State,
                CartItems = selectedSeats.Select(s => new CartItemVM
                {
                    SeatId = s.Id_Asiento.ToString(),
                    Label = $"Asiento {s.Numero}",
                    Price = 0
                }).ToList(),
                Subtotal = 0
            };

            return View("Receipt", vm);
        }


    }
}
