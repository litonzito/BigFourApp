using BigFourApp.Data;
using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.EntityFrameworkCore;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailService _emailService;

    public NotificationBackgroundService(IServiceProvider serviceProvider, IEmailService emailService)
    {
        _serviceProvider = serviceProvider;
        _emailService = emailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BaseDatos>();

            // actividades a notificar en los próximos 30 minutos
            var ahora = DateTime.Now;
            var boletos = await db.Boletos
                .Include(b => b.DetalleVentas)
                    .ThenInclude(dv => dv.Asiento)
                        .ThenInclude(a => a.Event)
                .Include(b => b.DetalleVentas)
                    .ThenInclude(dv => dv.Venta)
                        .ThenInclude(v => v.Usuario)
                .Where(b =>
                    b.Notificar &&
                    b.DetalleVentas.Any(dv =>
                        dv.Asiento.Event.Date <= ahora.AddMinutes(30) &&
                        dv.Asiento.Event.Date > ahora
                    ))
                .ToListAsync(stoppingToken);



            foreach (var boleto in boletos)
            {
                var detalle = boleto.DetalleVentas.FirstOrDefault();

                await _emailService.SendEmail(
                    detalle.Venta.Usuario.Email,
                    $"Recordatorio: {detalle.Asiento.Event.Name}",
                    $@"
                                <p>Tu evento <b>{detalle.Asiento.Event.Name}</b> está por comenzar.</p>
                                <p>Fecha y hora: <b>{detalle.Asiento.Event.Date}</b></p>
                            "
                );


                // Desactiva notificación para no volverla a mandar
                boleto.Notificar = false;
            }
            Console.WriteLine("Background service running...");

            await db.SaveChangesAsync();

            // revisar cada 60 segundos
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
