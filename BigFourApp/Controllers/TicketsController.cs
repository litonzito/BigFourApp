using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BigFourApp.Models;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigFourApp.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly BaseDatos _context;

        public TicketsController(BaseDatos context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var tickets = await _context.DetallesVenta
                .Include(dv => dv.Asiento)
                    .ThenInclude(a => a.Event)
                        .ThenInclude(e => e.Venues)
                .Include(dv => dv.Boleto)
                .Include(dv => dv.Venta)
                    .ThenInclude(v => v.Usuario)
                .Where(dv => dv.Venta.Id_Usuario == userId)
                .OrderByDescending(dv => dv.Venta.Fecha)
                .AsNoTracking()
                .ToListAsync();

            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var ticket = await _context.DetallesVenta
                .Include(dv => dv.Asiento)
                    .ThenInclude(a => a.Event)
                        .ThenInclude(e => e.Venues)
                .Include(dv => dv.Boleto)
                .Include(dv => dv.Venta)
                    .ThenInclude(v => v.Usuario)
                .AsNoTracking()
                .FirstOrDefaultAsync(dv => dv.Id_DetalleVenta == id && dv.Venta.Id_Usuario == userId);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
    }
}
