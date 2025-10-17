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
            {
                return BadRequest("Event id is required.");
            }

            var evento = _context.Events
                .Include(e => e.Venues)
                .FirstOrDefault(e => e.Id_Evento == id);

            if (evento is null)
            {
                return NotFound();
            }

            return View("~/Views/Checkout/Index.cshtml", evento);
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
    }
}
