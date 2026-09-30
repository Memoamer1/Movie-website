using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;

namespace CimaMem.Controllers;

public class SeriesController : Controller
{
    private readonly ApplicationDbContext _db;

    public SeriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("Series")]
    public async Task<IActionResult> Index(int? genreId, string sort = "latest", int page = 1)
    {
        const int pageSize = 18;
        var query = _db.Series
            .Include(s => s.SeriesGenres)
            .ThenInclude(sg => sg.Genre)
            .AsQueryable();

        if (genreId.HasValue && genreId > 0)
        {
            query = query.Where(s => s.SeriesGenres.Any(sg => sg.GenreId == genreId));
            ViewBag.CurrentGenre = await _db.Genres.FindAsync(genreId);
        }

        query = sort switch
        {
            "top_rated" => query.OrderByDescending(s => s.Rating),
            "popular" => query.OrderByDescending(s => s.Popularity),
            _ => query.OrderByDescending(s => s.FirstAirDate ?? s.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var seriesList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Genres = await _db.Genres.ToListAsync();
        ViewBag.Sort = sort;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.GenreId = genreId;

        return View(seriesList);
    }

    [HttpGet("Series/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var series = await _db.Series
            .Include(s => s.SeriesGenres).ThenInclude(sg => sg.Genre)
            .Include(s => s.Seasons).ThenInclude(sn => sn.Episodes)
            .Include(s => s.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (series == null)
        {
            return NotFound();
        }

        var genreIds = series.SeriesGenres.Select(sg => sg.GenreId).ToList();
        var relatedSeries = await _db.Series
            .Where(s => s.Id != series.Id && s.SeriesGenres.Any(sg => genreIds.Contains(sg.GenreId)))
            .Take(6)
            .ToListAsync();

        ViewBag.RelatedSeries = relatedSeries;

        return View(series);
    }
}
