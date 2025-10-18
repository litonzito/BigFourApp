using System;
using System.Collections.Generic;
using System.Linq;
using BigFourApp.Models;
using BigFourApp.Models.Event;
using BigFourApp.Models.ViewModels;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigFourApp.Controllers
{
    /// <summary>
    /// Controlador MVC que expone las vistas de selección de asientos y checkout basándose en los asientos almacenados en la base de datos.
    /// </summary>
    public class SeatController : Controller
    {
        private readonly BaseDatos _context;
        private const int SeatsPerRow = 10;
        private const decimal DefaultBasePrice = 85m;

        public SeatController(BaseDatos context)
        {
            _context = context;
        }
        /// <summary>
        /// Vista de selección de asientos para un evento específico.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Event id is required.");
            }

            var evento = _context.Events
                .Include(e => e.Venues)
                .Include(e => e.Asientos)
                .FirstOrDefault(e => e.Id_Evento == id);

            if (evento is null)
            {
                return NotFound();
            }

            var primaryVenue = evento.Venues.FirstOrDefault();
            var orderedSeats = evento.Asientos
                .Where(a => string.Equals(a.EventId, evento.Id_Evento, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Numero)
                .ToList();

            var sectionDisplayName = !string.IsNullOrWhiteSpace(primaryVenue?.Name)
                ? primaryVenue.Name
                : $"{evento.Name} - General";

            var sections = BuildSectionsFromSeats(evento.Id_Evento, sectionDisplayName, orderedSeats);

            var viewModel = new SeatSelectionViewModel
            {
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = primaryVenue?.Name,
                City = primaryVenue?.City,
                State = primaryVenue?.State,
                SeatmapUrl = string.IsNullOrWhiteSpace(evento.SeatmapUrl) ? null : evento.SeatmapUrl,
                Sections = sections
            };

            return View("~/Views/SeatSelection/Seats.cshtml", viewModel);
        }
        /// <summary>
        /// Vista de checkout para un evento específico.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Checkout(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Event id is required.");

            var evento = _context.Events
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == id);

            if (evento is null)
                return NotFound();

            var v = evento.Venues.FirstOrDefault();

            var vm = new SeatSelectionViewModel
            {
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = v?.Name,
                City = v?.City,
                State = v?.State,
                CartItems = new List<CartItemVM>(),
                Subtotal = 0m
            };

            return View("~/Views/Checkout/Cart.cshtml", vm);
        }

        /// <summary>
        /// Construye las secciones y filas basándose en la lista de asientos proporcionada.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="sectionDisplayName"></param>
        /// <param name="seats"></param>
        /// <returns></returns>
        private IReadOnlyList<SectionVM> BuildSectionsFromSeats(string eventId, string sectionDisplayName, List<Asiento> seats)
        {
            var sectionId = $"SEC-{eventId}";

            if (string.IsNullOrWhiteSpace(sectionDisplayName))
            {
                sectionDisplayName = $"{eventId} - General";
            }

            if (seats.Count == 0)
            {
                return new[]
                {
                    new SectionVM
                    {
                        SectionId = sectionId,
                        Name = sectionDisplayName,
                        Rows = Array.Empty<RowVM>()
                    }
                };
            }

            var totalRows = Math.Max(1, (seats.Count + SeatsPerRow - 1) / SeatsPerRow);

            var rows = seats
                .Select((seat, index) => new { seat, index })
                .GroupBy(item => item.index / SeatsPerRow)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var rowNumber = group.Key + 1;
                    var rowId = $"{sectionId}-ROW-{rowNumber:00}";

                    var seatViewModels = group
                        .Select(item => CreateSeatVm(item.seat, sectionId, rowId, rowNumber, totalRows))
                        .ToList();

                    return new RowVM
                    {
                        RowId = rowId,
                        Name = $"Row {rowNumber}",
                        Seats = seatViewModels
                    };
                })
                .ToList();

            return new[]
            {
                new SectionVM
                {
                    SectionId = sectionId,
                    Name = sectionDisplayName,
                    Rows = rows
                }
            };
        }
        /// <summary>
        /// Crea un ViewModel de asiento basándose en el asiento proporcionado y su posición.
        /// </summary>
        private SeatVM CreateSeatVm(Asiento seat, string sectionId, string rowId, int rowNumber, int totalRows)
        {
            return new SeatVM
            {
                SeatId = seat.Id_Asiento.ToString(),
                Label = $"Asiento {seat.Numero}",
                SeatNumber = seat.Numero,
                Price = CalculatePrice(rowNumber, totalRows, DefaultBasePrice),
                IsAvailable = seat.Estado == EstadoAsiento.Disponible,
                State = seat.Estado,
                SectionId = sectionId,
                RowId = rowId
            };
        }
        /// <summary>
        /// Calcula el precio del asiento basándose en su fila y el precio base.
        /// </summary>
        /// <param name="rowNumber"></param>
        /// <param name="totalRows"></param>
        /// <param name="basePrice"></param>
        /// <returns></returns>
        private static decimal CalculatePrice(int rowNumber, int totalRows, decimal basePrice)
        {
            const decimal rowAdjustment = 7.5m;

            var effectiveRows = totalRows == 0 ? 1 : totalRows;
            var multiplier = effectiveRows - rowNumber;
            var price = basePrice + (multiplier * rowAdjustment);

            price = Math.Round(price, 2, MidpointRounding.AwayFromZero);
            return price < 25m ? 25m : price;
        }

        [HttpPost]
        public IActionResult Checkout([FromBody] CheckoutRequest payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.EventId) || payload.SeatIds == null || payload.SeatIds.Count == 0)
                return BadRequest("Datos inválidos.");

            var evento = _context.Events
                .Include(e => e.Venues)
                .Include(e => e.Asientos)
                .FirstOrDefault(e => e.Id_Evento == payload.EventId);
            if (evento is null) return NotFound();

            var ids = new HashSet<int>();
            foreach (var s in payload.SeatIds)
                if (int.TryParse(s, out var n)) ids.Add(n);

            var allSeats = evento.Asientos
                .Where(a => string.Equals(a.EventId, evento.Id_Evento, StringComparison.OrdinalIgnoreCase))
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

            var selected = allSeats.Where(a => ids.Contains(a.Id_Asiento)).ToList();

            var cartItems = selected.Select(s =>
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

            var vm = new SeatSelectionViewModel
            {
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = evento.Venues.FirstOrDefault()?.Name,
                City = evento.Venues.FirstOrDefault()?.City,
                State = evento.Venues.FirstOrDefault()?.State,
                Sections = new List<SectionVM>(),
                CartItems = cartItems,
                Subtotal = cartItems.Sum(x => x.Price)
            };

            return View("~/Views/Checkout/Cart.cshtml", vm);
        }

        public class CheckoutRequest
        {
            public string EventId { get; set; } = "";
            public List<string> SeatIds { get; set; } = new();
        }

    }
}
