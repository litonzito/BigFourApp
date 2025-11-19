using System.ComponentModel.DataAnnotations;
using BigFourApp.Models.Event;

namespace BigFourApp.Models
{
    public class Boleto
    {
        [Key]
        public int Id_Boleto { get; set; }

        public string Tipo { get; set; } = "Sin tipo asignado";
        public bool Notificar { get; set; } = true;

        // Navigation
        public ICollection<DetalleVenta> DetalleVentas { get; set; }
    }
}
