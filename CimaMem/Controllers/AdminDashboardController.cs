using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;
using CimaMem.Entities;

namespace CimaMem.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminDashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.MoviesCount = await _db.Movies.CountAsync();
        ViewBag.SeriesCount = await _db.Series.CountAsync();
        ViewBag.UsersCount = await _db.Users.CountAsync();
        ViewBag.GenresCount = await _db.Genres.CountAsync();

        ViewBag.RecentMovies = await _db.Movies.OrderByDescending(m => m.CreatedAt).Take(5).ToListAsync();
        ViewBag.RecentSeries = await _db.Series.OrderByDescending(s => s.CreatedAt).Take(5).ToListAsync();

        return View();
    }

    [HttpGet("Movies")]
    public async Task<IActionResult> Movies()
    {
        var movies = await _db.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return View(movies);
    }

    [HttpPost("Movies/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await _db.Movies.FindAsync(id);
        if (movie != null)
        {
            _db.Movies.Remove(movie);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"تم حذف الفيلم \"{movie.Title}\" بنجاح.";
        }
        return RedirectToAction(nameof(Movies));
    }

    [HttpGet("Series")]
    public async Task<IActionResult> Series()
    {
        var seriesList = await _db.Series
            .Include(s => s.Seasons)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(seriesList);
    }

    [HttpPost("Series/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSeries(int id)
    {
        var series = await _db.Series.FindAsync(id);
        if (series != null)
        {
            _db.Series.Remove(series);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"تم حذف المسلسل \"{series.Title}\" بنجاح.";
        }
        return RedirectToAction(nameof(Series));
    }
}
