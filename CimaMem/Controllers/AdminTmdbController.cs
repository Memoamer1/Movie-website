using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CimaMem.Services;

namespace CimaMem.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Tmdb")]
public class AdminTmdbController : Controller
{
    private readonly ITmdbService _tmdbService;
    private readonly ILogger<AdminTmdbController> _logger;

    public AdminTmdbController(ITmdbService tmdbService, ILogger<AdminTmdbController> logger)
    {
        _tmdbService = tmdbService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.RecentLogs = await _tmdbService.GetRecentImportLogsAsync(10);
        return View();
    }

    [HttpPost("SyncCategory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncCategory(string type, int pages = 3)
    {
        TmdbSyncResult result;
        pages = Math.Clamp(pages, 1, 50);

        switch (type.ToLower())
        {
            case "movies_popular":
                result = await _tmdbService.SyncMoviesCategoryAsync("popular", pages);
                break;
            case "movies_now_playing":
                result = await _tmdbService.SyncMoviesCategoryAsync("now_playing", pages);
                break;
            case "movies_upcoming":
                result = await _tmdbService.SyncMoviesCategoryAsync("upcoming", pages);
                break;
            case "movies_top_rated":
                result = await _tmdbService.SyncMoviesCategoryAsync("top_rated", pages);
                break;
            case "movies_discover":
                result = await _tmdbService.DiscoverMoviesAsync(pages);
                break;
            case "series_popular":
                result = await _tmdbService.SyncSeriesCategoryAsync("popular", pages);
                break;
            case "series_top_rated":
                result = await _tmdbService.SyncSeriesCategoryAsync("top_rated", pages);
                break;
            case "series_on_the_air":
                result = await _tmdbService.SyncSeriesCategoryAsync("on_the_air", pages);
                break;
            case "series_airing_today":
                result = await _tmdbService.SyncSeriesCategoryAsync("airing_today", pages);
                break;
            case "series_discover":
                result = await _tmdbService.DiscoverSeriesAsync(pages);
                break;
            default:
                result = await _tmdbService.SyncGenresAsync();
                break;
        }

        TempData["SyncResult"] = string.Join("<br/>", result.Messages);
        TempData["SyncSuccess"] = result.FailedCount == 0;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SyncEverything")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncEverything(int pagesPerCategory = 3)
    {
        pagesPerCategory = Math.Clamp(pagesPerCategory, 1, 20);
        var result = await _tmdbService.SyncAllContentAsync(pagesPerCategory);
        
        TempData["SyncResult"] = $"تمت المزامنة الشاملة بنجاح! تم استيراد/تحديث {result.ImportedCount + result.UpdatedCount} عنصر.";
        TempData["SyncSuccess"] = result.FailedCount == 0;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("GetLogs")]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _tmdbService.GetRecentImportLogsAsync(10);
        return Json(logs);
    }
}
