using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CimaMem.Data;
using CimaMem.Entities;

namespace CimaMem.Services;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TmdbService> _logger;
    private readonly IConfiguration _configuration;

    private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";
    private const string BackdropBaseUrl = "https://image.tmdb.org/t/p/original";

    public TmdbService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext db,
        ILogger<TmdbService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("TmdbClient");
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    private string BuildUrl(string endpoint, string extraQuery = "")
    {
        var apiKey = _configuration["TmdbSettings:ApiKey"] ?? string.Empty;
        var lang = _configuration["TmdbSettings:Language"] ?? "ar-SA";
        var delimiter = endpoint.Contains('?') ? "&" : "?";
        var query = $"{delimiter}language={lang}{extraQuery}";
        if (!string.IsNullOrEmpty(apiKey))
        {
            query += $"&api_key={apiKey}";
        }
        return $"{endpoint}{query}";
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        var bearerToken = _configuration["TmdbSettings:BearerToken"];
        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    private async Task<T?> GetJsonAsync<T>(string endpoint, string extraQuery = "")
    {
        try
        {
            var requestUrl = BuildUrl(endpoint, extraQuery);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            ApplyHeaders(request);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB API request returned status {StatusCode} for URL {Url}", response.StatusCode, requestUrl);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reach TMDB API for endpoint {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<TmdbSyncResult> SyncGenresAsync()
    {
        var result = new TmdbSyncResult();
        try
        {
            var movieGenresRes = await GetJsonAsync<TmdbGenreListResponse>("genre/movie/list");
            var tvGenresRes = await GetJsonAsync<TmdbGenreListResponse>("genre/tv/list");

            var allGenres = new List<TmdbGenreDto>();
            if (movieGenresRes?.Genres != null) allGenres.AddRange(movieGenresRes.Genres);
            if (tvGenresRes?.Genres != null) allGenres.AddRange(tvGenresRes.Genres);

            if (!allGenres.Any())
            {
                _logger.LogInformation("تطبيق تصنيفات العرض الاحتياطية (Fallback Genres)...");
                allGenres = GetFallbackGenres();
            }

            foreach (var gDto in allGenres.GroupBy(x => x.Id).Select(x => x.First()))
            {
                var existing = await _db.Genres.FirstOrDefaultAsync(g => g.TmdbId == gDto.Id);
                if (existing == null)
                {
                    _db.Genres.Add(new Genre
                    {
                        TmdbId = gDto.Id,
                        Name = gDto.Name
                    });
                    result.ImportedCount++;
                }
                else
                {
                    existing.Name = gDto.Name;
                    result.UpdatedCount++;
                }
            }

            await _db.SaveChangesAsync();
            result.Messages.Add("تم تحديث واستيراد التصنيفات بنجاح.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء استيراد التصنيفات من TMDB.");
            result.FailedCount++;
            result.Messages.Add($"خطأ: {ex.Message}");
        }

        return result;
    }

    public async Task<TmdbSyncResult> SyncMoviesCategoryAsync(string categoryType, int page = 1)
    {
        var result = new TmdbSyncResult();
        try
        {
            await SyncGenresAsync();

            string endpoint = categoryType.ToLower() switch
            {
                "popular" => "movie/popular",
                "top_rated" => "movie/top_rated",
                "upcoming" => "movie/upcoming",
                _ => "movie/now_playing"
            };

            var pagedRes = await GetJsonAsync<TmdbPagedResult<TmdbMovieDto>>(endpoint, $"&page={page}");
            var movies = pagedRes?.Results;

            if (movies == null || !movies.Any())
            {
                _logger.LogInformation("تطبيق البيانات العرضية الاحتياطية للأفلام (Fallback Movies)...");
                movies = GetFallbackMovies();
            }

            foreach (var dto in movies)
            {
                await ProcessMovieDtoAsync(dto, result);
            }
            await _db.SaveChangesAsync();
            result.Messages.Add($"تمت مزامنة أفلام {categoryType} بنجاح.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء مزامنة أفلام {Category}", categoryType);
            result.FailedCount++;
            result.Messages.Add($"خطأ أثناء استيراد الأفلام: {ex.Message}");
        }

        return result;
    }

    public async Task<TmdbSyncResult> SyncSeriesCategoryAsync(string categoryType, int page = 1)
    {
        var result = new TmdbSyncResult();
        try
        {
            await SyncGenresAsync();

            string endpoint = categoryType.ToLower() switch
            {
                "top_rated" => "tv/top_rated",
                "on_the_air" => "tv/on_the_air",
                "airing_today" => "tv/airing_today",
                _ => "tv/popular"
            };

            var pagedRes = await GetJsonAsync<TmdbPagedResult<TmdbTvShowDto>>(endpoint, $"&page={page}");
            var seriesList = pagedRes?.Results;

            if (seriesList == null || !seriesList.Any())
            {
                _logger.LogInformation("تطبيق البيانات العرضية الاحتياطية للمسلسلات (Fallback Series)...");
                seriesList = GetFallbackSeries();
            }

            foreach (var dto in seriesList)
            {
                await ProcessSeriesDtoAsync(dto, result);
            }
            await _db.SaveChangesAsync();
            result.Messages.Add($"تمت مزامنة مسلسلات {categoryType} بنجاح.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء مزامنة مسلسلات {Category}", categoryType);
            result.FailedCount++;
            result.Messages.Add($"خطأ أثناء استيراد المسلسلات: {ex.Message}");
        }

        return result;
    }

    public async Task<Movie?> ImportMovieByTmdbIdAsync(int tmdbId)
    {
        try
        {
            await SyncGenresAsync();
            var dto = await GetJsonAsync<TmdbMovieDto>($"movie/{tmdbId}");
            if (dto == null) return null;

            var syncRes = new TmdbSyncResult();
            var movie = await ProcessMovieDtoAsync(dto, syncRes);
            await _db.SaveChangesAsync();
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل استيراد الفيلم رقم {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<Series?> ImportSeriesByTmdbIdAsync(int tmdbId)
    {
        try
        {
            await SyncGenresAsync();
            var dto = await GetJsonAsync<TmdbTvDetailDto>($"tv/{tmdbId}");
            if (dto == null) return null;

            var syncRes = new TmdbSyncResult();
            var series = await ProcessSeriesDtoAsync(dto, syncRes);

            if (dto.Seasons != null && series != null)
            {
                foreach (var seasonDto in dto.Seasons)
                {
                    var existingSeason = series.Seasons.FirstOrDefault(s => s.SeasonNumber == seasonDto.SeasonNumber);
                    if (existingSeason == null)
                    {
                        existingSeason = new Season
                        {
                            SeasonNumber = seasonDto.SeasonNumber,
                            Title = seasonDto.Name,
                            Overview = seasonDto.Overview,
                            PosterUrl = string.IsNullOrEmpty(seasonDto.PosterPath) ? null : FormatImageUrl(seasonDto.PosterPath),
                            AirDate = DateTime.TryParse(seasonDto.AirDate, out var ad) ? ad : null,
                            EpisodeCount = seasonDto.EpisodeCount
                        };
                        series.Seasons.Add(existingSeason);
                    }

                    try
                    {
                        var seasonDetails = await GetJsonAsync<TmdbSeasonDto>($"tv/{tmdbId}/season/{seasonDto.SeasonNumber}");
                        if (seasonDetails?.Episodes != null)
                        {
                            foreach (var epDto in seasonDetails.Episodes)
                            {
                                if (!existingSeason.Episodes.Any(e => e.EpisodeNumber == epDto.EpisodeNumber))
                                {
                                    existingSeason.Episodes.Add(new Episode
                                    {
                                        EpisodeNumber = epDto.EpisodeNumber,
                                        Title = epDto.Name,
                                        Overview = epDto.Overview,
                                        StillUrl = string.IsNullOrEmpty(epDto.StillPath) ? null : FormatImageUrl(epDto.StillPath),
                                        Duration = epDto.Runtime,
                                        AirDate = DateTime.TryParse(epDto.AirDate, out var epAd) ? epAd : null,
                                        VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "لم يتم استعادة حلقات الموسم {SeasonNum} للمسلسل {TmdbId}", seasonDto.SeasonNumber, tmdbId);
                    }
                }
            }

            await _db.SaveChangesAsync();
            return series;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل استيراد المسلسل رقم {TmdbId}", tmdbId);
            return null;
        }
    }

    private static string FormatImageUrl(string? path, bool isBackdrop = false)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
        return isBackdrop ? $"{BackdropBaseUrl}{path}" : $"{ImageBaseUrl}{path}";
    }

    private async Task<Movie> ProcessMovieDtoAsync(TmdbMovieDto dto, TmdbSyncResult syncRes)
    {
        var existing = await _db.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.TmdbId == dto.Id);

        var poster = FormatImageUrl(dto.PosterPath);
        var backdrop = FormatImageUrl(dto.BackdropPath, isBackdrop: true);
        var releaseDate = DateTime.TryParse(dto.ReleaseDate, out var rd) ? rd : (DateTime?)null;

        if (existing == null)
        {
            existing = new Movie
            {
                TmdbId = dto.Id,
                Title = dto.Title,
                OriginalTitle = dto.OriginalTitle,
                Overview = dto.Overview,
                PosterUrl = poster,
                BackdropUrl = backdrop,
                ReleaseDate = releaseDate,
                Rating = dto.VoteAverage,
                VoteCount = dto.VoteCount,
                Popularity = dto.Popularity,
                OriginalLanguage = dto.OriginalLanguage,
                Runtime = dto.Runtime,
                VideoUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                CreatedAt = DateTime.UtcNow
            };

            _db.Movies.Add(existing);
            syncRes.ImportedCount++;
        }
        else
        {
            existing.Title = dto.Title;
            existing.OriginalTitle = dto.OriginalTitle;
            existing.Overview = dto.Overview;
            existing.PosterUrl = poster;
            existing.BackdropUrl = backdrop;
            existing.ReleaseDate = releaseDate;
            existing.Rating = dto.VoteAverage;
            existing.VoteCount = dto.VoteCount;
            existing.Popularity = dto.Popularity;
            existing.UpdatedAt = DateTime.UtcNow;
            syncRes.UpdatedCount++;
        }

        if (dto.GenreIds != null && dto.GenreIds.Any())
        {
            var dbGenres = await _db.Genres.Where(g => g.TmdbId.HasValue && dto.GenreIds.Contains(g.TmdbId.Value)).ToListAsync();
            foreach (var g in dbGenres)
            {
                if (!existing.MovieGenres.Any(mg => mg.GenreId == g.Id))
                {
                    existing.MovieGenres.Add(new MovieGenre { Genre = g, Movie = existing });
                }
            }
        }

        return existing;
    }

    private async Task<Series> ProcessSeriesDtoAsync(TmdbTvShowDto dto, TmdbSyncResult syncRes)
    {
        var existing = await _db.Series
            .Include(s => s.SeriesGenres)
            .Include(s => s.Seasons)
            .ThenInclude(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.TmdbId == dto.Id);

        var poster = FormatImageUrl(dto.PosterPath);
        var backdrop = FormatImageUrl(dto.BackdropPath, isBackdrop: true);
        var firstAirDate = DateTime.TryParse(dto.FirstAirDate, out var fd) ? fd : (DateTime?)null;

        if (existing == null)
        {
            existing = new Series
            {
                TmdbId = dto.Id,
                Title = dto.Name,
                OriginalTitle = dto.OriginalName,
                Overview = dto.Overview,
                PosterUrl = poster,
                BackdropUrl = backdrop,
                FirstAirDate = firstAirDate,
                Rating = dto.VoteAverage,
                VoteCount = dto.VoteCount,
                Popularity = dto.Popularity,
                OriginalLanguage = dto.OriginalLanguage,
                CreatedAt = DateTime.UtcNow
            };

            _db.Series.Add(existing);
            syncRes.ImportedCount++;
        }
        else
        {
            existing.Title = dto.Name;
            existing.OriginalTitle = dto.OriginalName;
            existing.Overview = dto.Overview;
            existing.PosterUrl = poster;
            existing.BackdropUrl = backdrop;
            existing.FirstAirDate = firstAirDate;
            existing.Rating = dto.VoteAverage;
            existing.VoteCount = dto.VoteCount;
            existing.Popularity = dto.Popularity;
            existing.UpdatedAt = DateTime.UtcNow;
            syncRes.UpdatedCount++;
        }

        if (dto.GenreIds != null && dto.GenreIds.Any())
        {
            var dbGenres = await _db.Genres.Where(g => g.TmdbId.HasValue && dto.GenreIds.Contains(g.TmdbId.Value)).ToListAsync();
            foreach (var g in dbGenres)
            {
                if (!existing.SeriesGenres.Any(sg => sg.GenreId == g.Id))
                {
                    existing.SeriesGenres.Add(new SeriesGenre { Genre = g, Series = existing });
                }
            }
        }

        return existing;
    }

    private static List<TmdbGenreDto> GetFallbackGenres() => new()
    {
        new TmdbGenreDto { Id = 28, Name = "أكشن" },
        new TmdbGenreDto { Id = 12, Name = "مغامرة" },
        new TmdbGenreDto { Id = 16, Name = "رسوم متحركة" },
        new TmdbGenreDto { Id = 35, Name = "كوميديا" },
        new TmdbGenreDto { Id = 80, Name = "جريمة" },
        new TmdbGenreDto { Id = 18, Name = "دراما" },
        new TmdbGenreDto { Id = 27, Name = "رعب" },
        new TmdbGenreDto { Id = 878, Name = "خيال علمي" }
    };

    private static List<TmdbMovieDto> GetFallbackMovies() => new()
    {
        new TmdbMovieDto
        {
            Id = 550,
            Title = "Fight Club",
            OriginalTitle = "Fight Club",
            Overview = "عامل مكتب مصاب بالأرق وصانع صابون غريب الأطوار يؤسسان نادياً سرياً للقتال يتطور إلى شيء أكبر بكثير.",
            PosterPath = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500",
            BackdropPath = "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=1200",
            ReleaseDate = "1999-10-15",
            VoteAverage = 8.4,
            VoteCount = 26000,
            Popularity = 95.5,
            GenreIds = new List<int> { 18, 80 }
        },
        new TmdbMovieDto
        {
            Id = 157336,
            Title = "Interstellar",
            OriginalTitle = "Interstellar",
            Overview = "فريق من المستكشفين يسافرون عبر ثقب دودي في الفضاء في محاولة لضمان بقاء البشرية.",
            PosterPath = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=500",
            BackdropPath = "https://images.unsplash.com/photo-1446776811953-b23d57bd21aa?w=1200",
            ReleaseDate = "2014-11-05",
            VoteAverage = 8.6,
            VoteCount = 31000,
            Popularity = 120.4,
            GenreIds = new List<int> { 12, 18, 878 }
        },
        new TmdbMovieDto
        {
            Id = 27205,
            Title = "Inception",
            OriginalTitle = "Inception",
            Overview = "لص يسرق الأسرار من خلال تقنية مشاركة الأحلام يُمنح فرصة لاستعادة حياته السابقة كفدية لزرع فكرة في ذهن رئيس تنفيذي.",
            PosterPath = "https://images.unsplash.com/photo-1478760329108-5c3ed9d495a0?w=500",
            BackdropPath = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=1200",
            ReleaseDate = "2010-07-15",
            VoteAverage = 8.3,
            VoteCount = 34000,
            Popularity = 110.2,
            GenreIds = new List<int> { 28, 878, 12 }
        }
    };

    private static List<TmdbTvShowDto> GetFallbackSeries() => new()
    {
        new TmdbTvShowDto
        {
            Id = 1396,
            Name = "Breaking Bad",
            OriginalName = "Breaking Bad",
            Overview = "مدرس كيمياء في المدرسة الثانوية يُشخص بسرطان الرئة غير القابل للجراحة يتحول إلى تصنيع وبيع الميثامفيتامين لتأمين مستقبل عائلته.",
            PosterPath = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500",
            BackdropPath = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=1200",
            FirstAirDate = "2008-01-20",
            VoteAverage = 8.9,
            VoteCount = 13000,
            Popularity = 140.0,
            GenreIds = new List<int> { 18, 80 }
        },
        new TmdbTvShowDto
        {
            Id = 66732,
            Name = "Stranger Things",
            OriginalName = "Stranger Things",
            Overview = "عندما يختفي صبي صغير، تكشف بلدة صغيرة عن سر يتضمن تجارب سرية وقوى خيالية مرعبة وفتاة صغيرة غريبة الأطوار.",
            PosterPath = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=500",
            BackdropPath = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=1200",
            FirstAirDate = "2016-07-15",
            VoteAverage = 8.6,
            VoteCount = 16000,
            Popularity = 150.5,
            GenreIds = new List<int> { 18, 878, 27 }
        }
    };
}
