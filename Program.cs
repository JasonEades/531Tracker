using FiveThreeOneTracker.Components;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

const string AdminRole  = "Admin";

var builder = WebApplication.CreateBuilder(args);

var adminEmail = builder.Configuration["App:AdminEmail"] ?? "";

// Database — resolve to an absolute path so deployments never overwrite the file.
var dbPath = Environment.GetEnvironmentVariable("DB_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "fivethreeone.db");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Data Source={dbPath}";

if (connectionString.Contains("Data Source=fivethreeone.db"))
    connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

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
app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext ctx) =>
{
    await signInManager.SignOutAsync();
    ctx.Response.Redirect("/login");
});

app.Run();
