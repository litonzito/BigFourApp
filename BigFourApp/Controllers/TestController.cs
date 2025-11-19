using BigFourApp.Models;
using BigFourApp.Models.Event;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class TestController : Controller
{
    private readonly BaseDatos _context;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TestController(IEmailService emailService, UserManager<ApplicationUser> userManager)
    {
        _emailService = emailService;
        _userManager = userManager;
    }

    public async Task<IActionResult> ProbarCorreo()
    {
        var user = await _userManager.GetUserAsync(User);

        await _emailService.SendEmail(
            user.Email,
            " Notificación de prueba",
            "Si recibes este correo, tu sistema StudyTrack puede enviar notificaciones correctamente."
        );

        return Content("Correo enviado — revisa tu bandeja");
    }
    public async Task<IActionResult> TestCrearBoletoYNotificar()
    {

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Content("No hay usuario logueado");

        // ----------------------------
        // 1. Crear evento de prueba
        // ----------------------------
        var evento = new Evento
        {
            Name = "Concierto de Prueba",
            Date = DateTime.Now.AddMinutes(10),
        };
        _context.Events.Add(evento);
        await _context.SaveChangesAsync();

        // ----------------------------
        // 2. Crear asiento ligado al evento
        // ----------------------------
        var asiento = new Asiento
        {
            Numero = 12,
            SectionId = "A1",
            Estado = EstadoAsiento.Ocupado,
            EventId = evento.Id_Evento.ToString()
        };
        _context.Asientos.Add(asiento);
        await _context.SaveChangesAsync();

        // ----------------------------
        // 3. Crear venta
        // ----------------------------
        var venta = new Venta
        {
            Id_Usuario = user.Id, // si usas IdentityUser extiende bien esto
            Fecha = DateTime.Now,
            Total = 250,
            MetodoPago = "Prueba"
        };
        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();

        // ----------------------------
        // 4. Crear detalle de venta ligado al asiento
        // ----------------------------
        var detalle = new DetalleVenta
        {
            Id_Venta = venta.Id_Venta,
            Cantidad = 1,
            PrecioUnitario = 250,
            Id_Asiento = asiento.Id_Asiento
        };
        _context.DetallesVenta.Add(detalle);
        await _context.SaveChangesAsync();

        // ----------------------------
        // 5. Crear boleto ligado al detalle venta
        // ----------------------------
        var boleto = new Boleto
        {
            Notificar = true,
            Tipo ="Recordatorio"
        };
                // ----------------------------
                // 6. Enviar correo igual que el background service
                // ----------------------------
    await _emailService.SendEmail(
        user.Email,
        $"Recordatorio de Prueba: {evento.Name}",
        $@"
            <p>Tu evento de prueba <b>{evento.Name}</b> está por comenzar.</p>
            <p>Fecha y hora: <b>{evento.Date}</b></p>
            <p>Asiento: <b>{asiento.Numero}</b> — Sección <b>{asiento.SectionId}</b></p>
        "
    );

    return Content($"Se creó un boleto de prueba y se envió correo a: {user.Email}");
    }


}
