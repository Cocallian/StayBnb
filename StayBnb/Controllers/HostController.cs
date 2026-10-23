using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    [Authorize(Roles = Roles.Host)]
    public class HostController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public HostController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        private string HostId => _userManager.GetUserId(User)!;

        private IQueryable<HostProperty> MyProperties => _db.HostProperties.Where(p => p.HostId == HostId);

        private IQueryable<Booking> MyBookings => _db.Bookings.Where(b => b.Property!.HostId == HostId);

        // Works out the host's totals, earnings, average rating and recent bookings for the dashboard page.
        public async Task<IActionResult> Dashboard()
        {
            var bookings = await MyBookings.Include(b => b.Payment).ToListAsync();
            var ratings = await _db.Reviews.Where(r => r.Property!.HostId == HostId).Select(r => r.Rating).ToListAsync();

            var model = new HostDashboardViewModel
            {
                TotalProperties = await MyProperties.CountAsync(),
                PendingRequests = bookings.Count(b => b.Status == BookingStatus.Pending),
                UpcomingBookings = bookings.Count(b => b.Status == BookingStatus.Approved && b.CheckInDate >= DateTime.Today),
                Earnings = bookings.Where(b => b.Payment?.Status == PaymentStatus.Paid).Sum(b => b.Payment!.Amount),
                AverageRating = ratings.Count == 0 ? 0 : ratings.Average(),
                ReviewCount = ratings.Count,
                RecentBookings = await MyBookings
                    .Include(b => b.Guest).Include(b => b.Property)
                    .OrderByDescending(b => b.CheckInDate).Take(5).ToListAsync()
            };
            return View(model);
        }

        // Lists the logged-in host's own properties in order of title, with their images and reviews.
        public async Task<IActionResult> Properties()
        {
            var properties = await MyProperties
                .Include(p => p.Images).Include(p => p.Reviews)
                .OrderBy(p => p.Title).ToListAsync();
            return View(properties);
        }

        // Shows an empty form for adding a new property, with the list of amenities to pick from.
        public async Task<IActionResult> AddProperty()
        {
            await LoadAmenitiesAsync();
            return View(new HostProperty { IsActive = true });
        }

        // Checks the form, links the chosen amenities and image, and saves a new property for this host.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProperty(HostProperty model, string? imageUrl, int[]? amenityIds)
        {
            ModelState.Remove(nameof(HostProperty.HostId));
            if (!ModelState.IsValid)
            {
                await LoadAmenitiesAsync(amenityIds);
                return View(model);
            }

            model.Id = 0;
            model.HostId = HostId;
            if (amenityIds != null)
                model.Amenities = await _db.Amenities.Where(a => amenityIds.Contains(a.Id)).ToListAsync();
            if (!string.IsNullOrWhiteSpace(imageUrl))
                model.Images.Add(new PropertyImage { ImageUrl = imageUrl.Trim() });

            _db.HostProperties.Add(model);
            _db.LogActivity(HostId, "PropertyAdded", ActivityType.Property);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Properties));
        }

        // Finds one of the host's own properties and shows it in the edit form.
        public async Task<IActionResult> EditProperty(int id)
        {
            var property = await MyProperties.Include(p => p.Images).Include(p => p.Amenities).FirstOrDefaultAsync(p => p.Id == id);
            if (property == null) return NotFound();
            await LoadAmenitiesAsync(property.Amenities.Select(a => a.Id));
            return View(property);
        }

        // Saves the edited property details, and tells people who wishlisted it when it becomes available to book.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProperty(int id, HostProperty model, string? imageUrl, int[]? amenityIds)
        {
            var property = await MyProperties.Include(p => p.Images).Include(p => p.Amenities).FirstOrDefaultAsync(p => p.Id == id);
            if (property == null) return NotFound();

            ModelState.Remove(nameof(HostProperty.HostId));
            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.Images = property.Images;
                await LoadAmenitiesAsync(amenityIds);
                return View(model);
            }

            var becameActive = !property.IsActive && model.IsActive;

            property.Title = model.Title;
            property.Description = model.Description;
            property.Address = model.Address;
            property.City = model.City;
            property.PricePerNight = model.PricePerNight;
            property.CleaningFee = model.CleaningFee;
            property.ServiceFee = model.ServiceFee;
            property.IsActive = model.IsActive;
            property.Amenities = await _db.Amenities.Where(a => (amenityIds ?? Array.Empty<int>()).Contains(a.Id)).ToListAsync();

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var image = property.Images.FirstOrDefault();
                if (image == null) property.Images.Add(new PropertyImage { ImageUrl = imageUrl.Trim() });
                else image.ImageUrl = imageUrl.Trim();
            }

            if (becameActive)
            {
                var title = $"{property.Title} is now available to book";
                var wishers = await _db.WishlistItems
                    .Where(w => w.PropertyId == property.Id && w.UserId != HostId)
                    .Select(w => w.UserId).Distinct().ToListAsync();
                foreach (var userId in wishers)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Title = title.Length > 100 ? title.Substring(0, 100) : title,
                        Type = NotificationType.Booking
                    });
                }
            }

            _db.LogActivity(HostId, "PropertyUpdated", ActivityType.Property);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Properties));
        }

        // Lists the pending booking requests for the host's properties, with the earliest check-in date first.
        public async Task<IActionResult> BookingRequests()
        {
            var bookings = await MyBookings
                .Include(b => b.Guest).Include(b => b.Property)
                .Where(b => b.Status == BookingStatus.Pending)
                .OrderBy(b => b.CheckInDate).ToListAsync();
            return View(bookings);
        }

        // Approves or declines a pending booking, creates the payment if approved and notifies the guest.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToBooking(int id, bool approve)
        {
            var booking = await MyBookings.Include(b => b.Property).Include(b => b.Payment).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();
            if (booking.Status != BookingStatus.Pending) return RedirectToAction(nameof(BookingRequests));

            booking.Status = approve ? BookingStatus.Approved : BookingStatus.Rejected;

            if (approve && booking.Payment == null)
                booking.Payment = new Payment { Amount = booking.TotalPrice, Status = PaymentStatus.Pending };

            _db.Notifications.Add(new Notification
            {
                UserId = booking.GuestId,
                Title = $"Your booking for {booking.Property?.Title} was {(approve ? "approved" : "declined")}",
                Type = NotificationType.Booking
            });
            _db.LogActivity(HostId, approve ? "BookingApproved" : "BookingDeclined", ActivityType.Booking);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(BookingRequests));
        }

        // Lists the guest check-ins and their documents for the host's properties, newest first.
        public async Task<IActionResult> CheckIns()
        {
            var checkIns = await _db.GuestCheckIns
                .Include(c => c.Documents)
                .Include(c => c.Booking!).ThenInclude(b => b.Guest)
                .Include(c => c.Booking!).ThenInclude(b => b.Property)
                .Where(c => c.Booking!.Property!.HostId == HostId)
                .OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(checkIns);
        }

        // Verifies or rejects a guest check-in, marks the booking checked in and the payment paid.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewCheckIn(int id, bool verify)
        {
            var checkIn = await _db.GuestCheckIns
                .Include(c => c.Booking!).ThenInclude(b => b.Property)
                .Include(c => c.Booking!).ThenInclude(b => b.Payment)
                .FirstOrDefaultAsync(c => c.Id == id && c.Booking!.Property!.HostId == HostId);
            if (checkIn == null) return NotFound();

            checkIn.Status = verify ? CheckInStatus.Verified : CheckInStatus.Rejected;

            if (verify && checkIn.Booking!.Status == BookingStatus.Approved)
                checkIn.Booking.Status = BookingStatus.CheckedIn;

            if (verify && checkIn.Booking!.Payment?.Status == PaymentStatus.Pending)
                checkIn.Booking.Payment.Status = PaymentStatus.Paid;

            _db.Notifications.Add(new Notification
            {
                UserId = checkIn.Booking!.GuestId,
                Title = $"Your check-in for {checkIn.Booking.Property?.Title} was {(verify ? "verified" : "rejected")}",
                Type = NotificationType.CheckIn
            });
            _db.LogActivity(HostId, verify ? "CheckInVerified" : "CheckInRejected", ActivityType.CheckIn);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(CheckIns));
        }

        // Lets a host open a guest's uploaded document, but only for the host's own properties.
        public async Task<IActionResult> Document(int id)
        {
            var document = await _db.GuestDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.GuestCheckIn!.Booking!.Property!.HostId == HostId);
            if (document == null) return NotFound();

            var fileName = Path.GetFileName(document.FileName);
            var path = Path.Combine(_env.ContentRootPath, "PrivateUploads", "documents", fileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            var contentTypes = new Dictionary<string, string>
            {
                [".pdf"] = "application/pdf",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png"
            };
            var contentType = contentTypes.GetValueOrDefault(Path.GetExtension(fileName).ToLowerInvariant(), "application/octet-stream");
            return PhysicalFile(path, contentType);
        }

        // Lists the host's bookings that have a payment, newest payment first, for the earnings page.
        public async Task<IActionResult> Earnings()
        {
            var bookings = await MyBookings
                .Include(b => b.Guest).Include(b => b.Property).Include(b => b.Payment)
                .Where(b => b.Payment != null)
                .OrderByDescending(b => b.Payment!.CreatedAt).ToListAsync();
            return View(bookings);
        }

        // Lists the reviews written about the host's properties, with the newest review shown first.
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _db.Reviews
                .Include(r => r.Reviewer).Include(r => r.Property)
                .Where(r => r.Property!.HostId == HostId)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(reviews);
        }

        // Loads all amenities and the chosen ones into the ViewBag so the form can show its checkboxes.
        private async Task LoadAmenitiesAsync(IEnumerable<int>? selected = null)
        {
            ViewBag.Amenities = await _db.Amenities.OrderBy(a => a.Name).ToListAsync();
            ViewBag.SelectedAmenities = selected?.ToList() ?? new List<int>();
        }
    }
}
