using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;
using CimaMem.Entities;
using CimaMem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add Memory Cache & Response Caching
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Register TMDB HttpClient & TmdbService
builder.Services.AddHttpClient("TmdbClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["TmdbSettings:BaseUrl"] ?? "https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<ITmdbService, TmdbService>();

// Add MVC Services
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Roles & Default Admin User
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding default roles and admin.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
