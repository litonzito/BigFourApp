using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class NotificacionesController : Controller
{
    private readonly BaseDatos _context;

    public NotificacionesController(BaseDatos context )
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Notificaciones()
    {
        var listNot = await _context.Notificaciones
            .Include(n => n.Usuario)
            .OrderByDescending(n => n.fecha)
            .ToListAsync();
        return View("NotifsHistory", listNot);
    }

    [HttpPost]
    public async Task<IActionResult> EliminarNotificacion(int id)
    {
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null)
        {
            return NotFound();
        }
        _context.Notificaciones.Remove(notificacion);
        await _context.SaveChangesAsync();
        return RedirectToAction("Notificaciones");
    }
}
