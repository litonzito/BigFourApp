namespace BigFourApp.Models
{
    public class Usuarios
    {
        public int Id_Usuario { get; set; }
        public int ventaId { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }

        public List<Venta> Ventas { get; set; }

    }
}
