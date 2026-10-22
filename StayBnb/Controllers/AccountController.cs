using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StayBnb.Data;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            _db.LogActivity(user!.Id, "UserLoggedIn", ActivityType.Login);
            await _db.SaveChangesAsync();
            return await RedirectByRoleAsync(user, model.ReturnUrl);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // Everyone starts as a Guest; hosts are promoted through the host application.
            await _userManager.AddToRoleAsync(user, Roles.Guest);
            await _signInManager.SignInAsync(user, isPersistent: false);
            _db.LogActivity(user.Id, "UserRegistered", ActivityType.Registration);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _db.LogActivity(User.FindFirstValue(ClaimTypes.NameIdentifier), "UserLoggedOut", ActivityType.Logout);
            await _db.SaveChangesAsync();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // Sends each role to its own landing page (unless the user was heading somewhere specific).
        private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser user, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin) || await _userManager.IsInRoleAsync(user, Roles.Admin))
                return RedirectToAction("Dashboard", "Admin");

            if (await _userManager.IsInRoleAsync(user, Roles.Host))
                return RedirectToAction("Dashboard", "Host");

            return RedirectToAction("Index", "Home");
        }
    }
}
