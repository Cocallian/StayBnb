using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayBnb.Models;

namespace StayBnb.Controllers
{
    [Authorize(Roles = Roles.Host)]
    public class HostController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
