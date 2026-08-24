using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;
using System.Security.Cryptography;

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
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            var model = new ResetAdminPasswordViewModel
            {
                UserId = admin.Id,
                AdminEmail = admin.Email
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetAdminPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _userManager.FindByIdAsync(model.UserId);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(admin);

            var result =
                await _userManager.ResetPasswordAsync(
                    admin,
                    token,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Admin password changed successfully.";

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public IActionResult ResetPasswordSuccess(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AdminEmail = email;
            ViewBag.TemporaryPassword = password;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(admin, "RestaurantAdmin"))
            {
                return BadRequest("This user is not a Restaurant Admin.");
            }

            var result = await _userManager.DeleteAsync(admin);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    string.Join(
                        " ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string all = upper + lower + digits;

            using var random = RandomNumberGenerator.Create();

            string GetRandomChar(string chars)
            {
                var bytes = new byte[4];
                random.GetBytes(bytes);

                var index =
                    BitConverter.ToUInt32(bytes, 0) %
                    (uint)chars.Length;

                return chars[(int)index].ToString();
            }

            return
                GetRandomChar(upper) +
                GetRandomChar(lower) +
                GetRandomChar(digits) +
                GetRandomChar(all) +
                GetRandomChar(all) +
                GetRandomChar(all) +
                GetRandomChar(all) +
                GetRandomChar(all);
        }
    }
}