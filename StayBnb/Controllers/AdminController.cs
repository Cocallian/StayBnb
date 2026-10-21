using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    [Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                Users = await _db.Users.ToListAsync(),
                Hosts = (await _userManager.GetUsersInRoleAsync(Roles.Host)).ToList(),
                HostApplications = await _db.HostApplications.Include(a => a.ApplicationUser).ToListAsync(),
                Properties = await _db.HostProperties.ToListAsync(),
                Bookings = await _db.Bookings.ToListAsync(),
                Payments = await _db.Payments.ToListAsync(),
                RecentActivity = await _db.ActivityLogs.Include(a => a.User).OrderByDescending(a => a.CreatedAt).Take(5).ToListAsync()
            };
            return View(model);
        }

        // Only the SuperAdmin can manage users
        [Authorize(Roles = Roles.SuperAdmin)]
        public async Task<IActionResult> HostRequests()
        {
            var applications = await _db.HostApplications
                .Include(a => a.ApplicationUser)
                .Include(a => a.Amenities)
                .Where(a => a.Status == ApplicationStatus.Pending
                    && a.ApplicationUser != null
                    && !string.IsNullOrEmpty(a.ImageUrl))
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
            return View(applications);
        }

        // Turns a pending application into a live property and makes the applicant a Host
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _db.HostApplications.Include(a => a.Amenities).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();
            if (application.Status != ApplicationStatus.Pending) return RedirectToAction(nameof(HostRequests));

            var user = await _userManager.FindByIdAsync(application.ApplicationUserId);
            if (user == null) return NotFound();

            var property = new HostProperty
            {
                Title = application.Title,
                Description = application.Description,
                Address = application.Address,
                City = application.City,
                PricePerNight = application.PricePerNight,
                CleaningFee = application.CleaningFee,
                ServiceFee = application.ServiceFee,
                HostId = user.Id,
                IsActive = true,
                Amenities = application.Amenities.ToList(),
                Images = { new PropertyImage { ImageUrl = application.ImageUrl } }
            };
            _db.HostProperties.Add(property);

            application.Status = ApplicationStatus.Approved;
            application.Property = property;
            await _db.SaveChangesAsync();

            if (!await _userManager.IsInRoleAsync(user, Roles.Host))
                await _userManager.AddToRoleAsync(user, Roles.Host);

            return RedirectToAction(nameof(HostRequests));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var application = await _db.HostApplications.FindAsync(id);
            if (application == null) return NotFound();

            if (application.Status == ApplicationStatus.Pending)
            {
                application.Status = ApplicationStatus.Rejected;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(HostRequests));
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Property).ThenInclude(p => p!.Host)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();
            return View(bookings);
        }

        public async Task<IActionResult> Properties()
        {
            var properties = await _db.HostProperties
                .Include(p => p.Host)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .OrderBy(p => p.Title)
                .ToListAsync();
            return View(properties);
        }

        public async Task<IActionResult> Messages()
        {
            var messages = await _db.Messages.Include(m => m.Sender).Include(m => m.Receiver).OrderByDescending(m => m.Timestamp).ToListAsync();
            return View(messages);
        }

        public async Task<IActionResult> Notifications()
        {
            var notifications = await _db.Notifications.Include(n => n.User).OrderByDescending(n => n.CreatedAt).ToListAsync();
            return View(notifications);
        }

        public async Task<IActionResult> ActivityLogs()
        {
            var logs = await _db.ActivityLogs.Include(a => a.User).OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(logs);
        }
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users.Include(u => u.Properties).Include(u => u.Bookings).Include(u => u.Reviews).ToListAsync();
            return View(users);
        }
        [HttpPost]
        public async Task<IActionResult> Promote(string id, string role)
        {
            var r = Roles.Guest;
            switch (role)
                {
                case "Guest":
                    r = Roles.Guest;
                    break;

                case "Admin":
                    r = Roles.Admin;
                    break;

                case "SuperAdmin":
                    r = Roles.SuperAdmin;
                    break;

                case "Host":
                    r = Roles.Host;
                    break;


                    
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, r);
            return RedirectToAction(nameof(Users));
        }
    }
}
