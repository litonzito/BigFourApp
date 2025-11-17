using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BigFourApp.Models.Manager
{
    public class CreateEventVM
    {
        public string? Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Nombre del evento")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Display(Name = "Venue")]
        public string VenueName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha del evento")]
        public string? EventDateText { get; set; }

        [Url]
        [Display(Name = "Url oficial")]
        public string? Url { get; set; }

        [Display(Name = "Segmento")]
        public string? Segment { get; set; }

        [Display(Name = "Género")]
        public string? Genre { get; set; }

        [Display(Name = "Subgénero")]
        public string? SubGenre { get; set; }

        [Display(Name = "Soporta SafeTix")]
        public bool SafeTix { get; set; }

        [Display(Name = "Evento cancelado")]
        public bool IsCancelled { get; set; }

        [Display(Name = "Mapa de asientos")]
        public IFormFile? SeatmapImage { get; set; }

        [Display(Name = "Imágenes del evento")]
        public List<IFormFile> EventImages { get; set; } = new();

        public List<SectionInputVM> Sections { get; set; } = new();

        public string? ExistingSeatmapUrl { get; set; }
        public List<string> ExistingImages { get; set; } = new();
        public List<string> ImagesToRemove { get; set; } = new();

        public bool IsEdit => !string.IsNullOrWhiteSpace(Id);
    }

    public class SectionInputVM
    {
        [Required]
        [Display(Name = "Nombre de la sección")]
        public string DisplayName { get; set; } = string.Empty;

        [Range(1, 100000)]
        [Display(Name = "Asientos totales")]
        public int SeatCount { get; set; } = 50;

        [Range(1, 100000)]
        [Display(Name = "Asientos por fila")]
        public int SeatsPerRow { get; set; } = 10;

        [Range(1, 100000)]
        [DataType(DataType.Currency)]
        [Display(Name = "Precio base (USD)")]
        public decimal BasePrice { get; set; } = 85m;

        [Display(Name = "Código interno")]
        public string? SectionCode { get; set; }
    }

    public class EventDashboardVM
    {
        public IReadOnlyList<EventSummaryVM> Events { get; set; } = new List<EventSummaryVM>();
    }

    public class EventSummaryVM
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int SectionCount { get; set; }
        public int SeatCount { get; set; }
        public int AvailableSeats { get; set; }
    }
}
