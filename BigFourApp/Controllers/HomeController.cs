using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.EntityFrameworkCore;
using BigFourApp.Models.Event;

namespace BigFourApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly BaseDatos _context;

    public HomeController(ILogger<HomeController> logger, BaseDatos context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        var eventos = _context.Events
            .Include(e => e.EventImageUrl)
            .Include(e => e.Venues)
            .Include(e => e.Classifications)
            .OrderBy(e => e.Date)
            .ToList();
        return View(eventos);
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
