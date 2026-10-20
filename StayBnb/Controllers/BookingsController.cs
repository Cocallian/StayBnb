using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    public class BookingsController : Controller
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public BookingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Create(int id)
        {
            var property = await _context.HostProperties
                .Include(p => p.Images)
                .Include(p => p.Amenities)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (property == null) return NotFound();
            return View(property);
        }

        [Authorize(Roles = Roles.Guest)]
        public async Task<IActionResult> CheckIn(int id)
        {
            var property = await LoadPropertyAsync(id);
            if (property == null) return NotFound();

            return View(new CheckInViewModel { PropertyId = id, Property = property });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Guest)]
        public async Task<IActionResult> CheckIn(CheckInViewModel model)
        {
            var property = await LoadPropertyAsync(model.PropertyId);
            if (property == null) return NotFound();
            model.Property = property;

            if (model.CheckInDate.HasValue && model.CheckOutDate.HasValue)
            {
                var checkIn = model.CheckInDate.Value.Date;
                var checkOut = model.CheckOutDate.Value.Date;

                if (checkIn < DateTime.Today)
                    ModelState.AddModelError(nameof(model.CheckInDate), "Check-in cannot be in the past.");
                if (checkOut <= checkIn)
                    ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out must be after check-in.");
                else if (await _context.Bookings.AnyAsync(b => b.PropertyId == property.Id
                         && b.Status == BookingStatus.Approved
                         && b.CheckInDate < checkOut && b.CheckOutDate > checkIn))
                    ModelState.AddModelError(string.Empty, "Those dates are not available for this property.");
            }

            if (model.Document != null)
            {
                var extension = Path.GetExtension(model.Document.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                    ModelState.AddModelError(nameof(model.Document), "Only PDF, JPG or PNG files are allowed.");
                else if (model.Document.Length == 0 || model.Document.Length > MaxFileSize)
                    ModelState.AddModelError(nameof(model.Document), "The file must be between 1 byte and 5 MB.");
            }

            if (!ModelState.IsValid) return View(model);

            var guestId = _userManager.GetUserId(User)!;
            var nights = (model.CheckOutDate!.Value.Date - model.CheckInDate!.Value.Date).Days;

            // Save the uploaded document under wwwroot/uploads/documents with a generated name
            var folder = Path.Combine(_env.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(folder);
            var storedName = Guid.NewGuid() + Path.GetExtension(model.Document!.FileName).ToLowerInvariant();
            await using (var stream = System.IO.File.Create(Path.Combine(folder, storedName)))
            {
                await model.Document.CopyToAsync(stream);
            }

            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = guestId,
                CheckInDate = model.CheckInDate.Value.Date,
                CheckOutDate = model.CheckOutDate.Value.Date,
                NumberOfGuests = model.NumberOfGuests,
                TotalPrice = property.PricePerNight * nights + property.CleaningFee + property.ServiceFee,
                Status = BookingStatus.Pending,
                GuestCheckIn = new GuestCheckIn
                {
                    CheckInProcessId = property.CheckInProcess!.Id,
                    Status = CheckInStatus.Submitted,
                    Documents =
                    {
                        new GuestDocument
                        {
                            DocumentType = model.DocumentType,
                            FileName = storedName,
                            Status = DocumentStatus.Pending
                        }
                    }
                }
            };
            _context.Bookings.Add(booking);

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = guestId,
                Action = "BookingRequested",
                ActivityType = ActivityType.Booking
            });
            _context.Notifications.Add(new Notification
            {
                UserId = property.HostId,
                Title = "New booking request",
                Type = NotificationType.Booking
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = Roles.Guest)]
        public async Task<IActionResult> Index()
        {
            var guestId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Property)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();
            return View(bookings);
        }

        // Property with everything the check-in page displays; null when missing or inactive
        private async Task<HostProperty?> LoadPropertyAsync(int id)
        {
            var property = await _context.HostProperties
                .Include(p => p.Images)
                .Include(p => p.Amenities)
                .Include(p => p.CheckInProcess)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (property == null) return null;

            // Every property needs a check-in process for the guest check-in to attach to
            if (property.CheckInProcess == null)
            {
                property.CheckInProcess = new CheckInProcess { Title = $"Check-in for {property.Title}" };
                await _context.SaveChangesAsync();
            }
            return property;
        }
    }
}
