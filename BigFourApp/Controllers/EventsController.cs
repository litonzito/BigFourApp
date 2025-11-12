using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.EntityFrameworkCore;
using BigFourApp.Models.Event;

namespace BigFourApp.Controllers;

public class EventsController : Controller
{
    private readonly BaseDatos _context;
    public EventsController(BaseDatos context)
    {
        _context = context;
    }

   
    public IActionResult Detalles(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var evento = _context.Events
            .Include(e => e.EventImageUrl)
            .Include(e => e.Venues)
            .Include(e => e.Classifications)
            .AsEnumerable()
            .FirstOrDefault(e => e.Id_Evento.Equals(id, StringComparison.OrdinalIgnoreCase));

        var venue = _context.Venues.Include(v => v.VenueImageUrl);
        
        if (evento == null)
            return NotFound();

        return View(evento);
    }

}
