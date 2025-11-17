using BigFourApp.Models;
using BigFourApp.Models.Event;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace BigFourApp.Persistence
{
    public class BaseDatos : DbContext
    {
        public BaseDatos(DbContextOptions<BaseDatos> options) : base(options) { }
        //Las tablas que se crean en la base de datos
        public DbSet<Boleto> Boletos { get; set; }
        public DbSet<Asiento> Asientos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Venta> Ventas { get; set; }    
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<Evento> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Classification> Classifications { get; set; }
        public DbSet<Images> Images { get; set; }
        public DbSet<VenueSection> VenueSections { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- 🔹 Claves primarias ---
            modelBuilder.Entity<Boleto>().HasKey(b => b.Id_Boleto);
            modelBuilder.Entity<Asiento>().HasKey(a => a.Id_Asiento);
            modelBuilder.Entity<Usuario>().HasKey(u => u.Id_Usuario);
            modelBuilder.Entity<Venta>().HasKey(v => v.Id_Venta);
            modelBuilder.Entity<DetalleVenta>().HasKey(dv => dv.Id_DetalleVenta);
            modelBuilder.Entity<Evento>().HasKey(e => e.Id_Evento);
            modelBuilder.Entity<Venue>().HasKey(v => v.Id_Venue);
            modelBuilder.Entity<Classification>().HasKey(c => c.Id_Classification);
            modelBuilder.Entity<Images>().HasKey(i => i.Id_Image);

            // --- 🔹 Relaciones de tu sistema principal ---
            // Usuario -> Ventas (1:N)
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.Id_Usuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta -> DetalleVenta (1:N)
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Venta)
                .WithMany(v => v.DetallesVenta)
                .HasForeignKey(dv => dv.Id_Venta)
                .OnDelete(DeleteBehavior.Restrict);

            // DetalleVenta -> Boleto (1:1)
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Boleto)
                .WithOne(b => b.DetalleVenta)
                .HasForeignKey<DetalleVenta>(dv => dv.Id_Boleto)
                .OnDelete(DeleteBehavior.Restrict);

            // Boleto -> Asiento (1:1)
            modelBuilder.Entity<Boleto>()
                .HasOne(b => b.Asiento)
                .WithOne(a => a.Boleto)
                .HasForeignKey<Boleto>(b => b.Id_Asiento)
                .OnDelete(DeleteBehavior.Restrict);


            // --- 🔹 Relaciones de la API Discovery ---
            // Evento -> Venue (1:N)
            modelBuilder.Entity<Venue>()
                .HasOne(v => v.Event)
                .WithMany(e => e.Venues)
                .HasForeignKey(v => v.EventId)
                .OnDelete(DeleteBehavior.SetNull);//Agregacion. Puede existir sin el evento

            // Evento -> Classification (1:N)
            modelBuilder.Entity<Classification>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Classifications)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Evento -> Images (1:N)
            modelBuilder.Entity<Images>()
                .HasOne(i => i.Event)
                .WithMany(e => e.Images)
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Evento -> Asiento (1:N)
            modelBuilder.Entity<Asiento>()
                .HasOne(a => a.Event)
                .WithMany(e => e.Asientos)
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.SetNull);//Agregacion. Puede existir sin el evento

            // Conversión de enum EstadoAsiento a string
            modelBuilder.Entity<Asiento>()
                .Property(a => a.Estado)
                .HasConversion<string>();

            modelBuilder.Entity<Asiento>()
                .Property(a => a.SectionId)
                .HasMaxLength(64);

            modelBuilder.Entity<VenueSection>()
                .HasOne(s => s.Venue)
                .WithMany(v => v.Sections)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- 🔹 Opcional: restricciones adicionales ---
            modelBuilder.Entity<Evento>().Property(e => e.Name).IsRequired().HasMaxLength(255);
            modelBuilder.Entity<Evento>().Property(e => e.Url).HasMaxLength(500);
            modelBuilder.Entity<Evento>().Property(e => e.SeatmapUrl).HasMaxLength(500);
        }

    }

}
