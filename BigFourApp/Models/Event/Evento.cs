using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BigFourApp.Models;

namespace BigFourApp.Models.Event
{
    public class Evento
    {
        [Key]
        [MaxLength(50)]
        public string Id_Evento { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.MinValue;

        [MaxLength(500)]
        public string SeatmapUrl { get; set; } = string.Empty;

        public bool SafeTix { get; set; } = false;
        public bool IsCancelled { get; set; } = false;

        public string? EventImageUrl { get; internal set; }

        [ForeignKey(nameof(Manager))]
        public string? ManagerId { get; set; }
        public ApplicationUser? Manager { get; set; }

        // Inicializar colecciones para evitar null warnings
        public ICollection<Venue> Venues { get; set; } = new List<Venue>();
        public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
        public ICollection<Asiento> Asientos { get; set; } = new List<Asiento>();
    }
}
