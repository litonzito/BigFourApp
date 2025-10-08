using BigFourApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BigFourApp.Persistence
{
    public class BaseDatos : DbContext
    {
        public BaseDatos(DbContextOptions<BaseDatos> options) : base(options) { }
        //Las tablas que se crean en la base de datos
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Boleto> Boletos { get; set; }
        public DbSet<Venta> Ventas { get; set; }    
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        
    }
}
