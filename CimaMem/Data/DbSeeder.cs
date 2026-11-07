using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CimaMem.Entities;
using CimaMem.Services;

namespace CimaMem.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure Database Schema is Migrated
        try
        {
            await db.Database.MigrateAsync();
        }
        catch
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Seed Roles
        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin User
        var adminEmail = "admin@cinewave.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Initial Content from TMDB if Database is Empty
        if (!await db.Movies.AnyAsync())
        {
            var tmdbService = serviceProvider.GetRequiredService<ITmdbService>();
            await tmdbService.SyncGenresAsync();
            await tmdbService.SyncMoviesCategoryAsync("popular", maxPages: 2);
            await tmdbService.SyncMoviesCategoryAsync("top_rated", maxPages: 2);
            await tmdbService.SyncSeriesCategoryAsync("popular", maxPages: 2);
        }
    }
}
