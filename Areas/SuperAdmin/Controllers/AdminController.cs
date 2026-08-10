using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("RestaurantAdmin");

            var result = new List<RestaurantAdminListViewModel>();

            foreach (var user in adminUsers)
            {
                var restaurant = await _context.Restaurants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == user.RestaurantId);

                result.Add(new RestaurantAdminListViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName ?? "Unknown",
                    Email = user.Email ?? "",
                    RestaurantId = user.RestaurantId ?? 0,
                    RestaurantName = restaurant?.Name ?? "Unassigned",
                    RestaurantIsActive = restaurant?.IsActive ?? false
                });
            }

            return View(
                "~/Areas/SuperAdmin/Views/Admin/Index.cshtml",
                result);
        }
    }
}