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

        private const string SuperAdminAnalyticsGroup =
            "superadmin-analytics";


        public OrderHub(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }


        // =========================================================
        // WHEN A SIGNALR CLIENT CONNECTS
        // =========================================================

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var user =
                    await _userManager.GetUserAsync(Context.User);

                if (user != null)
                {
                    // =================================================
                    // RESTAURANT USERS
                    // =================================================
                    //
                    // Admin / Kitchen users continue joining their
                    // restaurant-specific group exactly as before.
                    // =================================================

                    if (user.RestaurantId != null)
                    {
                        await Groups.AddToGroupAsync(
                            Context.ConnectionId,
                            GetRestaurantGroup(
                                user.RestaurantId.Value
                            )
                        );
                    }


                    // =================================================
                    // SUPER ADMIN
                    // =================================================
                    //
                    // If the connected authenticated user is a
                    // SuperAdmin, automatically add that connection
                    // to the global RestaurantQR analytics group.
                    // =================================================

                    if (await _userManager.IsInRoleAsync(
                        user,
                        "SuperAdmin"
                    ))
                    {
                        await Groups.AddToGroupAsync(
                            Context.ConnectionId,
                            GetSuperAdminAnalyticsGroup()
                        );
                    }
                }
            }


            await base.OnConnectedAsync();
        }


        // =========================================================
        // CUSTOMER ORDER TRACKING GROUP
        // =========================================================

        public async Task JoinOrderGroup(
            string trackingToken)
        {
            if (string.IsNullOrWhiteSpace(
                trackingToken))
            {
                return;
            }


            // Make sure this is a real order token before
            // allowing the connection into the group.

            var exists =
                await _context.Orders
                    .AsNoTracking()
                    .AnyAsync(
                        o =>
                            o.TrackingToken
                            == trackingToken
                    );


            if (!exists)
            {
                return;
            }


            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetOrderGroup(
                    trackingToken
                )
            );
        }


        // =========================================================
        // SUPER ADMIN ANALYTICS GROUP
        // =========================================================
        //
        // The SuperAdmin dashboard also explicitly calls this
        // method after connecting/reconnecting.
        //
        // We verify the user's role before adding the connection.
        // A normal restaurant user/customer cannot join this group.
        // =========================================================

        public async Task JoinSuperAdminAnalyticsGroup()
        {
            if (
                Context.User?.Identity?.IsAuthenticated
                != true
            )
            {
                return;
            }


            var user =
                await _userManager.GetUserAsync(
                    Context.User
                );


            if (user == null)
            {
                return;
            }


            var isSuperAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "SuperAdmin"
                );


            if (!isSuperAdmin)
            {
                return;
            }


            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetSuperAdminAnalyticsGroup()
            );
        }


        // =========================================================
        // RESTAURANT GROUP NAME
        // =========================================================

        public static string GetRestaurantGroup(
            int restaurantId)
        {
            return $"restaurant-{restaurantId}";
        }


        // =========================================================
        // CUSTOMER ORDER GROUP NAME
        // =========================================================

        public static string GetOrderGroup(
            string trackingToken)
        {
            return $"order-{trackingToken}";
        }


        // =========================================================
        // SUPER ADMIN ANALYTICS GROUP NAME
        // =========================================================

        public static string
            GetSuperAdminAnalyticsGroup()
        {
            return SuperAdminAnalyticsGroup;
        }
    }
}