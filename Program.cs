using FiveThreeOneTracker.Components;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Security.Claims;

const string AdminRole  = "Admin";

var builder = WebApplication.CreateBuilder(args);

// PORT — Digital Ocean injects a PORT env var; bind to it so traffic is routed correctly.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Trust the X-Forwarded-Proto header from Digital Ocean's load balancer so
// OAuth redirect URIs are built with https:// instead of http://.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var adminEmail = builder.Configuration["App:AdminEmail"] ?? "";

// Database — use PostgreSQL when DATABASE_URL is set (production / Digital Ocean),
// otherwise fall back to SQLite for local development.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var pgConnection = FiveThreeOneTracker.Data.AppDbContextFactory.ConvertDatabaseUrl(databaseUrl);
        options.UseNpgsql(pgConnection);
    }
    else
    {
        var dbPath = Path.Combine(builder.Environment.ContentRootPath, "fivethreeone.db");
        var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? $"Data Source={dbPath}";
        if (sqliteConnection == "Data Source=fivethreeone.db")
            sqliteConnection = $"Data Source={dbPath}";
        options.UseSqlite(sqliteConnection);
    }
});

// Data Protection \u2014 persist keys to DB so they survive container restarts on DO App Platform.
// Without this, every redeploy generates new keys and invalidates all auth cookies/OAuth state.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("FiveThreeOneTracker");

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Google OAuth
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]     ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";

        // Block disabled users immediately after Google validates the ticket
        options.Events.OnTicketReceived = async ctx =>
        {
            var userManager = ctx.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();
            var email = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (email is not null)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user is not null && !user.IsEnabled)
                {
                    ctx.Fail("Your account has been disabled. Contact the administrator.");
                }
            }
        };
    });

// Cookie paths for Blazor Server
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath  = "/login";
    options.LogoutPath = "/logout";
});

// Blazor auth
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Application services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserInitService, UserInitService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IBbbMappingService, BbbMappingService>();
builder.Services.AddScoped<IWeightCalculator, WeightCalculator>();
builder.Services.AddScoped<ICycleService, CycleService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IAccessoryService, AccessoryService>();
builder.Services.AddScoped<ILiftService, LiftService>();
builder.Services.AddScoped<IPlateCalculatorService, PlateCalculatorService>();
builder.Services.AddScoped<IPplProgramService, PplProgramService>();
builder.Services.AddScoped<IPplSessionService, PplSessionService>();
builder.Services.AddScoped<IPplProgressionService, PplProgressionService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var app = builder.Build();

// Startup seed: migrate, enable existing users, seed Admin role + owner
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await db.Database.MigrateAsync();

    // Ensure all existing users are enabled (fixes rows defaulted to 0 by migration)
    var disabledUsers = db.Users.Where(u => !u.IsEnabled).ToList();
    foreach (var u in disabledUsers) { u.IsEnabled = true; }
    await db.SaveChangesAsync();

    // Seed Admin role
    if (!await roleManager.RoleExistsAsync(AdminRole))
        await roleManager.CreateAsync(new IdentityRole(AdminRole));

    // Assign Admin role to owner if they exist
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is not null && !await userManager.IsInRoleAsync(adminUser, AdminRole))
        await userManager.AddToRoleAsync(adminUser, AdminRole);
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Auth endpoints

// 1. Kick off the Google OAuth flow, storing the final destination in RedirectUri → /auth/callback
app.MapGet("/challenge/{provider}", async (string provider, string? returnUrl, HttpContext ctx) =>
{
    returnUrl ??= "/";
    var props = new AuthenticationProperties
    {
        RedirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(returnUrl)}"
    };
    await ctx.ChallengeAsync(provider, props);
});

// 2. Google redirects here after the user authenticates.
//    Complete the Identity sign-in and redirect to the original destination.
app.MapGet("/auth/callback", async (
    string? returnUrl,
    HttpContext ctx,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;

    // Read the external login info that Google just provided
    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info is null)
    {
        ctx.Response.Redirect("/login");
        return;
    }

    // Try to sign in with the existing external login link
    var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
    if (existingUser is not null && !existingUser.IsEnabled)
    {
        ctx.Response.Redirect("/login?error=disabled");
        return;
    }

    var result = await signInManager.ExternalLoginSignInAsync(
        info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);

    if (result.Succeeded)
    {
        ctx.Response.Redirect(returnUrl);
        return;
    }

    // First time this Google account has logged in — create the user
    var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email) ?? "";
    var name  = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? email;
    var picture = info.Principal.FindFirstValue("picture") ?? "";

    var user = new ApplicationUser
    {
        UserName      = email,
        Email         = email,
        DisplayName   = name,
        ProfilePictureUrl = picture,
        EmailConfirmed = true,
        IsEnabled      = true
    };

    var createResult = await userManager.CreateAsync(user);
    if (createResult.Succeeded)
    {
        await userManager.AddLoginAsync(user, info);
        await signInManager.SignInAsync(user, isPersistent: true);
        ctx.Response.Redirect(returnUrl);
        return;
    }

    // Creation failed — send back to login
    ctx.Response.Redirect("/login");
});

app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext ctx) =>
{
    await signInManager.SignOutAsync();
    ctx.Response.Redirect("/login");
});

app.Run();
