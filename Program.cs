using FiveThreeOneTracker.Components;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
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
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("Postgres");

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

        options.Events.OnRedirectToAuthorizationEndpoint = ctx =>
        {
            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Auth.Google");
            log.LogInformation("[AUTH] Redirecting to Google authorization endpoint: {Uri}", ctx.RedirectUri);
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnTicketReceived = async ctx =>
        {
            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Auth.Google");
            log.LogInformation("[AUTH] OnTicketReceived fired");

            var userManager   = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = ctx.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

            var email       = ctx.Principal?.FindFirstValue(ClaimTypes.Email) ?? "";
            var name        = ctx.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;
            var picture     = ctx.Principal?.FindFirstValue("picture") ?? "";
            var providerKey = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            log.LogInformation("[AUTH] Google claims — email: {Email}, name: {Name}, providerKey: {Key}",
                string.IsNullOrEmpty(email) ? "(empty)" : email,
                string.IsNullOrEmpty(name)  ? "(empty)" : name,
                string.IsNullOrEmpty(providerKey) ? "(empty)" : providerKey);

            if (string.IsNullOrEmpty(email))
            {
                log.LogWarning("[AUTH] No email in Google principal — aborting sign-in");
                ctx.Fail("No email returned from Google.");
                return;
            }

            // Find or create the user
            var user = await userManager.FindByEmailAsync(email);
            log.LogInformation("[AUTH] FindByEmail result: {Found}", user is null ? "not found — will create" : $"found id={user.Id}");
            if (user is null)
            {
                log.LogInformation("[AUTH] Creating new user for email: {Email}", email);
                user = new ApplicationUser
                {
                    UserName          = email,
                    Email             = email,
                    DisplayName       = name,
                    ProfilePictureUrl = picture,
                    EmailConfirmed    = true,
                    IsEnabled         = true
                };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    log.LogError("[AUTH] Failed to create user: {Errors}", errors);
                    ctx.Fail("Failed to create user account.");
                    return;
                }
                log.LogInformation("[AUTH] User created successfully, id: {Id}", user.Id);
                var loginResult = await userManager.AddLoginAsync(user, new UserLoginInfo("Google", providerKey, "Google"));
                log.LogInformation("[AUTH] AddLoginAsync succeeded: {Ok}", loginResult.Succeeded);
            }
            else if (!user.IsEnabled)
            {
                log.LogWarning("[AUTH] User {Email} is disabled — sign-in refused", email);
                ctx.Fail("Your account has been disabled. Contact the administrator.");
                return;
            }
            else
            {
                log.LogInformation("[AUTH] Existing user found, IsEnabled: {Enabled}", user.IsEnabled);
            }

            // Ensure Admin role is assigned if this is the admin account.
            // Doing it here (before SignInAsync) guarantees the role claim lands in the
            // cookie on every login — even if the startup seed ran before this user existed.
            if (!string.IsNullOrEmpty(adminEmail) &&
                string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase) &&
                !await userManager.IsInRoleAsync(user, AdminRole))
            {
                await userManager.AddToRoleAsync(user, AdminRole);
                log.LogInformation("[AUTH] Admin role assigned to {Email}", email);
            }

            // Sign in with Identity (issues the application cookie)
            log.LogInformation("[AUTH] Calling SignInAsync for user: {Email}", email);
            await signInManager.SignInAsync(user, isPersistent: true);
            log.LogInformation("[AUTH] SignInAsync completed");

            // We've handled this — redirect to the return URL
            var redirectUri = ctx.Properties?.RedirectUri ?? "/";
            log.LogInformation("[AUTH] Calling HandleResponse and redirecting to: {Uri}", redirectUri);
            ctx.HandleResponse();
            ctx.Response.Redirect(redirectUri);
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
builder.Services.AddScoped<IProtocolService, ProtocolService>();
builder.Services.AddScoped<IAccessoryService, AccessoryService>();
builder.Services.AddScoped<ILiftService, LiftService>();
builder.Services.AddScoped<IPlateCalculatorService, PlateCalculatorService>();
builder.Services.AddScoped<IPplProgramService, PplProgramService>();
builder.Services.AddScoped<IPplSessionService, PplSessionService>();
builder.Services.AddScoped<IPplProgressionService, PplProgressionService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // DetailedErrors surfaces the real exception message/stack in circuit logs instead
        // of the generic "An unhandled error has occurred" message. Gate behind an env var
        // so it can be toggled on in production temporarily for diagnosis without a code change.
        options.DetailedErrors = builder.Environment.IsDevelopment()
            || Environment.GetEnvironmentVariable("BLAZOR_DETAILED_ERRORS") == "true";
    });

builder.Services.AddScoped<CircuitHandler, LoggingCircuitHandler>();

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

// Kick off the Google OAuth flow — sign-in completion is handled inside OnTicketReceived
app.MapGet("/challenge/{provider}", async (string provider, string? returnUrl, HttpContext ctx, ILoggerFactory lf) =>
{
    var log = lf.CreateLogger("Auth.Challenge");
    returnUrl ??= "/";
    log.LogInformation("[AUTH] /challenge/{Provider} called, returnUrl={ReturnUrl}, scheme={Scheme}",
        provider, returnUrl, ctx.Request.Scheme);
    var props = new AuthenticationProperties { RedirectUri = returnUrl };
    await ctx.ChallengeAsync(provider, props);
    log.LogInformation("[AUTH] ChallengeAsync issued, response status: {Status}", ctx.Response.StatusCode);
});

app.MapGet("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext ctx) =>
{
    await signInManager.SignOutAsync();
    ctx.Response.Redirect("/login");
});

// Client-side error reporting — mobile browsers have no accessible console, so JS errors
// (window.onerror / unhandledrejection) are POSTed here and logged server-side.
app.MapPost("/api/client-log", (ClientLogEntry entry, HttpContext ctx, ILoggerFactory lf) =>
{
    var log = lf.CreateLogger("Client.Error");
    var ua = ctx.Request.Headers.UserAgent.ToString();
    log.LogError(
        "[CLIENT] {Message} | Source={Source} Line={Line}:{Col} | Url={Url} | UA={UserAgent} | Stack={Stack}",
        entry.Message, entry.Source, entry.Line, entry.Column, entry.Url, ua, entry.Stack);
    return Results.Ok();
});

app.Run();

record ClientLogEntry(string Message, string? Source, int? Line, int? Column, string? Url, string? Stack);

