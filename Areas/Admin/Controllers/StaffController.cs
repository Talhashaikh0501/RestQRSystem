using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "RestaurantAdmin")]
    public class StaffController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        // -----------------------------------------------
        // KITCHEN USERS
        // -----------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var admin = await GetCurrentUserAsync();

            if (admin?.RestaurantId == null)
            {
                return Forbid();
            }

            var restaurantUsers = await _userManager.Users
                .Where(u =>
                    u.RestaurantId == admin.RestaurantId)
                .ToListAsync();

            var kitchenUsers =
                new List<ApplicationUser>();

            foreach (var user in restaurantUsers)
            {
                if (await _userManager.IsInRoleAsync(
                    user,
                    "Kitchen"))
                {
                    kitchenUsers.Add(user);
                }
            }

            return View(
                "~/Areas/Admin/Views/Staff/Index.cshtml",
                kitchenUsers);
        }

        // -----------------------------------------------
        // CREATE
        // -----------------------------------------------

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                "~/Areas/Admin/Views/Staff/Create.cshtml",
                new CreateKitchenUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateKitchenUserViewModel model)
        {
            var admin = await GetCurrentUserAsync();

            if (admin?.RestaurantId == null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Areas/Admin/Views/Staff/Create.cshtml",
                    model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists.");

                return View(
                    "~/Areas/Admin/Views/Staff/Create.cshtml",
                    model);
            }

            var kitchenUser =
                new ApplicationUser
                {
                    UserName =
                        model.Email.Trim(),

                    Email =
                        model.Email.Trim(),

                    FullName =
                        model.FullName.Trim(),

                    EmailConfirmed =
                        true,

                    RestaurantId =
                        admin.RestaurantId
                };

            var result =
                await _userManager.CreateAsync(
                    kitchenUser,
                    model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(
                    "~/Areas/Admin/Views/Staff/Create.cshtml",
                    model);
            }

            var roleResult =
                await _userManager.AddToRoleAsync(
                    kitchenUser,
                    "Kitchen");

            if (!roleResult.Succeeded)
            {
                // Prevent a half-created staff account.
                await _userManager.DeleteAsync(kitchenUser);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(
                    "~/Areas/Admin/Views/Staff/Create.cshtml",
                    model);
            }

            TempData["Success"] =
                "Kitchen user created successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}