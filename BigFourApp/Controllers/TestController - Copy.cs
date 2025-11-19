using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BigFourApp.Models;
using Microsoft.EntityFrameworkCore;

public class NotificacionController : Controller
{
    private readonly DbContext<BaseDatos>  _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificacionController( UserManager<ApplicationUser> userManager)
    {
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
}
