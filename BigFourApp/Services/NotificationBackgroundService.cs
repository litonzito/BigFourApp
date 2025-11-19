using BigFourApp.Data;
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
            var proximas = await db.Boletos
                .Include(a => a.Asiento)
                .Where(a => a.Asiento.Event.Date <= ahora.AddMinutes(30))
                .ToListAsync();

            foreach (var act in proximas)
            {
                await _emailService.SendEmail(
                    act.DetalleVenta.Venta.Usuario.Email,
                    $"Recordatorio: {act.Asiento.Event.Name}",
                    $"Tu evento <b>{act.Asiento.Event.Name}</b> esta proximo a inicial a las <b>{act.Asiento.Event.Date}</b>."
                );

                // Desactiva notificación para no volverla a mandar
                act.Notificar = false;
            }
            Console.WriteLine("Background service running...");

            await db.SaveChangesAsync();

            // revisar cada 60 segundos
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
