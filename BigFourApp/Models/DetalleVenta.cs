namespace BigFourApp.Models
{
    public class DetalleVenta
    {
        public int Id_DetalleVenta { get; set; }
        public int Id_Venta { get; set; }
        public int Id_Boleto { get; set; }
        public int Cantidad { get; set; }   
        public decimal PrecioUnitario { get; set; } 
    }
}
