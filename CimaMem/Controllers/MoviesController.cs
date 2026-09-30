using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;

namespace CimaMem.Controllers;

public class MoviesController : Controller
{
    private readonly ApplicationDbContext _db;

    public MoviesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("Movies")]
    public async Task<IActionResult> Index(int? genreId, string sort = "latest", int page = 1)
    {
        const int pageSize = 18;
        var query = _db.Movies
            .Include(m => m.MovieGenres)
            .ThenInclude(mg => mg.Genre)
            .AsQueryable();

        if (genreId.HasValue && genreId > 0)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId));
            ViewBag.CurrentGenre = await _db.Genres.FindAsync(genreId);
        }

        query = sort switch
        {
            "top_rated" => query.OrderByDescending(m => m.Rating),
            "popular" => query.OrderByDescending(m => m.Popularity),
            _ => query.OrderByDescending(m => m.ReleaseDate ?? m.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var movies = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Genres = await _db.Genres.ToListAsync();
        ViewBag.Sort = sort;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.GenreId = genreId;

        return View(movies);
    }

    [HttpGet("Movie/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var movie = await _db.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(m => m.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        var genreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList();
        var relatedMovies = await _db.Movies
            .Where(m => m.Id != movie.Id && m.MovieGenres.Any(mg => genreIds.Contains(mg.GenreId)))
            .Take(6)
            .ToListAsync();

        ViewBag.RelatedMovies = relatedMovies;

        return View(movie);
    }
}
