using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigFourApp.Models.Event
{
    public class VenueSection
    {
        [Key]
        public int Id { get; set; }

        public int VenueId { get; set; }

        [MaxLength(64)]
        public string SectionCode { get; set; } = string.Empty;

        [MaxLength(128)]
        public string DisplayName { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public int SeatCount { get; set; }

        public int SeatsPerRow { get; set; } = 10;

        [ForeignKey(nameof(VenueId))]
        public Venue? Venue { get; set; }
    }
}
