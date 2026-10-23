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

        // Shows the login page and remembers where the user wanted to go after signing in.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // Checks the email and password, logs the sign in, then sends the user to the right page.
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

        // Shows an empty registration page where a new user can create an account.
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // Creates a new guest account from the form, signs them in and records it in the log.
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

            await _userManager.AddToRoleAsync(user, Roles.Guest);
            await _signInManager.SignInAsync(user, isPersistent: false);
            _db.LogActivity(user.Id, "UserRegistered", ActivityType.Registration);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        // Records that the user left, signs them out and sends them back to the home page.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _db.LogActivity(User.FindFirstValue(ClaimTypes.NameIdentifier), "UserLoggedOut", ActivityType.Logout);
            await _db.SaveChangesAsync();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Shows a page telling the user they are not allowed to open the page they asked for.
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Sends admins, hosts and guests to their own start page, or back to the page they wanted.
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
