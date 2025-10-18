using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigFourApp.Models.Event
{
    public class Classification
    {
        [Key]
        public int Id_Classification { get; set; }

        [MaxLength(50)]
        public string EventId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Segment { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Genre { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SubGenre { get; set; } = string.Empty;

        [ForeignKey("EventId")]
        public Evento Event { get; set; }= null!; // ! indica que no es null
    }

}
