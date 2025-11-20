using System.Collections.Generic;
using System.Linq;
using BigFourApp.Models;
using BigFourApp.Models.Event;
using BigFourApp.Models.ViewModels;
using BigFourApp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BigFourApp.Data
{
    public static class SeatSeeder
    {
        public static void EnsureSeats(BaseDatos context)
        {
            var events = context.Events
                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .Include(e => e.Asientos)
                .Where(e => !e.IsCancelled)
                .ToList();

            var changed = false;

            foreach (var evento in events)
            {
                if (evento.Asientos.Any())
                {
                    continue;
                }

                changed |= GenerateSeatsForEventInternal(context, evento, overwriteExisting: false);
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }

        public static bool GenerateSeatsForEvent(BaseDatos context, Evento evento, bool overwriteExisting = false, bool saveChanges = false)
        {
            if (evento is null)
            {
                return false;
            }

            context.Entry(evento).Collection(e => e.Venues).Query().Include(v => v.Sections).Load();
            context.Entry(evento).Collection(e => e.Asientos).Load();

            var changed = GenerateSeatsForEventInternal(context, evento, overwriteExisting);

            if (changed && saveChanges)
            {
                context.SaveChanges();
            }

            return changed;
        }

        private static bool GenerateSeatsForEventInternal(BaseDatos context, Evento evento, bool overwriteExisting)
        {
            if (evento.Asientos.Any() && !overwriteExisting)
            {
                return false;
            }
            var venue = evento.Venues.FirstOrDefault();
            var definitions = VenueLayout.GetSections(venue);

            if (definitions.Count == 0)
            {
                definitions = new[]
                {
                    new SectionDefinition($"SEC-{evento.Id_Evento}", $"{evento.Name} General", 85m, 240, 12)
                };
            }

            var existingSeats = evento.Asientos
                .Where(a => !string.IsNullOrWhiteSpace(a.SectionId))
                .GroupBy(a => a.SectionId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            var seatsToAdd = new List<Asiento>();

            foreach (var definition in definitions)
            {
                existingSeats.TryGetValue(definition.SectionId, out var seatsInSection);
                var availableSeats = seatsInSection?.Where(s => s.Estado == EstadoAsiento.Disponible).ToList() ?? new List<Asiento>();
                var totalSeats = seatsInSection?.Count ?? 0;

                if (!overwriteExisting && totalSeats >= definition.SeatCount)
                {
                    continue;
                }

                if (overwriteExisting)
                {
                    var seatsToKeep = seatsInSection?
                        .Where(s => s.Estado != EstadoAsiento.Disponible)
                        .ToList() ?? new List<Asiento>();

                    context.Asientos.RemoveRange(seatsInSection?.Except(seatsToKeep) ?? Enumerable.Empty<Asiento>());

                    totalSeats = seatsToKeep.Count;
                    availableSeats = seatsToKeep.Where(s => s.Estado == EstadoAsiento.Disponible).ToList();
                }

                var seatsNeeded = definition.SeatCount - totalSeats;
                if (seatsNeeded <= 0)
                {
                    continue;
                }

                var nextNumber = seatsInSection?.Any() == true
                    ? seatsInSection.Max(s => s.Numero) + 1
                    : (definitions.ToList().IndexOf(definition) + 1) * 10000 + 1;

                for (var i = 0; i < seatsNeeded; i++)
                {
                    seatsToAdd.Add(new Asiento
                    {
                        EventId = evento.Id_Evento,
                        Numero = nextNumber++,
                        SectionId = definition.SectionId,
                        Estado = EstadoAsiento.Disponible
                    });
                }
            }

            if (!seatsToAdd.Any())
            {
                return false;
            }

            context.Asientos.AddRange(seatsToAdd);
            return true;
        }
    }
}
