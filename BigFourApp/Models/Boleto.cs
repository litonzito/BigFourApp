using System.ComponentModel.DataAnnotations;
using BigFourApp.Models.Event;

namespace BigFourApp.Models
{
    public class Boleto
    {
        [Key]
        public int Id_Boleto { get; set; }

        [Required]
        public string CodigoUnico { get; set; } = Guid.NewGuid().ToString("N");
        public bool Notificar { get; set; } = true;

        // Navigation
        public ICollection<DetalleVenta> DetalleVentas { get; set; }
    }
}
