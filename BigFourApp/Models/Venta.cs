using System.ComponentModel.DataAnnotations;

namespace BigFourApp.Models
{
    public class Venta
    {
        [Key]
        public int Id_Venta { get; set; }
        public int Id_Usuario { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = "No especificado";
        // Navigation properties
        public Usuario Usuario { get; set; }
        public List<DetalleVenta> DetallesVenta { get; set; } 
    }
}
