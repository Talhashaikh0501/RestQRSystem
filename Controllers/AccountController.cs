using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Incorrect email or password.");

                return View(model);
            }

            return RedirectToDashboard();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "SuperAdmin" });
            }

            if (User.IsInRole("RestaurantAdmin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }

            if (User.IsInRole("Kitchen"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Kitchen" });
            }

            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> RestaurantDisabled()
        {
            // If they're signed in, sign them out so they
            // return to the login screen afterward.
            if (User.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
            }

            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }

}