using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class NotificacionesController : Controller
{
    private readonly BaseDatos _context;
    private readonly UserManager<ApplicationUser> _userManager;

    // Inyectar UserManager
    public NotificacionesController(BaseDatos context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Notificaciones()
    {
        // Obtener ID del usuario actual
        var userId = _userManager.GetUserId(User);

        // Listar notificaciones del usuario
        var listNot = await _context.Notificaciones
            .Include(n => n.Usuario)
            .Where(n => n.Id_Usuario == userId) // solo las del usuario
            .OrderByDescending(n => n.fecha)
            .ToListAsync();

        return View("NotifsHistory", listNot);
    }

    [HttpPost]
    public async Task<IActionResult> EliminarNotificacion(int id)
    {
        // Buscar notificación del usuario actual
        var userId = _userManager.GetUserId(User);
        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.Id_Notificacion == id && n.Id_Usuario == userId);

        if (notificacion == null)
        {
            return NotFound();
        }

        _context.Notificaciones.Remove(notificacion);
        await _context.SaveChangesAsync();

        return RedirectToAction("Notificaciones");
    }
}
