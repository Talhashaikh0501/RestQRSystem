using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Models;

namespace RestaurantQR.Hubs
{
    public class OrderHub : Hub
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public OrderHub(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var user =
                    await _userManager.GetUserAsync(Context.User);

                if (user?.RestaurantId != null)
                {
                    await Groups.AddToGroupAsync(
                        Context.ConnectionId,
                        GetRestaurantGroup(
                            user.RestaurantId.Value));
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinOrderGroup(string trackingToken)
        {
            if (string.IsNullOrWhiteSpace(trackingToken))
            {
                return;
            }

            // Make sure this is a real order token before
            // allowing the connection into the group.
            var exists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o =>
                    o.TrackingToken == trackingToken);

            if (!exists)
            {
                return;
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetOrderGroup(trackingToken));
        }

        public static string GetRestaurantGroup(
            int restaurantId)
        {
            return $"restaurant-{restaurantId}";
        }

        public static string GetOrderGroup(
            string trackingToken)
        {
            return $"order-{trackingToken}";
        }
    }
}