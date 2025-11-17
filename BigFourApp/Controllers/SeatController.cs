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
    public class SeatController : Controller
    {
        private readonly BaseDatos _context;
        private const int DefaultSeatsPerRow = 10;
        private const decimal DefaultBasePrice = 85m;

        public SeatController(BaseDatos context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Event id is required.");
            }

            var evento = _context.Events
                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .Include(e => e.Asientos)
                .FirstOrDefault(e => e.Id_Evento == id);

            if (evento is null || evento.IsCancelled)
            {
                return NotFound();
            }

            var primaryVenue = evento.Venues.FirstOrDefault();
            var orderedSeats = evento.Asientos
                .Where(a => string.Equals(a.EventId, evento.Id_Evento, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Numero)
                .ToList();

            var availableSeats = orderedSeats.Where(a => a.Estado == EstadoAsiento.Disponible).ToList();
            if (!availableSeats.Any())
            {
                var soldOutVm = new SoldOutViewModel
                {
                    EventName = evento.Name,
                    EventDate = evento.Date,
                    VenueName = primaryVenue?.Name,
                    City = primaryVenue?.City,
                    State = primaryVenue?.State
                };

                return View("~/Views/SeatSelection/SoldOut.cshtml", soldOutVm);
            }

            var sections = BuildSectionsFromSeats(evento, primaryVenue, orderedSeats);

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

        [HttpGet]
        public IActionResult Checkout(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Event id is required.");
            }

            var evento = _context.Events
                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .FirstOrDefault(e => e.Id_Evento == id);

            if (evento is null || evento.IsCancelled)
            {
                return NotFound();
            }

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

        private IReadOnlyList<SectionVM> BuildSectionsFromSeats(Evento evento, Venue? venue, List<Asiento> seats)
        {
            var definitions = ResolveSectionDefinitions(evento, venue, seats.Count);
            var lookup = BuildSeatLookup(seats);
            var fallbackQueue = lookup.TryGetValue(string.Empty, out var unassigned)
                ? new Queue<Asiento>(unassigned)
                : new Queue<Asiento>();

            var sections = new List<SectionVM>();

            foreach (var definition in definitions)
            {
                var sectionKey = NormalizeSectionKey(definition.SectionId);
                var sectionSeats = lookup.TryGetValue(sectionKey, out var assignedSeats)
                    ? assignedSeats
                    : TakeFallbackSeats(fallbackQueue, definition.SeatCount);

                var rows = BuildRowsForSection(definition, sectionSeats);

                sections.Add(new SectionVM
                {
                    SectionId = definition.SectionId,
                    Name = definition.DisplayName,
                    BasePrice = definition.BasePrice,
                    SeatsPerRow = definition.SeatsPerRow,
                    Rows = rows
                });
            }

            if (!sections.Any())
            {
                var fallbackDefinition = new SectionDefinition(
                    $"SEC-{evento.Id_Evento}",
                    string.IsNullOrWhiteSpace(venue?.Name) ? $"{evento.Name} General" : venue!.Name,
                    DefaultBasePrice,
                    Math.Max(seats.Count, DefaultSeatsPerRow),
                    DefaultSeatsPerRow);

                sections.Add(new SectionVM
                {
                    SectionId = fallbackDefinition.SectionId,
                    Name = fallbackDefinition.DisplayName,
                    BasePrice = fallbackDefinition.BasePrice,
                    SeatsPerRow = fallbackDefinition.SeatsPerRow,
                    Rows = BuildRowsForSection(fallbackDefinition, seats)
                });
            }

            return sections;
        }

        private IReadOnlyList<RowVM> BuildRowsForSection(SectionDefinition definition, List<Asiento> seats)
        {
            var contexts = CreateRowContexts(definition, seats);
            var rows = new List<RowVM>();

            foreach (var context in contexts)
            {
                var seatViewModels = context.Seats.Select((seat, index) =>
                {
                    var seatLabel = $"{definition.DisplayName} - Asiento {context.RowIndex * context.SeatsPerRow + index + 1}";
                    return CreateSeatVm(seat, definition.SectionId, context.RowId, seatLabel, context.Price);
                }).ToList();

                rows.Add(new RowVM
                {
                    RowId = context.RowId,
                    Name = context.RowName,
                    Seats = seatViewModels
                });
            }

            return rows;
        }

        private SeatVM CreateSeatVm(Asiento seat, string sectionId, string rowId, string label, decimal price)
        {
            return new SeatVM
            {
                SeatId = seat.Id_Asiento.ToString(),
                Label = label,
                SeatNumber = seat.Numero,
                Price = price,
                IsAvailable = seat.Estado == EstadoAsiento.Disponible,
                State = seat.Estado,
                SectionId = sectionId,
                RowId = rowId
            };
        }

        private IReadOnlyList<SectionDefinition> ResolveSectionDefinitions(Evento evento, Venue? venue, int seatCount)
        {
            var definitions = VenueLayout.GetSections(venue);
            if (definitions.Count > 0)
            {
                return definitions;
            }

            var fallbackSeatCount = seatCount > 0 ? seatCount : DefaultSeatsPerRow * 20;
            var fallbackName = string.IsNullOrWhiteSpace(venue?.Name)
                ? $"{evento.Name} General"
                : venue!.Name;

            return new[]
            {
                new SectionDefinition(
                    $"SEC-{evento.Id_Evento}",
                    fallbackName,
                    DefaultBasePrice,
                    fallbackSeatCount,
                    DefaultSeatsPerRow)
            };
        }

        private static Dictionary<string, List<Asiento>> BuildSeatLookup(List<Asiento> seats)
        {
            return seats
                .GroupBy(s => NormalizeSectionKey(s.SectionId))
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Numero).ToList());
        }

        private static List<Asiento> TakeFallbackSeats(Queue<Asiento> queue, int requested)
        {
            var result = new List<Asiento>();
            while (requested-- > 0 && queue.Count > 0)
            {
                result.Add(queue.Dequeue());
            }

            return result;
        }

        private List<RowContext> CreateRowContexts(SectionDefinition definition, List<Asiento> seats)
        {
            var orderedSeats = seats?.OrderBy(s => s.Numero).ToList() ?? new List<Asiento>();
            var seatsPerRow = definition.SeatsPerRow <= 0 ? DefaultSeatsPerRow : definition.SeatsPerRow;
            var totalRows = Math.Max(1, (orderedSeats.Count + seatsPerRow - 1) / seatsPerRow);
            var contexts = new List<RowContext>();

            for (var rowIndex = 0; rowIndex < totalRows; rowIndex++)
            {
                var rowSeats = orderedSeats
                    .Skip(rowIndex * seatsPerRow)
                    .Take(seatsPerRow)
                    .ToList();

                if (!rowSeats.Any())
                {
                    continue;
                }

                var price = CalculatePrice(rowIndex + 1, totalRows, definition.BasePrice);
                contexts.Add(new RowContext
                {
                    RowId = $"{definition.SectionId}-ROW-{rowIndex + 1:00}",
                    RowName = $"Row {rowIndex + 1}",
                    RowIndex = rowIndex,
                    SeatsPerRow = seatsPerRow,
                    Price = price,
                    Seats = rowSeats
                });
            }

            return contexts;
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

        [HttpPost]
        public IActionResult Checkout([FromBody] CheckoutRequest payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.EventId) || payload.SeatIds == null || payload.SeatIds.Count == 0)
            {
                return BadRequest("Datos inválidos.");
            }

            var evento = _context.Events
                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .Include(e => e.Asientos)
                .FirstOrDefault(e => e.Id_Evento == payload.EventId);

            if (evento is null || evento.IsCancelled)
            {
                return NotFound();
            }

            var ids = new HashSet<int>();
            foreach (var s in payload.SeatIds)
            {
                if (int.TryParse(s, out var n))
                {
                    ids.Add(n);
                }
            }

            var allSeats = evento.Asientos
                .Where(a => string.Equals(a.EventId, evento.Id_Evento, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Numero)
                .ToList();

            var selected = allSeats.Where(a => ids.Contains(a.Id_Asiento)).ToList();
            if (!selected.Any())
            {
                return BadRequest("No se seleccionaron asientos válidos.");
            }

            var venue = evento.Venues.FirstOrDefault();
            var definitions = ResolveSectionDefinitions(evento, venue, allSeats.Count);
            var seatLookup = BuildSeatLookup(allSeats);
            var seatAssignments = AssignSeatsToDefinitions(definitions, seatLookup);
            var seatDisplayMap = BuildSeatDisplayMap(definitions, seatAssignments);
            var fallbackDefinition = definitions.First();

            var cartItems = selected.Select(s =>
            {
                if (!seatDisplayMap.TryGetValue(s.Id_Asiento, out var displayInfo))
                {
                    displayInfo = new SeatDisplayInfo(
                        string.IsNullOrWhiteSpace(s.SectionId) ? (venue?.Name ?? fallbackDefinition.DisplayName) : fallbackDefinition.DisplayName,
                        "Row",
                        CalculatePrice(1, 1, fallbackDefinition.BasePrice));
                }

                var sectionLabel = string.IsNullOrWhiteSpace(displayInfo.SectionName)
                    ? (venue?.Name ?? fallbackDefinition.DisplayName)
                    : displayInfo.SectionName;

                var rowLabel = string.IsNullOrWhiteSpace(displayInfo.RowName)
                    ? "Row"
                    : displayInfo.RowName;

                return new CartItemVM
                {
                    SeatId = s.Id_Asiento.ToString(),
                    Label = $"{sectionLabel} - {rowLabel} - Asiento {s.Numero}",
                    Price = displayInfo.Price
                };
            }).ToList();

            var vm = new SeatSelectionViewModel
            {
                EventId = evento.Id_Evento,
                EventName = evento.Name,
                VenueName = venue?.Name,
                City = venue?.City,
                State = venue?.State,
                Sections = new List<SectionVM>(),
                CartItems = cartItems,
                Subtotal = cartItems.Sum(x => x.Price)
            };

            return View("~/Views/Checkout/Cart.cshtml", vm);
        }

        private Dictionary<string, List<Asiento>> AssignSeatsToDefinitions(IReadOnlyList<SectionDefinition> definitions, Dictionary<string, List<Asiento>> seatLookup)
        {
            var assignments = new Dictionary<string, List<Asiento>>();
            var fallbackQueue = seatLookup.TryGetValue(string.Empty, out var unassigned)
                ? new Queue<Asiento>(unassigned)
                : new Queue<Asiento>();

            foreach (var definition in definitions)
            {
                var key = NormalizeSectionKey(definition.SectionId);
                if (seatLookup.TryGetValue(key, out var assigned))
                {
                    assignments[key] = assigned;
                }
                else
                {
                    assignments[key] = TakeFallbackSeats(fallbackQueue, definition.SeatCount);
                }
            }

            return assignments;
        }

        private Dictionary<int, SeatDisplayInfo> BuildSeatDisplayMap(IReadOnlyList<SectionDefinition> definitions, Dictionary<string, List<Asiento>> seatAssignments)
        {
            var map = new Dictionary<int, SeatDisplayInfo>();

            foreach (var definition in definitions)
            {
                var key = NormalizeSectionKey(definition.SectionId);
                seatAssignments.TryGetValue(key, out var seats);
                var contexts = CreateRowContexts(definition, seats ?? new List<Asiento>());

                foreach (var context in contexts)
                {
                    foreach (var seat in context.Seats)
                    {
                        map[seat.Id_Asiento] = new SeatDisplayInfo(definition.DisplayName, context.RowName, context.Price);
                    }
                }
            }

            return map;
        }

        private sealed class RowContext
        {
            public string RowId { get; set; } = string.Empty;
            public string RowName { get; set; } = string.Empty;
            public int RowIndex { get; set; }
            public int SeatsPerRow { get; set; }
            public decimal Price { get; set; }
            public List<Asiento> Seats { get; set; } = new List<Asiento>();
        }

        private sealed record SeatDisplayInfo(string SectionName, string RowName, decimal Price);

        private static string NormalizeSectionKey(string? sectionId) =>
            string.IsNullOrWhiteSpace(sectionId) ? string.Empty : sectionId.Trim();

        public class CheckoutRequest
        {
            public string EventId { get; set; } = string.Empty;
            public List<string> SeatIds { get; set; } = new();
        }
    }
}
