using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StayBnb.Data;

namespace StayBnb.Controllers
{
    public class HostApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HostApplicationController(ApplicationDbContext context)
        {
            _context = context;
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

            return View();
        }
    }
}
