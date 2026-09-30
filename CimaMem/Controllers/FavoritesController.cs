using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;
using CimaMem.Entities;

namespace CimaMem.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public FavoritesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("Favorites")]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var favorites = await _db.Favorites
            .Include(f => f.Movie)
            .Include(f => f.Series)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return View(favorites);
    }

    [HttpPost("Favorites/ToggleMovie")]
    public async Task<IActionResult> ToggleMovie(int movieId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "يرجى تسجيل الدخول أولاً" });

        var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);
        bool isAdded;

        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            isAdded = false;
        }
        else
        {
            _db.Favorites.Add(new Favorite
            {
                UserId = userId,
                MovieId = movieId
            });
            isAdded = true;
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true, isAdded = isAdded, message = isAdded ? "تمت الإضافة إلى المفضلة" : "تمت الإزالة من المفضلة" });
    }

    [HttpPost("Favorites/ToggleSeries")]
    public async Task<IActionResult> ToggleSeries(int seriesId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "يرجى تسجيل الدخول أولاً" });

        var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.SeriesId == seriesId);
        bool isAdded;

        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            isAdded = false;
        }
        else
        {
            _db.Favorites.Add(new Favorite
            {
                UserId = userId,
                SeriesId = seriesId
            });
            isAdded = true;
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true, isAdded = isAdded, message = isAdded ? "تمت الإضافة إلى المفضلة" : "تمت الإزالة من المفضلة" });
    }
}
