using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;

namespace RestaurantQR.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "RestaurantAdmin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.RestaurantId == null)
            {
                return Forbid();
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == user.RestaurantId);

            if (restaurant == null)
            {
                return NotFound();
            }

            return View(restaurant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMenuOnlyMode()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || user.RestaurantId == null)
            {
                return Forbid();
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == user.RestaurantId);

            if (restaurant == null)
            {
                return NotFound();
            }

            restaurant.MenuOnlyMode = !restaurant.MenuOnlyMode;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}