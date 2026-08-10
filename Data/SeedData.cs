using Microsoft.AspNetCore.Identity;
using RestaurantQR.Models;

namespace RestaurantQR.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // ----------------------------
            // Create roles
            // ----------------------------

            string[] roles =
            {
                "SuperAdmin",
                "RestaurantAdmin",
                "Kitchen"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // ----------------------------
            // Create initial SuperAdmin
            // ----------------------------

            const string email = "superadmin@restaurantqr.com";
            const string password = "Admin123";

            var superAdmin =
                await userManager.FindByEmailAsync(email);

            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "Super Admin",
                    EmailConfirmed = true,

                    // SuperAdmin does not belong to a restaurant.
                    RestaurantId = null
                };

                var result = await userManager.CreateAsync(
                    superAdmin,
                    password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to create SuperAdmin: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(
                    superAdmin,
                    "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(
                    superAdmin,
                    "SuperAdmin");
            }
        }
    }
}