using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using BigFourApp.Data;
using BigFourApp.Models;
using BigFourApp.Models.Event;
using BigFourApp.Models.Manager;
using BigFourApp.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BigFourApp.Controllers
{
    [Authorize(Roles = "EventManager")]
    public class ManagerController : Controller
    {
        private readonly BaseDatos _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ManagerController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerController(BaseDatos context, IWebHostEnvironment environment, ILogger<ManagerController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> EventDashboard()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var events = await _context.Events
                .Where(e => e.ManagerId == userId)

                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .Include(e => e.Asientos)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            var summaries = events.Select(e =>
            {
                var venue = e.Venues.FirstOrDefault();
                var sectionCount = venue?.Sections.Count ?? 0;
                var seatCount = venue?.Sections.Sum(s => s.SeatCount) ?? e.Asientos.Count;
                var availableSeats = e.Asientos.Count(a => a.Estado == EstadoAsiento.Disponible);
                return new EventSummaryVM
                {
                    Id = e.Id_Evento,
                    Name = e.Name,
                    Date = e.Date,
                    VenueName = venue?.Name ?? "Sin asignar",
                    City = venue?.City ?? string.Empty,
                    State = venue?.State ?? string.Empty,
                    IsCancelled = e.IsCancelled,
                    // changed to be a single url stored in Events table .,. 
                    ImageUrl = e.EventImageUrl ?? string.Empty,
                    SectionCount = sectionCount,
                    SeatCount = seatCount,
                    AvailableSeats = availableSeats
                };
            }).ToList();

            var vm = new EventDashboardVM { Events = summaries };
            return View("~/Views/Manager/EventDashboard.cshtml", vm);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            var vm = BuildDefaultCreateVm();
            ConfigureFormMetadata(isEdit: false);
            return View("~/Views/Manager/EventForm.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventVM vm)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var hasValidDate = TryParseEventDate(vm, out var eventDate);
            if (!ModelState.IsValid || !hasValidDate)
            {
                ConfigureFormMetadata(isEdit: false);
                return View("~/Views/Manager/EventForm.cshtml", vm);
            }

            NormalizeSections(vm);
            if (!vm.Sections.Any())
            {
                ModelState.AddModelError(nameof(vm.Sections), "Define al menos una sección con asientos.");
                ConfigureFormMetadata(isEdit: false);
                return View("~/Views/Manager/EventForm.cshtml", vm);
            }

            var eventId = string.IsNullOrWhiteSpace(vm.Id) ? Guid.NewGuid().ToString("N") : vm.Id!;
            var evento = new Evento
            {
                Id_Evento = eventId,
                Name = vm.Name.Trim(),
                Url = vm.Url ?? string.Empty,
                Date = eventDate,
                SeatmapUrl = vm.ExistingSeatmapUrl ?? string.Empty,
                SafeTix = vm.SafeTix,
                IsCancelled = vm.IsCancelled,
                ManagerId = userId
               
            };

            var venue = new Venue
            {
                EventId = eventId,
                Name = vm.VenueName.Trim(),
                City = vm.City.Trim(),
                State = vm.State.Trim()
            };

            foreach (var sectionVm in vm.Sections)
            {
                venue.Sections.Add(new VenueSection
                {
                    SectionCode = sectionVm.SectionCode?.Trim(),
                    DisplayName = sectionVm.DisplayName.Trim(),
                    BasePrice = sectionVm.BasePrice,
                    SeatCount = sectionVm.SeatCount,
                    SeatsPerRow = sectionVm.SeatsPerRow <= 0 ? 10 : sectionVm.SeatsPerRow
                });
            }

            evento.Venues.Add(venue);
            ApplyClassificationValues(evento, vm);
            await HandleUploadsAsync(evento, vm, isEdit: false);

            _context.Events.Add(evento);
            await _context.SaveChangesAsync();

            SeatSeeder.GenerateSeatsForEvent(_context, evento, overwriteExisting: true, saveChanges: true);

            return RedirectToAction(nameof(EventDashboard));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var evento = await _context.Events
                .Where(e => e.ManagerId == userId)

                .Include(e => e.Classifications)
                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .FirstOrDefaultAsync(e => e.Id_Evento == id);

            if (evento is null)
            {
                return NotFound();
            }

            var vm = MapEventToVm(evento);
            ConfigureFormMetadata(isEdit: true);
            return View("~/Views/Manager/EventForm.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(CreateEventVM vm)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var hasValidDate = TryParseEventDate(vm, out var eventDate);
            if (!ModelState.IsValid || !hasValidDate)
            {
                ConfigureFormMetadata(isEdit: true);
                return View("~/Views/Manager/EventForm.cshtml", vm);
            }

            NormalizeSections(vm);
            if (!vm.Sections.Any())
            {
                ModelState.AddModelError(nameof(vm.Sections), "Define al menos una sección con asientos.");
                ConfigureFormMetadata(isEdit: true);
                return View("~/Views/Manager/EventForm.cshtml", vm);
            }

            if (string.IsNullOrWhiteSpace(vm.Id))
            {
                return NotFound();
            }

            var evento = await _context.Events
                .Where(e => e.ManagerId == userId)

                .Include(e => e.Classifications)
                .Include(e => e.Venues)
                    .ThenInclude(v => v.Sections)
                .Include(e => e.Asientos)
                .FirstOrDefaultAsync(e => e.Id_Evento == vm.Id);

            if (evento is null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(evento.ManagerId))
            {
                evento.ManagerId = userId;
            }

            evento.Name = vm.Name.Trim();
            evento.Url = vm.Url ?? string.Empty;
            evento.Date = eventDate;
            evento.SafeTix = vm.SafeTix;
            evento.IsCancelled = vm.IsCancelled;

            var venue = evento.Venues.FirstOrDefault();
            if (venue is null)
            {
                venue = new Venue { EventId = evento.Id_Evento };
                evento.Venues.Add(venue);
            }

            venue.Name = vm.VenueName.Trim();
            venue.City = vm.City.Trim();
            venue.State = vm.State.Trim();

            if (venue.Sections.Any())
            {
                _context.VenueSections.RemoveRange(venue.Sections);
                venue.Sections.Clear();
            }

            foreach (var sectionVm in vm.Sections)
            {
                venue.Sections.Add(new VenueSection
                {
                    SectionCode = sectionVm.SectionCode?.Trim(),
                    DisplayName = sectionVm.DisplayName.Trim(),
                    BasePrice = sectionVm.BasePrice,
                    SeatCount = sectionVm.SeatCount,
                    SeatsPerRow = sectionVm.SeatsPerRow <= 0 ? 10 : sectionVm.SeatsPerRow
                });
            }

            ApplyClassificationValues(evento, vm);
            await HandleUploadsAsync(evento, vm, isEdit: true);

            await _context.SaveChangesAsync();

            SeatSeeder.GenerateSeatsForEvent(_context, evento, overwriteExisting: true, saveChanges: true);

            return RedirectToAction(nameof(EventDashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEvent(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(EventDashboard));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var evento = await _context.Events.FirstOrDefaultAsync(e => e.Id_Evento == id && e.ManagerId == userId);
            if (evento is null)
            {
                return RedirectToAction(nameof(EventDashboard));
            }

            evento.IsCancelled = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(EventDashboard));
        }

        private void ApplyClassificationValues(Evento evento, CreateEventVM vm)
        {
            var hasClassificationData = !string.IsNullOrWhiteSpace(vm.Segment) ||
                                        !string.IsNullOrWhiteSpace(vm.Genre) ||
                                        !string.IsNullOrWhiteSpace(vm.SubGenre);

            if (!hasClassificationData)
            {
                return;
            }

            var classification = evento.Classifications.FirstOrDefault();
            if (classification is null)
            {
                classification = new Classification();
                evento.Classifications.Add(classification);
            }

            classification.Segment = vm.Segment ?? string.Empty;
            classification.Genre = vm.Genre ?? string.Empty;
            classification.SubGenre = vm.SubGenre ?? string.Empty;
        }

        private CreateEventVM BuildDefaultCreateVm()
        {
            return new CreateEventVM
            {
                EventDateText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Sections = new List<SectionInputVM>
                {
                    new SectionInputVM
                    {
                        DisplayName = "Sección 1",
                        SeatCount = 100,
                        SeatsPerRow = 10,
                        BasePrice = 85m
                    }
                }
            };
        }

        private void NormalizeSections(CreateEventVM vm)
        {
            vm.Sections ??= new List<SectionInputVM>();

            vm.Sections = vm.Sections
                .Select(section =>
                {
                    var seatCount = section.SeatCount <= 0 ? 1 : section.SeatCount;
                    var seatsPerRow = section.SeatsPerRow <= 0 ? 1 : section.SeatsPerRow;
                    if (seatsPerRow > seatCount)
                    {
                        seatsPerRow = seatCount;
                    }

                    return new SectionInputVM
                    {
                        DisplayName = section.DisplayName?.Trim() ?? string.Empty,
                        BasePrice = section.BasePrice <= 0 ? 1 : section.BasePrice,
                        SeatCount = seatCount,
                        SeatsPerRow = seatsPerRow,
                        SectionCode = section.SectionCode?.Trim()
                    };
                })
                .Where(s => !string.IsNullOrWhiteSpace(s.DisplayName))
                .ToList();
        }

        private void ConfigureFormMetadata(bool isEdit)
        {
            ViewData["FormAction"] = isEdit ? nameof(EditEvent) : nameof(CreateEvent);
            ViewData["SubmitLabel"] = isEdit ? "Guardar cambios" : "Crear evento";
            ViewData["FormTitle"] = isEdit ? "Editar evento" : "Nuevo evento";
            ViewData["FormSubtitle"] = isEdit
                ? "Actualiza los datos del evento y su distribución de asientos."
                : "Define la información base y las secciones del venue.";
        }

        private bool TryParseEventDate(CreateEventVM vm, out DateTime eventDate)
        {
            eventDate = default;
            var hasValidDate = DateTime.TryParseExact(
                vm.EventDateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date);

            if (!hasValidDate)
            {
                ModelState.AddModelError(nameof(vm.EventDateText), "Fecha inválida.");
            }
            // Validar hora
            if (string.IsNullOrWhiteSpace(vm.EventTimeText))
            {
                ModelState.AddModelError(nameof(vm.EventTimeText), "La hora es obligatoria.");
                return false;
            }
            if (!TimeSpan.TryParse(vm.EventTimeText, out var time))
            {
                ModelState.AddModelError(nameof(vm.EventTimeText), "Formato de hora inválido.");
                return false;
            }
            if (!hasValidDate)
            {
                return false;
            }

            eventDate = date + time;
            return true;
        }

        private async Task HandleUploadsAsync(Evento evento, CreateEventVM vm, bool isEdit)
        {
            
            if (vm.SeatmapImage != null)
            {
                var seatmapUrl = await SaveFileAsync(vm.SeatmapImage, "seatmaps");
                if (!string.IsNullOrEmpty(seatmapUrl))
                {
                    if (isEdit)
                    {
                        TryDeleteAsset(evento.SeatmapUrl);
                    }
                    evento.SeatmapUrl = seatmapUrl;
                }
            }

            // Remove current event image if requested
            if (isEdit && vm.ImagesToRemove?.Any() == true)
            {
              
                if (!string.IsNullOrEmpty(evento.EventImageUrl) &&
                    vm.ImagesToRemove.Contains(evento.EventImageUrl))
                {
                    TryDeleteAsset(evento.EventImageUrl);
                    evento.EventImageUrl = null;
                }
            }

            // Upload a new event image (we only store one URL as of now :3 )
            if (vm.EventImages?.Any() == true)
            {
                // current upload is the main image for the event
                var file = vm.EventImages.First();
                var url = await SaveFileAsync(file, "events");
                if (!string.IsNullOrEmpty(url))
                {
                    // If editing, deletes the previous image . . hopefully .. 
                    if (isEdit && !string.IsNullOrEmpty(evento.EventImageUrl))
                    {
                        TryDeleteAsset(evento.EventImageUrl);
                    }

                    evento.EventImageUrl = url;
                }
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            return $"/uploads/{folder}/{fileName}".Replace("\\", "/");
        }

        private void TryDeleteAsset(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var localPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, localPath);

            if (!System.IO.File.Exists(fullPath))
            {
                return;
            }

            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el archivo {File}", fullPath);
            }
        }

        private CreateEventVM MapEventToVm(Evento evento)
        {
            var venue = evento.Venues.FirstOrDefault();
            var classification = evento.Classifications.FirstOrDefault();

            var vm = new CreateEventVM
            {
                Id = evento.Id_Evento,
                Name = evento.Name,
                VenueName = venue?.Name ?? string.Empty,
                City = venue?.City ?? string.Empty,
                State = venue?.State ?? string.Empty,
                EventDateText = evento.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Url = evento.Url,
                Segment = classification?.Segment,
                Genre = classification?.Genre,
                SubGenre = classification?.SubGenre,
                SafeTix = evento.SafeTix,
                ExistingSeatmapUrl = evento.SeatmapUrl,
                // turns eventimageurl into a list since thats how the previous version did it 
                ExistingImages = string.IsNullOrEmpty(evento.EventImageUrl)
                    ? new List<string>()
                    : new List<string> { evento.EventImageUrl },
                IsCancelled = evento.IsCancelled
            };

            vm.Sections = venue?.Sections.Any() == true
                ? venue.Sections.Select(s => new SectionInputVM
                {
                    DisplayName = s.DisplayName,
                    SeatCount = s.SeatCount,
                    SeatsPerRow = s.SeatsPerRow,
                    BasePrice = s.BasePrice,
                    SectionCode = s.SectionCode
                }).ToList()
                : new List<SectionInputVM>
                {
                    new SectionInputVM
                    {
                        DisplayName = "Sección 1",
                        SeatCount = 100,
                        SeatsPerRow = 10,
                        BasePrice = 85m
                    }
                };

            return vm;
        }
    }
}
