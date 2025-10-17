using System;
using System.Collections.Generic;
using System.Linq;
using BigFourApp.Models;
using BigFourApp.Models.Event;
using BigFourApp.Models.ViewModels;
using BigFourApp.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BigFourApp.Data
{
    /// <summary>
    /// Seeder reutilizable que asegura la creación de asientos para cada evento, generando numeración y estado inicial cuando faltan registros en la tabla Asientos.
    /// </summary>
    public static class SeatSeeder
    {
        /// <summary>
        /// Asegura que todos los eventos tengan asientos generados.
        /// </summary>
        public static void EnsureSeats(BaseDatos context)
        {
            var events = context.Events
                .Include(e => e.Venues)
                .Include(e => e.Asientos)
                .ToList();

            foreach (var evento in events)
            {
                if (evento.Asientos.Any())
                {
                    continue;
                }

                var venue = evento.Venues.FirstOrDefault();
                var definitions = VenueLayout.GetSections(venue);

                if (definitions.Count == 0)
                {
                    definitions = new[] { new SectionDefinition($"SEC-{evento.Id_Evento}", $"{evento.Name} Section", 85m) };
                }

                var newSeats = new List<Asiento>();

                for (var sectionIndex = 0; sectionIndex < definitions.Count; sectionIndex++)
                {
                    var definition = definitions[sectionIndex];
                    var seatCount = EstimateSeatCount(definition);
                    var sectionCode = sectionIndex + 1;

                    for (var seatOffset = 0; seatOffset < seatCount; seatOffset++)
                    {
                        var seatNumberWithinSection = seatOffset + 1;
                        var numero = sectionCode * 1000 + seatNumberWithinSection;

                        newSeats.Add(new Asiento
                        {
                            EventId = evento.Id_Evento,
                            Numero = numero,
                            Estado = EstadoAsiento.Disponible
                        });
                    }
                }

                if (newSeats.Count > 0)
                {
                    context.Asientos.AddRange(newSeats);
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Estima el número de asientos en una sección basada en su código.
        /// </summary>
        private static int EstimateSeatCount(SectionDefinition definition)
        {
            var code = definition.SectionId.AsSpan(4);

            if (code.Length == 1 && char.IsLetter(code[0]))
            {
                return 80;
            }

            if (int.TryParse(code, out var numericCode))
            {
                if (numericCode >= 200)
                {
                    return 120;
                }

                if (numericCode >= 100)
                {
                    return 100;
                }
            }

            return 60;
        }
    }
}
