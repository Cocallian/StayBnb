using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    public class HostApplicationController : Controller
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageSize = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HostApplicationController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Not logged in -> login, no application yet -> apply, otherwise show the status.
        public async Task<IActionResult> Status()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var application = await _context.HostApplications
                .Where(a => a.ApplicationUserId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .FirstOrDefaultAsync();
            if (application == null)
                return RedirectToAction(nameof(Apply));

            return View(application);
        }

        [Authorize]
        public async Task<IActionResult> Apply()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (await _context.HostApplications.AnyAsync(a => a.ApplicationUserId == userId))
                return RedirectToAction(nameof(Status));

            await LoadAmenitiesAsync();
            return View(new HostApplication());
        }

        // Binds only the fields the applicant fills in; the user, status and date are set here
        [HttpPost, ValidateAntiForgeryToken, Authorize]
        public async Task<IActionResult> Apply(
            [Bind(nameof(HostApplication.Title), nameof(HostApplication.Description), nameof(HostApplication.PricePerNight),
                  nameof(HostApplication.CleaningFee), nameof(HostApplication.ServiceFee), nameof(HostApplication.Address),
                  nameof(HostApplication.City))] HostApplication application,
            IFormFile? image,
            IFormFile? document,
            List<int> amenityIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (await _context.HostApplications.AnyAsync(a => a.ApplicationUserId == userId))
                return RedirectToAction(nameof(Status));

            // Set by the server, not posted
            ModelState.Remove(nameof(HostApplication.ApplicationUserId));

            if (image == null || image.Length == 0)
                ModelState.AddModelError("image", "An application cannot be submitted without an image.");
            else
            {
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                    ModelState.AddModelError("image", "Only JPG, PNG or WEBP images are allowed.");
                else if (image.Length > MaxImageSize)
                    ModelState.AddModelError("image", "The image must be 5 MB or smaller.");
            }

            if (document == null || document.Length == 0)
                ModelState.AddModelError("document", "An application cannot be submitted without a document.");
            else if (Path.GetExtension(document.FileName).ToLowerInvariant() != ".pdf")
                ModelState.AddModelError("document", "Only PDF documents are allowed.");
            else if (document.Length > MaxImageSize)
                ModelState.AddModelError("document", "The document must be 5 MB or smaller.");

            if (!ModelState.IsValid)
            {
                await LoadAmenitiesAsync(amenityIds);
                return View(application);
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads", "properties");
            Directory.CreateDirectory(folder);
            var storedName = Guid.NewGuid() + Path.GetExtension(image!.FileName).ToLowerInvariant();
            await using (var stream = System.IO.File.Create(Path.Combine(folder, storedName)))
            {
                await image.CopyToAsync(stream);
            }

            var documentFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(documentFolder);
            var documentName = Guid.NewGuid() + ".pdf";
            await using (var stream = System.IO.File.Create(Path.Combine(documentFolder, documentName)))
            {
                await document!.CopyToAsync(stream);
            }

            application.ApplicationUserId = userId;
            application.DocumentUrl = "/uploads/documents/" + documentName;
            application.ImageUrl = "/uploads/properties/" + storedName;
            application.Amenities = await _context.Amenities.Where(a => amenityIds.Contains(a.Id)).ToListAsync();

            _context.HostApplications.Add(application);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Status));
        }

        private async Task LoadAmenitiesAsync(IEnumerable<int>? selected = null)
        {
            ViewBag.Amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            ViewBag.SelectedAmenities = selected?.ToList() ?? new List<int>();
        }
    }
}
