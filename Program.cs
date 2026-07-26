using FiveThreeOneTracker.Components;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Database — resolve to an absolute path so deployments never overwrite the file.
// In production, set ConnectionStrings__DefaultConnection or the DB_PATH environment variable
// to a persistent directory outside the deploy folder.
var dbPath = Environment.GetEnvironmentVariable("DB_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "fivethreeone.db");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Data Source={dbPath}";

// If the configured connection string uses a relative "Data Source=", make it absolute.
if (connectionString.Contains("Data Source=fivethreeone.db"))
    connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Application services
builder.Services.AddScoped<IBbbMappingService, BbbMappingService>();
builder.Services.AddScoped<IWeightCalculator, WeightCalculator>();
builder.Services.AddScoped<ICycleService, CycleService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IAccessoryService, AccessoryService>();
builder.Services.AddScoped<ILiftService, LiftService>();
builder.Services.AddScoped<IPlateCalculatorService, PlateCalculatorService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
