using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;
using RestaurantQR.ViewModels;

namespace RestaurantQR.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;


        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }


        // =========================================================
        // LOGIN - GET
        // =========================================================

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


        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // =====================================================
            // FIND USER
            // =====================================================

            var user =
                await _userManager.FindByEmailAsync(
                    model.Email
                );


            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password."
                );

                return View(model);
            }


            // =====================================================
            // CHECK PASSWORD FIRST
            // =====================================================
            //
            // We verify the password BEFORE checking subscription
            // status.
            //
            // This prevents someone from discovering whether an
            // email address belongs to a pending restaurant account
            // without knowing its password.
            // =====================================================

            var passwordCheck =
                await _signInManager
                    .CheckPasswordSignInAsync(
                        user,
                        model.Password,
                        lockoutOnFailure: false
                    );


            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Incorrect email or password."
                );

                return View(model);
            }


            // =====================================================
            // RESTAURANT ADMIN APPROVAL CHECK
            // =====================================================

            var isRestaurantAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "RestaurantAdmin"
                );


            if (
                isRestaurantAdmin &&
                user.RestaurantId.HasValue
            )
            {
                var restaurantId =
                    user.RestaurantId.Value;


                // =================================================
                // FIND PAID SUBSCRIPTION WAITING FOR APPROVAL
                // =================================================

                var pendingApproval =
                    await _context.Subscriptions

                        .AsNoTracking()

                        .AnyAsync(s =>
                            s.RestaurantId ==
                                restaurantId
                            &&
                            s.PaymentStatus ==
                                PaymentStatus.Paid
                            &&
                            s.Status ==
                                SubscriptionStatus.Pending
                        );


                if (pendingApproval)
                {
                    ModelState.AddModelError(
                        string.Empty,

                        "Payment received successfully. " +
                        "Your RestaurantQR account is currently " +
                        "awaiting Super Admin approval. " +
                        "You will receive an email once your " +
                        "account has been activated."
                    );


                    return View(model);
                }
            }


            // =====================================================
            // NORMAL LOGIN
            // =====================================================
            //
            // At this point:
            //
            // - Password is correct
            // - User is not waiting for SuperAdmin approval
            //
            // Continue with the normal existing Identity login.
            // =====================================================

            var result =
                await _signInManager
                    .PasswordSignInAsync(
                        user,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false
                    );


            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Incorrect email or password."
                );

                return View(model);
            }


            return RedirectToDashboard();
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager
                .SignOutAsync();


            return RedirectToAction(
                "Login",
                "Account"
            );
        }


        // =========================================================
        // ROLE DASHBOARD REDIRECTION
        // =========================================================

        private IActionResult RedirectToDashboard()
        {
            // =====================================================
            // SUPERADMIN
            // =====================================================

            if (User.IsInRole("SuperAdmin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new
                    {
                        area =
                            "SuperAdmin"
                    }
                );
            }


            // =====================================================
            // RESTAURANT ADMIN
            // =====================================================

            if (User.IsInRole("RestaurantAdmin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new
                    {
                        area =
                            "Admin"
                    }
                );
            }


            // =====================================================
            // KITCHEN
            // =====================================================

            if (User.IsInRole("Kitchen"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new
                    {
                        area =
                            "Kitchen"
                    }
                );
            }


            return RedirectToAction(
                "Index",
                "Home"
            );
        }


        // =========================================================
        // RESTAURANT DISABLED
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> RestaurantDisabled()
        {
            // =====================================================
            // SIGN OUT DISABLED RESTAURANT USER
            // =====================================================
            //
            // ActiveRestaurantFilter sends restaurant-bound users
            // here when their Restaurant.IsActive is false.
            // =====================================================

            if (User.Identity?.IsAuthenticated == true)
            {
                await _signInManager
                    .SignOutAsync();
            }


            return View();
        }


        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}