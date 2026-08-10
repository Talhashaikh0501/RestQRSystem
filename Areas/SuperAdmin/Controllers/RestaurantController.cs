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
    public class RestaurantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RestaurantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: SuperAdmin/Restaurant/Index
        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants.ToListAsync();
            return View(restaurants);
        }

        // GET: SuperAdmin/Restaurant/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SuperAdmin/Restaurant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRestaurantViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Start a database transaction to ensure both or neither are created
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 2. Create the Restaurant
                var restaurant = new Restaurant
                {
                    Name = model.Name,
                    Address = model.Address,
                    Phone = model.Phone,
                    Email = model.Email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync(); // This generates the Restaurant.Id

                // 3. Create the Restaurant Admin User
                var adminUser = new ApplicationUser
                {
                    UserName = model.AdminEmail,
                    Email = model.AdminEmail,
                    FullName = model.AdminFullName,
                    RestaurantId = restaurant.Id, // Link to the new restaurant
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, model.AdminPassword);

                if (result.Succeeded)
                {
                    // 4. Assign the RestaurantAdmin role
                    await _userManager.AddToRoleAsync(adminUser, "RestaurantAdmin");

                    // 5. Commit everything to the database
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // If user creation failed, add errors to model and rollback
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    await transaction.RollbackAsync();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
                return View(model);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound();
            }

            restaurant.IsActive = !restaurant.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = restaurant.IsActive
                ? "Restaurant activated successfully."
                : "Restaurant deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}