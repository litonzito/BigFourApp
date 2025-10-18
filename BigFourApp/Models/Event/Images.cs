using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigFourApp.Models.Event
{
    public class Images
    {
        [Key]
        public int Id_Image { get; set; }

        [MaxLength(50)]
        public string EventId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [ForeignKey("EventId")]
        public Evento Event { get; set; } = null!; // ! indica que no es null
    }

}
