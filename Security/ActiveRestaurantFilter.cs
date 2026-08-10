using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;

namespace RestaurantQR.Security
{
    public class ActiveRestaurantFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(
            AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Only applies to logged-in users.
            if (user?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            // SuperAdmin is never blocked by restaurant status.
            if (user.IsInRole("SuperAdmin"))
            {
                return;
            }

            // Only restaurant-bound roles are checked.
            var isRestaurantRole =
                user.IsInRole("RestaurantAdmin") ||
                user.IsInRole("Kitchen");

            if (!isRestaurantRole)
            {
                return;
            }

            var services =
                context.HttpContext.RequestServices;

            var userManager =
                services.GetRequiredService<
                    UserManager<ApplicationUser>>();

            var dbContext =
                services.GetRequiredService<
                    ApplicationDbContext>();

            var appUser =
                await userManager.GetUserAsync(user);

            if (appUser?.RestaurantId == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var restaurantActive =
                await dbContext.Restaurants
                    .AsNoTracking()
                    .AnyAsync(r =>
                        r.Id == appUser.RestaurantId &&
                        r.IsActive);

            if (!restaurantActive)
            {
                // Restaurant disabled by SuperAdmin.
                context.Result = new RedirectToActionResult(
                    "RestaurantDisabled",
                    "Account",
                    new { area = "" });
            }
        }
    }
}