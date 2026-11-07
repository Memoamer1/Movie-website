using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;
using CimaMem.Models;
using CimaMem.Services;
using CimaMem.Entities;

namespace CimaMem.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITmdbService _tmdbService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext db, ITmdbService tmdbService, ILogger<HomeController> logger)
    {
        _db = db;
        _tmdbService = tmdbService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // If database has no movies or series yet, trigger initial sync
        if (!await _db.Movies.AnyAsync())
        {
            try
            {
                await _tmdbService.SyncMoviesCategoryAsync("popular", 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial background sync for movies failed.");
            }
        }

        if (!await _db.Series.AnyAsync())
        {
            try
            {
                await _tmdbService.SyncSeriesCategoryAsync("popular", 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial background sync for series failed.");
            }
        }

        var heroMovies = await _db.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(m => m.Rating)
            .Take(5)
            .ToListAsync();

        var latestMovies = await _db.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(m => m.ReleaseDate ?? m.CreatedAt)
            .Take(12)
            .ToListAsync();

        var latestSeries = await _db.Series
            .Include(s => s.SeriesGenres).ThenInclude(sg => sg.Genre)
            .OrderByDescending(s => s.FirstAirDate ?? s.CreatedAt)
            .Take(12)
            .ToListAsync();

        var topRatedMovies = await _db.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderByDescending(m => m.Rating)
            .Take(12)
            .ToListAsync();

        List<Genre> genres = await _db.Genres
            .Take(10)
            .ToListAsync();

        ViewBag.HeroMovies = heroMovies;
        ViewBag.LatestMovies = latestMovies;
        ViewBag.LatestSeries = latestSeries;
        ViewBag.TopRatedMovies = topRatedMovies;
        ViewBag.Genres = genres;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("About")]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet("Home/NotFoundPage")]
    public IActionResult NotFoundPage()
    {
        return View();
    }

    [HttpGet("test-tmdb")]
    public async Task<IActionResult> TestTmdb()
    {
        var genreResult = await _tmdbService.SyncGenresAsync();
        var movieResult = await _tmdbService.SyncMoviesCategoryAsync("popular", maxPages: 1);
        return Json(new { Genres = genreResult, Movies = movieResult });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
