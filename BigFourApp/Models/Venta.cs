namespace BigFourApp.Models
{
    public class Venta
    {
        public int Id_Venta { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public string MetodoPago { get; set; }
        public int Id_Usuario { get; set; }
        public int Id_Boleto { get; set; }
        // Navigation properties
        public Usuarios Usuario { get; set; }
        public Boleto Boleto { get; set; }
        public List<DetalleVenta> DetallesVenta { get; set; }
    }
}
