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
    /// Controlador MVC que expone las vistas de selección de asientos y checkout; construye la estructura de secciones/filas con ayuda de VenueLayout y genera el modelo para la vista SeatSelection.
    /// </summary>
    public class SeatController : Controller
    {
        private readonly BaseDatos _context;
        private const int SeatsPerRow = 10;

        public SeatController(BaseDatos context)
        {
            _context = context;
        }
        /// <summary>
        /// Expone la vista de selección de asientos para un evento específico.
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
                .OrderBy(a => a.Numero)
                .ToList();

            var sectionDefinitions = VenueLayout.GetSections(primaryVenue).ToList();

            if (sectionDefinitions.Count == 0)
            {
                var displayName = !string.IsNullOrWhiteSpace(primaryVenue?.Name)
                    ? $"{primaryVenue.Name} - General"
                    : $"{evento.Name} Section";

                sectionDefinitions.Add(new SectionDefinition($"SEC-{evento.Id_Evento}", displayName, 85m));
            }

            var sections = BuildSections(sectionDefinitions, orderedSeats);

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
        /// Expone la vista de checkout para un evento específico.
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
        /// Construye las secciones de asientos basándose en las definiciones y los asientos disponibles.
        /// </summary>
        /// <param name="definitions"></param>
        /// <param name="seats"></param>
        /// <returns></returns>
        private IReadOnlyList<SectionVM> BuildSections(IReadOnlyList<SectionDefinition> definitions, List<Asiento> seats)
        {
            if (definitions.Count == 0)
            {
                return Array.Empty<SectionVM>();
            }

            var encodedBuckets = new Dictionary<int, List<SeatAssignment>>();
            var fallbackSeats = new List<Asiento>();
            var encodedFound = false;

            foreach (var seat in seats)
            {
                if (TryDecodeSectionSeat(seat.Numero, out var sectionIndex, out var seatWithinSection) &&
                    sectionIndex >= 0 && sectionIndex < definitions.Count)
                {
                    encodedFound = true;
                    if (!encodedBuckets.TryGetValue(sectionIndex, out var bucket))
                    {
                        bucket = new List<SeatAssignment>();
                        encodedBuckets[sectionIndex] = bucket;
                    }

                    bucket.Add(new SeatAssignment(seat, seatWithinSection));
                }
                else
                {
                    fallbackSeats.Add(seat);
                }
            }

            if (!encodedFound)
            {
                return BuildSectionsDistributed(definitions, seats);
            }

            var fallbackQueue = new Queue<Asiento>(fallbackSeats);
            var sections = new List<SectionVM>(definitions.Count);

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];

                encodedBuckets.TryGetValue(i, out var assignments);
                assignments ??= new List<SeatAssignment>();

                assignments = assignments
                    .OrderBy(a => a.SeatInSection)
                    .ToList();

                if (fallbackQueue.Count > 0)
                {
                    var nextSeatNumber = assignments.Count == 0 ? 1 : assignments[^1].SeatInSection + 1;
                    while (fallbackQueue.Count > 0 && nextSeatNumber <= 999)
                    {
                        var fallbackSeat = fallbackQueue.Dequeue();
                        assignments.Add(new SeatAssignment(fallbackSeat, nextSeatNumber));
                        nextSeatNumber++;
                    }
                }

                var rows = BuildRowsForSection(definition, assignments);

                sections.Add(new SectionVM
                {
                    SectionId = definition.SectionId,
                    Name = definition.DisplayName,
                    Rows = rows
                });
            }

            if (fallbackQueue.Count > 0)
            {
                var remaining = fallbackQueue.ToList();
                var fallbackSections = BuildSectionsDistributed(definitions, remaining);

                for (var i = 0; i < sections.Count; i++)
                {
                    if (!fallbackSections[i].Rows.Any())
                    {
                        continue;
                    }

                    var mergedRows = sections[i].Rows
                        .Concat(fallbackSections[i].Rows)
                        .ToList();

                    sections[i] = new SectionVM
                    {
                        SectionId = sections[i].SectionId,
                        Name = sections[i].Name,
                        Rows = mergedRows
                    };
                }
            }

            return sections;
        }
        /// <summary>
        /// Construye las secciones de asientos distribuyendo los asientos disponibles equitativamente entre las secciones.
        /// </summary>
        /// <param name="definitions"></param>
        /// <param name="seats"></param>
        /// <returns></returns>
        private IReadOnlyList<SectionVM> BuildSectionsDistributed(IReadOnlyList<SectionDefinition> definitions, List<Asiento> seats)
        {
            var sections = new List<SectionVM>(definitions.Count);
            var seatIndex = 0;

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];

                var seatsRemaining = seats.Count - seatIndex;
                var sectionsRemaining = definitions.Count - i;
                var targetCount = sectionsRemaining == 0 ? 0 : seatsRemaining / sectionsRemaining;

                if (sectionsRemaining > 0 && seatsRemaining % sectionsRemaining > 0)
                {
                    targetCount += 1;
                }

                var sectionSeats = targetCount <= 0
                    ? new List<Asiento>()
                    : seats.Skip(seatIndex).Take(targetCount).ToList();

                seatIndex += sectionSeats.Count;

                var assignments = sectionSeats
                    .Select((seat, offset) => new SeatAssignment(seat, offset + 1))
                    .ToList();

                var rows = BuildRowsForSection(definition, assignments);

                sections.Add(new SectionVM
                {
                    SectionId = definition.SectionId,
                    Name = definition.DisplayName,
                    Rows = rows
                });
            }

            return sections;
        }
        /// <summary>
        /// Construye las filas de una sección basándose en las asignaciones de asientos proporcionadas.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="seats"></param>
        /// <returns></returns>
        private IReadOnlyList<RowVM> BuildRowsForSection(SectionDefinition definition, IReadOnlyList<SeatAssignment> seats)
        {
            if (seats.Count == 0)
            {
                return Array.Empty<RowVM>();
            }

            var orderedSeats = seats
                .OrderBy(s => s.SeatInSection)
                .ToList();

            var totalRows = Math.Max(1, (orderedSeats.Count + SeatsPerRow - 1) / SeatsPerRow);

            var rows = orderedSeats
                .GroupBy(s => SeatsPerRow > 0 ? (s.SeatInSection - 1) / SeatsPerRow : 0)
                .OrderBy(g => g.Key)
                .Select(group =>
                {
                    var rowNumber = group.Key + 1;
                    var rowId = $"{definition.SectionId}-ROW-{rowNumber:00}";

                    var seatViewModels = group
                        .OrderBy(s => s.SeatInSection)
                        .Select(s => CreateSeatVm(s.Seat, definition, rowId, rowNumber, totalRows, s.SeatInSection))
                        .ToList();

                    return new RowVM
                    {
                        RowId = rowId,
                        Name = $"Row {rowNumber}",
                        Seats = seatViewModels
                    };
                })
                .ToList();

            return rows;
        }
        /// <summary>
        /// Crea una vista de modelo de asiento basándose en los parámetros proporcionados.
        /// </summary>
        /// <param name="seat"></param>
        /// <param name="definition"></param>
        /// <param name="rowId"></param>
        /// <param name="rowNumber"></param>
        /// <param name="totalRows"></param>
        /// <param name="seatInSection"></param>
        /// <returns></returns>
        private SeatVM CreateSeatVm(Asiento seat, SectionDefinition definition, string rowId, int rowNumber, int totalRows, int seatInSection)
        {
            return new SeatVM
            {
                SeatId = seat.Id_Asiento.ToString(),
                Label = $"{definition.DisplayName} Seat {seatInSection:000}",
                SeatNumber = seatInSection,
                Price = CalculatePrice(rowNumber, totalRows, definition.BasePrice),
                IsAvailable = seat.Estado == EstadoAsiento.Disponible,
                State = seat.Estado,
                SectionId = definition.SectionId,
                RowId = rowId
            };
        }
        /// <summary>
        /// Intenta decodificar el número de asiento en índice de sección y asiento dentro de la sección.
        /// </summary>
        /// <param name="numero"></param>
        /// <param name="sectionIndex"></param>
        /// <param name="seatWithinSection"></param>
        /// <returns></returns>
        private static bool TryDecodeSectionSeat(int numero, out int sectionIndex, out int seatWithinSection)
        {
            sectionIndex = -1;
            seatWithinSection = 0;

            if (numero < 1000)
            {
                return false;
            }

            sectionIndex = (numero / 1000) - 1;
            seatWithinSection = numero % 1000;

            if (seatWithinSection == 0)
            {
                seatWithinSection = 1000;
            }

            return sectionIndex >= 0;
        }
        /// <summary>
        /// Representa una asignación de asiento con el asiento y su posición dentro de la sección.
        /// </summary>
        /// <param name="Seat"></param>
        /// <param name="SeatInSection"></param>
        private sealed record SeatAssignment(Asiento Seat, int SeatInSection);

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
