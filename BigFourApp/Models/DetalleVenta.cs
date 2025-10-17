using System.ComponentModel.DataAnnotations;

namespace BigFourApp.Models
{
    public class DetalleVenta
    {
        [Key]
        public int Id_DetalleVenta { get; set; }
        public int Id_Venta { get; set; }
        public int Id_Boleto { get; set; }
        public int Cantidad { get; set; }   = 0;
        public decimal PrecioUnitario { get; set; } = 0.0m;

        public Venta Venta { get; set; }
        public Boleto Boleto { get; set; }
    }
}
