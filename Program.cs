using RestaurantQR.Security;
using RestaurantQR.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Data;
using RestaurantQR.Hubs;
using RestaurantQR.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------
// Database
// -------------------------------------------------

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable("TDA_EFMigrationsHistory");
        }));


// -------------------------------------------------
// Identity
// -------------------------------------------------

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


// -------------------------------------------------
// MVC
// -------------------------------------------------

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ActiveRestaurantFilter>();
});


// -------------------------------------------------
// SignalR
// -------------------------------------------------

builder.Services.AddSignalR();


// -------------------------------------------------
// Application Services
// -------------------------------------------------

builder.Services.AddScoped<QRCodeService>();


// -------------------------------------------------
// EMAIL SERVICE
// -------------------------------------------------

builder.Services.AddScoped<IEmailService, EmailService>();


// -------------------------------------------------
// AUTOMATIC SUBSCRIPTION REMINDER SERVICE
// -------------------------------------------------
//
// Checks subscriptions every hour.
//
// Sends:
// 7 day reminder
// 6 day reminder
// 5 day reminder
// 4 day reminder
// 3 day reminder
// 2 day reminder
// 1 day reminder
//
// After expiry:
// Subscription -> Expired
// Restaurant -> Inactive
// Final expired email
// -------------------------------------------------

builder.Services.AddHostedService<SubscriptionReminderService>();


// -------------------------------------------------
// Session
// -------------------------------------------------

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(60);

    options.Cookie.HttpOnly =
        true;

    options.Cookie.IsEssential =
        true;
});


// -------------------------------------------------
// Server URL
// -------------------------------------------------

builder.WebHost.UseUrls(
    "http://0.0.0.0:5088"
);

var app = builder.Build();


// -------------------------------------------------
// HTTP Pipeline
// -------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error"
    );

    app.UseHsts();
}


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();


// -------------------------------------------------
// Area Routing
// -------------------------------------------------

app.MapControllerRoute(
    name: "areas",
    pattern:
        "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");


// -------------------------------------------------
// Default Routing
// -------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");


// -------------------------------------------------
// SignalR
// -------------------------------------------------

app.MapHub<OrderHub>(
    "/orderHub"
);


// -------------------------------------------------
// Seed Roles + SuperAdmin
// -------------------------------------------------

using (var scope =
    app.Services.CreateScope())
{
    await SeedData.InitializeAsync(
        scope.ServiceProvider
    );
}


// -------------------------------------------------

app.Run();