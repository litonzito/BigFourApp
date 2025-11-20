using BigFourApp.Models;
using BigFourApp.Models.Event;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BigFourApp.Persistence
{
    public class BaseDatos : IdentityDbContext<ApplicationUser>
    {
        public BaseDatos(DbContextOptions<BaseDatos> options) : base(options) { }

        public DbSet<Boleto> Boletos { get; set; }
        public DbSet<Asiento> Asientos { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<Evento> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Classification> Classifications { get; set; }
        public DbSet<VenueSection> VenueSections { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------- CLAVES PRIMARIAS -------
            modelBuilder.Entity<Boleto>().HasKey(b => b.Id_Boleto);
            modelBuilder.Entity<Asiento>().HasKey(a => a.Id_Asiento);
            modelBuilder.Entity<ApplicationUser>().HasKey(u => u.Id);
            modelBuilder.Entity<Venta>().HasKey(v => v.Id_Venta);
            modelBuilder.Entity<DetalleVenta>().HasKey(dv => dv.Id_DetalleVenta);
            modelBuilder.Entity<Evento>().HasKey(e => e.Id_Evento);
            modelBuilder.Entity<Venue>().HasKey(v => v.Id_Venue);
            modelBuilder.Entity<Classification>().HasKey(c => c.Id_Classification);

            // ------- RELACIONES PRINCIPALES -------

            // Usuario (1) -> (N) Eventos administrados
            modelBuilder.Entity<Evento>()
                .HasOne(e => e.Manager)
                .WithMany(u => u.ManagedEvents)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Usuario (1) -> (N) Ventas
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Ventas)
                .HasForeignKey(v => v.Id_Usuario)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta (1) -> (N) DetallesVenta
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Venta)
                .WithMany(v => v.DetallesVenta)
                .HasForeignKey(dv => dv.Id_Venta)
                .OnDelete(DeleteBehavior.Restrict);

            // Boleto (1) -> (N) DetallesVenta
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Boleto)
                .WithMany(b => b.DetalleVentas)
                .HasForeignKey(dv => dv.Id_Boleto)
                .OnDelete(DeleteBehavior.Cascade);

            // DetalleVenta (1) -> (1) Asiento
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(dv => dv.Asiento)
                .WithOne(a => a.DetalleVenta)
                .HasForeignKey<DetalleVenta>(dv => dv.Id_Asiento)
                .OnDelete(DeleteBehavior.Restrict);

            // Evento (1) -> Venue(N)
            modelBuilder.Entity<Venue>()
                .HasOne(v => v.Event)
                .WithMany(e => e.Venues)
                .HasForeignKey(v => v.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Evento (1) -> Classification(N)
            modelBuilder.Entity<Classification>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Classifications)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Evento (1) -> Asientos(N)
            modelBuilder.Entity<Asiento>()
                .HasOne(a => a.Event)
                .WithMany(e => e.Asientos)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            // Enum  -> string
            modelBuilder.Entity<Asiento>()
                .Property(a => a.Estado)
                .HasConversion<string>();

            // Venue (1) -> VenueSection(N)
            modelBuilder.Entity<VenueSection>()
                .HasOne(s => s.Venue)
                .WithMany(v => v.Sections)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Usuario (1) -> Notificacion (N)
            modelBuilder.Entity<Notificacion>()
                .HasOne(n => n.Usuario)
                .WithMany(u => u.Notificaciones)
                .HasForeignKey(n => n.Id_Usuario)
                .OnDelete(DeleteBehavior.Cascade);

            // Extra validations
            modelBuilder.Entity<Evento>().Property(e => e.Name).IsRequired().HasMaxLength(255);
            modelBuilder.Entity<Evento>().Property(e => e.Url).HasMaxLength(500);
            modelBuilder.Entity<Evento>().Property(e => e.SeatmapUrl).HasMaxLength(500);
        }
    }
}
