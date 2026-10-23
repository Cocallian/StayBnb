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
        private static readonly string[] AssignableRoles = { Roles.Guest, Roles.Host, Roles.Admin, Roles.SuperAdmin };

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Collects users, hosts, applications, properties, bookings, payments and recent activity to show the admin overview page.
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
                RecentActivity = await _db.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };
            return View(model);
        }

        // Lists pending host applications that have an applicant and an image so the admin can review them.
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

        // Turns a pending host application into a live property and changes the applicant from guest to host.
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

            if (await _userManager.IsInRoleAsync(user, Roles.Guest))
                await _userManager.RemoveFromRoleAsync(user, Roles.Guest);
            if (!await _userManager.IsInRoleAsync(user, Roles.Host))
                await _userManager.AddToRoleAsync(user, Roles.Host);

            _db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Your host application was approved. You are now a host.",
                Type = NotificationType.HostApplication
            });
            _db.LogActivity(_userManager.GetUserId(User), "HostApplicationApproved", ActivityType.RoleChange);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(HostRequests));
        }

        // Marks a pending host application as rejected and sends the applicant a notification about it.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(int id)
        {
            var application = await _db.HostApplications.FindAsync(id);
            if (application == null) return NotFound();

            if (application.Status == ApplicationStatus.Pending)
            {
                application.Status = ApplicationStatus.Rejected;
                _db.Notifications.Add(new Notification
                {
                    UserId = application.ApplicationUserId,
                    Title = "Your host application was denied.",
                    Type = NotificationType.HostApplication
                });
                _db.LogActivity(_userManager.GetUserId(User), "HostApplicationDenied", ActivityType.HostApplication);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(HostRequests));
        }

        // Lists every booking on the site, newest check-in first, with the guest and host details.
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _db.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Property).ThenInclude(p => p!.Host)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();
            return View(bookings);
        }

        // Lists every property on the site in order of title, with its host, images and reviews.
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

        // Lists every message sent between users, newest first, so the admin can look at them.
        public async Task<IActionResult> Messages()
        {
            var messages = await _db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
            return View(messages);
        }

        // Lists every notification sent to any user, newest first, for the admin to look at.
        public async Task<IActionResult> Notifications()
        {
            var notifications = await _db.Notifications
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(notifications);
        }

        // Lists all recorded activity on the site, such as logins and bookings, with the newest first.
        public async Task<IActionResult> ActivityLogs()
        {
            var logs = await _db.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(logs);
        }

        // Lists all users with their properties, bookings and reviews; only the super admin can open this.
        [Authorize(Roles = Roles.SuperAdmin)]
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users
                .Include(u => u.Properties)
                .Include(u => u.Bookings)
                .Include(u => u.Reviews)
                .ToListAsync();
            return View(users);
        }

        // Replaces a user's roles with the chosen role and tells them if they became a host.
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.SuperAdmin)]
        public async Task<IActionResult> Promote(string id, string role)
        {
            if (!AssignableRoles.Contains(role)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var wasHost = currentRoles.Contains(Roles.Host);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            if (role == Roles.Host && !wasHost)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "You have been promoted to a host.",
                    Type = NotificationType.Success
                });
            }
            _db.LogActivity(_userManager.GetUserId(User), $"RoleChangedTo{role}", ActivityType.RoleChange);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }
    }
}
