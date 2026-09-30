using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;
using CimaMem.Entities;

namespace CimaMem.Controllers;

public class SearchSearchResultItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public double Rating { get; set; }
    public string Type { get; set; } = "Movie"; // Movie or Series
    public int? Year { get; set; }
}

public class SearchController : Controller
{
    private readonly ApplicationDbContext _db;

    public SearchController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("Search")]
    public async Task<IActionResult> Index(string? q)
    {
        var results = new List<SearchSearchResultItem>();
        q = q?.Trim();

        if (!string.IsNullOrEmpty(q))
        {
            var movies = await _db.Movies
                .Where(m => m.Title.Contains(q) ||
                            (m.OriginalTitle != null && m.OriginalTitle.Contains(q)) ||
                            (m.ArabicTitle != null && m.ArabicTitle.Contains(q)) ||
                            (m.Overview != null && m.Overview.Contains(q)))
                .Take(20)
                .Select(m => new SearchSearchResultItem
                {
                    Id = m.Id,
                    Title = m.Title,
                    PosterUrl = m.PosterUrl,
                    Rating = m.Rating,
                    Type = "Movie",
                    Year = m.ReleaseDate.HasValue ? m.ReleaseDate.Value.Year : null
                })
                .ToListAsync();

            var series = await _db.Series
                .Where(s => s.Title.Contains(q) ||
                            (s.OriginalTitle != null && s.OriginalTitle.Contains(q)) ||
                            (s.ArabicTitle != null && s.ArabicTitle.Contains(q)) ||
                            (s.Overview != null && s.Overview.Contains(q)))
                .Take(20)
                .Select(s => new SearchSearchResultItem
                {
                    Id = s.Id,
                    Title = s.Title,
                    PosterUrl = s.PosterUrl,
                    Rating = s.Rating,
                    Type = "Series",
                    Year = s.FirstAirDate.HasValue ? s.FirstAirDate.Value.Year : null
                })
                .ToListAsync();

            results.AddRange(movies);
            results.AddRange(series);
        }

        ViewBag.Query = q;
        return View(results);
    }
}
