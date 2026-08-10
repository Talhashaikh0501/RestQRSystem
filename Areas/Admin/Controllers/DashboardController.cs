using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantQR.Models;

namespace RestaurantQR.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "RestaurantAdmin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.RestaurantId == null)
            {
                return Forbid();
            }

            return View(user);
        }
    }
}