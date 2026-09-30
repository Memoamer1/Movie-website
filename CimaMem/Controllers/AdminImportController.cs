using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CimaMem.Services;

namespace CimaMem.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/Import")]
public class AdminImportController : Controller
{
    private readonly ITmdbService _tmdbService;
    private readonly ILogger<AdminImportController> _logger;

    public AdminImportController(ITmdbService tmdbService, ILogger<AdminImportController> logger)
    {
        _tmdbService = tmdbService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("SyncGenres")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncGenres()
    {
        var result = await _tmdbService.SyncGenresAsync();
        TempData["SyncResult"] = string.Join("<br/>", result.Messages);
        TempData["SyncSuccess"] = result.FailedCount == 0;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SyncMovies")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncMovies(string category = "popular", int page = 1)
    {
        var result = await _tmdbService.SyncMoviesCategoryAsync(category, page);
        TempData["SyncResult"] = string.Join("<br/>", result.Messages);
        TempData["SyncSuccess"] = result.FailedCount == 0;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("SyncSeries")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncSeries(string category = "popular", int page = 1)
    {
        var result = await _tmdbService.SyncSeriesCategoryAsync(category, page);
        TempData["SyncResult"] = string.Join("<br/>", result.Messages);
        TempData["SyncSuccess"] = result.FailedCount == 0;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ImportMovie")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportMovie(int tmdbId)
    {
        if (tmdbId <= 0)
        {
            TempData["SyncResult"] = "يرجى إدخال معرف TMDB صحيح للفيلم.";
            TempData["SyncSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var movie = await _tmdbService.ImportMovieByTmdbIdAsync(tmdbId);
        if (movie != null)
        {
            TempData["SyncResult"] = $"تم استيراد الفيلم \"{movie.Title}\" (TMDB ID: {tmdbId}) بنجاح!";
            TempData["SyncSuccess"] = true;
        }
        else
        {
            TempData["SyncResult"] = $"فشل استيراد الفيلم المعرف: {tmdbId}. يرجى التحقق من الرقم أو الاتصال بالشبكة.";
            TempData["SyncSuccess"] = false;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ImportSeries")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportSeries(int tmdbId)
    {
        if (tmdbId <= 0)
        {
            TempData["SyncResult"] = "يرجى إدخال معرف TMDB صحيح للمسلسل.";
            TempData["SyncSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var series = await _tmdbService.ImportSeriesByTmdbIdAsync(tmdbId);
        if (series != null)
        {
            TempData["SyncResult"] = $"تم استيراد المسلسل \"{series.Title}\" (TMDB ID: {tmdbId}) مع المواسم والحلقات بنجاح!";
            TempData["SyncSuccess"] = true;
        }
        else
        {
            TempData["SyncResult"] = $"فشل استيراد المسلسل المعرف: {tmdbId}. يرجى التحقق من الرقم أو الاتصال بالشبكة.";
            TempData["SyncSuccess"] = false;
        }

        return RedirectToAction(nameof(Index));
    }
}
