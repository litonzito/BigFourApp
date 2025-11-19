using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class NotificacionController : Controller
{
    private readonly BaseDatos _context;

    public NotificacionController(BaseDatos context )
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
        return View(listNot);
    }
}
