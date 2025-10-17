using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigFourApp.Models.Event
{

    public class Venue
    {
        [Key]
        public int Id_Venue { get; set; }

        [MaxLength(50)]
        public string? EventId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [ForeignKey("EventId")]
        public Evento? Event { get; set; } 
    }

}
