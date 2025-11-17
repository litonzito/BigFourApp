using BigFourApp.Models.Event;
using BigFourApp.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BigFourApp.Data
{
    public static class JsonLoader
    {
        public static void LoadEvents(BaseDatos context)
        {
            // Leer el archivo JSON completo
            string jsonString = File.ReadAllText("eventosDiluidos.json");
            using var doc = JsonDocument.Parse(jsonString);
            var eventsJson = doc.RootElement.GetProperty("events");

            foreach (var e in eventsJson.EnumerateArray())
            {
                var ev = new Evento
                {
                    Id_Evento = e.GetProperty("id").GetString(),
                    Name = e.GetProperty("name").GetString(),
                    Url = e.GetProperty("url").GetString(),
                    Date = DateTime.Parse(e.GetProperty("dates").GetProperty("start").GetProperty("localDate").GetString()),
                    SeatmapUrl = e.GetProperty("seatmap").GetProperty("staticUrl").GetString(),
                    SafeTix = e.GetProperty("ticketing").GetProperty("safeTix").GetProperty("enabled").GetBoolean(),
                    EventImageUrl = e.GetProperty("EventImageURL").GetString(), // ✅ corregido
                    Venues = new List<Venue>(),
                    Classifications = new List<Classification>()
                };

                // Venues
                foreach (var v in e.GetProperty("venues").EnumerateArray())
                {
                    ev.Venues.Add(new Venue
                    {
                        Name = v.GetProperty("name").GetString(),
                        City = v.GetProperty("city").GetString(),
                        State = v.GetProperty("state").GetString(),
                        VenueImageUrl = v.GetProperty("VenueImageURL").GetString() // ✅ corregido
                    });
                }

                // Classifications
                foreach (var c in e.GetProperty("classifications").EnumerateArray())
                {
                    ev.Classifications.Add(new Classification
                    {
                        Segment = c.GetProperty("segment").GetProperty("name").GetString(),
                        Genre = c.GetProperty("genre").GetProperty("name").GetString(),
                        SubGenre = c.GetProperty("subGenre").GetProperty("name").GetString()
                    });
                }

                context.Events.Add(ev);
            }

            context.SaveChanges();
        }
    }
}
