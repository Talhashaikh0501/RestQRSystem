using Microsoft.AspNetCore.Identity;
using RestaurantQR.Models;

namespace RestaurantQR.Data
{
    public static class SeedData
    {
        private const string SuperAdminEmail = "superadmin@restaurantqr.com";
        private const string PasswordResetEnvironmentVariable =
            "RESTQR_SUPERADMIN_RESET_PASSWORD";

        public static async Task InitializeAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create application roles when they do not already exist.
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
                    var roleResult =
                        await roleManager.CreateAsync(new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(
                            ", ",
                            roleResult.Errors.Select(e => e.Description));

                        throw new Exception(
                            $"Failed to create role '{role}': {errors}");
                    }
                }
            }

            // A password is supplied only for an explicitly requested
            // one-time SuperAdmin creation or password reset.
            var requestedPassword =
                Environment.GetEnvironmentVariable(
                    PasswordResetEnvironmentVariable);

            var superAdmin =
                await userManager.FindByEmailAsync(SuperAdminEmail);

            if (superAdmin == null)
            {
                if (string.IsNullOrWhiteSpace(requestedPassword))
                {
                    throw new InvalidOperationException(
                        $"The SuperAdmin account does not exist. Set the " +
                        $"{PasswordResetEnvironmentVariable} environment variable " +
                        "to the intended password before starting the application.");
                }

                superAdmin = new ApplicationUser
                {
                    UserName = SuperAdminEmail,
                    Email = SuperAdminEmail,
                    FullName = "Super Admin",
                    EmailConfirmed = true,

                    // SuperAdmin does not belong to a restaurant.
                    RestaurantId = null
                };

                var createResult =
                    await userManager.CreateAsync(
                        superAdmin,
                        requestedPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        createResult.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to create SuperAdmin: {errors}");
                }

                Console.WriteLine("SuperAdmin account created successfully.");
            }
            else if (!string.IsNullOrWhiteSpace(requestedPassword))
            {
                var resetToken =
                    await userManager.GeneratePasswordResetTokenAsync(
                        superAdmin);

                var resetResult =
                    await userManager.ResetPasswordAsync(
                        superAdmin,
                        resetToken,
                        requestedPassword);

                if (!resetResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        resetResult.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to reset SuperAdmin password: {errors}");
                }

                Console.WriteLine(
                    "SuperAdmin password reset successfully.");
            }

            // Remove the reset value from this application process after
            // a successful create/reset so normal restarts do not reuse it.
            if (!string.IsNullOrWhiteSpace(requestedPassword))
            {
                Environment.SetEnvironmentVariable(
                    PasswordResetEnvironmentVariable,
                    null,
                    EnvironmentVariableTarget.Process);
            }

            // Ensure the account has the SuperAdmin role.
            if (!await userManager.IsInRoleAsync(
                    superAdmin,
                    "SuperAdmin"))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(
                        superAdmin,
                        "SuperAdmin");

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        roleResult.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to assign SuperAdmin role: {errors}");
                }
            }
        }
    }
}