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
        var bearerToken = _configuration["TmdbSettings:AccessToken"] ?? _configuration["TmdbSettings:BearerToken"];
        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    private async Task<T?> GetJsonAsync<T>(string endpoint, string extraQuery = "", CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = BuildUrl(endpoint, extraQuery);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            ApplyHeaders(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB API request returned status {StatusCode} for URL {Url}", response.StatusCode, requestUrl);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reach TMDB API for endpoint {Endpoint}", endpoint);
            return default;
        }
    }

    private string FormatImageUrl(string? path, bool isBackdrop = false)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        if (path.StartsWith("http://") || path.StartsWith("https://")) return path;

        var baseUrl = isBackdrop
            ? (_configuration["TmdbSettings:BackdropBaseUrl"] ?? "https://image.tmdb.org/t/p/original")
            : (_configuration["TmdbSettings:ImageBaseUrl"] ?? "https://image.tmdb.org/t/p/w500");

        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public async Task<TmdbSyncResult> SyncGenresAsync(CancellationToken cancellationToken = default)
    {
        var result = new TmdbSyncResult { OperationType = "SyncGenres" };
        try
        {
            var movieGenresRes = await GetJsonAsync<TmdbGenreListResponse>("genre/movie/list", cancellationToken: cancellationToken);
            var tvGenresRes = await GetJsonAsync<TmdbGenreListResponse>("genre/tv/list", cancellationToken: cancellationToken);

            var allGenres = new List<TmdbGenreDto>();
            if (movieGenresRes?.Genres != null) allGenres.AddRange(movieGenresRes.Genres);
            if (tvGenresRes?.Genres != null) allGenres.AddRange(tvGenresRes.Genres);

            if (!allGenres.Any())
            {
                allGenres = GetFallbackGenres();
            }

            foreach (var gDto in allGenres.GroupBy(x => x.Id).Select(x => x.First()))
            {
                result.ProcessedCount++;
                var existing = await _db.Genres.FirstOrDefaultAsync(g => g.TmdbId == gDto.Id, cancellationToken);
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

            await _db.SaveChangesAsync(cancellationToken);
            result.Messages.Add("تم تحديث واستيراد التصنيفات بنجاح.");
            await LogImportResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء استيراد التصنيفات من TMDB.");
            result.FailedCount++;
            result.Status = "Failed";
            result.Messages.Add($"خطأ: {ex.Message}");
        }

        return result;
    }

    public async Task<TmdbSyncResult> SyncMoviesCategoryAsync(string categoryType, int maxPages = 1, CancellationToken cancellationToken = default)
    {
        var result = new TmdbSyncResult { OperationType = $"Movies_{categoryType}" };
        try
        {
            await SyncGenresAsync(cancellationToken);

            string endpoint = categoryType.ToLower() switch
            {
                "popular" => "movie/popular",
                "top_rated" => "movie/top_rated",
                "upcoming" => "movie/upcoming",
                _ => "movie/now_playing"
            };

            var configuredPages = _configuration.GetValue<int>($"TmdbSettings:ImportPages:{categoryType}", maxPages);
            int totalPagesToProcess = Math.Max(maxPages, configuredPages);

            for (int page = 1; page <= totalPagesToProcess; page++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var pagedRes = await GetJsonAsync<TmdbPagedResult<TmdbMovieDto>>(endpoint, $"&page={page}", cancellationToken);
                var movies = pagedRes?.Results;

                if (movies == null || !movies.Any())
                {
                    if (page == 1) movies = GetFallbackMovies();
                    else break;
                }

                foreach (var dto in movies)
                {
                    try
                    {
                        result.ProcessedCount++;
                        await ProcessMovieDtoAsync(dto, result, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process movie {TmdbId}", dto.Id);
                        result.FailedCount++;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                result.CurrentPage = page;
                result.TotalPages = pagedRes?.TotalPages ?? totalPagesToProcess;
                await Task.Delay(50, cancellationToken); // Rate limit delay
            }

            result.Messages.Add($"تمت مزامنة أفلام {categoryType} بنجاح.");
            await LogImportResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء مزامنة أفلام {Category}", categoryType);
            result.FailedCount++;
            result.Status = "Failed";
            result.Messages.Add($"خطأ أثناء استيراد الأفلام: {ex.Message}");
        }

        return result;
    }

    public async Task<TmdbSyncResult> DiscoverMoviesAsync(int maxPages = 1, string? sortBy = "popularity.desc", int? genreId = null, CancellationToken cancellationToken = default)
    {
        var result = new TmdbSyncResult { OperationType = "Movies_Discover" };
        try
        {
            await SyncGenresAsync(cancellationToken);

            string extraQuery = $"&sort_by={sortBy ?? "popularity.desc"}";
            if (genreId.HasValue && genreId > 0)
            {
                extraQuery += $"&with_genres={genreId.Value}";
            }

            for (int page = 1; page <= maxPages; page++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var pagedRes = await GetJsonAsync<TmdbPagedResult<TmdbMovieDto>>("discover/movie", $"&page={page}{extraQuery}", cancellationToken);
                var movies = pagedRes?.Results;

                if (movies == null || !movies.Any()) break;

                foreach (var dto in movies)
                {
                    try
                    {
                        result.ProcessedCount++;
                        await ProcessMovieDtoAsync(dto, result, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to discover process movie {TmdbId}", dto.Id);
                        result.FailedCount++;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                result.CurrentPage = page;
                result.TotalPages = pagedRes?.TotalPages ?? maxPages;
                await Task.Delay(50, cancellationToken);
            }

            result.Messages.Add("تمت عملية استكشاف وتحديث الأفلام بنجاح.");
            await LogImportResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء استكشاف الأفلام DiscoverMovies");
            result.FailedCount++;
            result.Status = "Failed";
            result.Messages.Add($"خطأ: {ex.Message}");
        }

        return result;
    }

    public async Task<TmdbSyncResult> SyncSeriesCategoryAsync(string categoryType, int maxPages = 1, CancellationToken cancellationToken = default)
    {
        var result = new TmdbSyncResult { OperationType = $"Series_{categoryType}" };
        try
        {
            await SyncGenresAsync(cancellationToken);

            string endpoint = categoryType.ToLower() switch
            {
                "top_rated" => "tv/top_rated",
                "on_the_air" => "tv/on_the_air",
                "airing_today" => "tv/airing_today",
                _ => "tv/popular"
            };

            var configuredPages = _configuration.GetValue<int>($"TmdbSettings:ImportPages:{categoryType}", maxPages);
            int totalPagesToProcess = Math.Max(maxPages, configuredPages);

            for (int page = 1; page <= totalPagesToProcess; page++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var pagedRes = await GetJsonAsync<TmdbPagedResult<TmdbTvShowDto>>(endpoint, $"&page={page}", cancellationToken);
                var seriesList = pagedRes?.Results;

                if (seriesList == null || !seriesList.Any())
                {
                    if (page == 1) seriesList = GetFallbackSeries();
                    else break;
                }

                foreach (var dto in seriesList)
                {
                    try
                    {
                        result.ProcessedCount++;
                        await ProcessSeriesDtoAsync(dto, result, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process series {TmdbId}", dto.Id);
                        result.FailedCount++;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                result.CurrentPage = page;
                result.TotalPages = pagedRes?.TotalPages ?? totalPagesToProcess;
                await Task.Delay(50, cancellationToken);
            }

            result.Messages.Add($"تمت مزامنة مسلسلات {categoryType} بنجاح.");
            await LogImportResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء مزامنة مسلسلات {Category}", categoryType);
            result.FailedCount++;
            result.Status = "Failed";
            result.Messages.Add($"خطأ أثناء استيراد المسلسلات: {ex.Message}");
        }

        return result;
    }

    public async Task<TmdbSyncResult> DiscoverSeriesAsync(int maxPages = 1, string? sortBy = "popularity.desc", int? genreId = null, CancellationToken cancellationToken = default)
    {
        var result = new TmdbSyncResult { OperationType = "Series_Discover" };
        try
        {
            await SyncGenresAsync(cancellationToken);

            string extraQuery = $"&sort_by={sortBy ?? "popularity.desc"}";
            if (genreId.HasValue && genreId > 0)
            {
                extraQuery += $"&with_genres={genreId.Value}";
            }

            for (int page = 1; page <= maxPages; page++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var pagedRes = await GetJsonAsync<TmdbPagedResult<TmdbTvShowDto>>("discover/tv", $"&page={page}{extraQuery}", cancellationToken);
                var seriesList = pagedRes?.Results;

                if (seriesList == null || !seriesList.Any()) break;

                foreach (var dto in seriesList)
                {
                    try
                    {
                        result.ProcessedCount++;
                        await ProcessSeriesDtoAsync(dto, result, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to discover series {TmdbId}", dto.Id);
                        result.FailedCount++;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                result.CurrentPage = page;
                result.TotalPages = pagedRes?.TotalPages ?? maxPages;
                await Task.Delay(50, cancellationToken);
            }

            result.Messages.Add("تمت عملية استكشاف وتحديث المسلسلات بنجاح.");
            await LogImportResultAsync(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء استكشاف المسلسلات DiscoverSeries");
            result.FailedCount++;
            result.Status = "Failed";
            result.Messages.Add($"خطأ: {ex.Message}");
        }

        return result;
    }

    public async Task<Movie?> ImportMovieByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            await SyncGenresAsync(cancellationToken);
            var dto = await GetJsonAsync<TmdbMovieDto>($"movie/{tmdbId}", cancellationToken: cancellationToken);
            if (dto == null) return null;

            var syncRes = new TmdbSyncResult();
            var movie = await ProcessMovieDtoAsync(dto, syncRes, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل استيراد الفيلم رقم {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<Series?> ImportSeriesByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        try
        {
            await SyncGenresAsync(cancellationToken);
            var dto = await GetJsonAsync<TmdbTvDetailDto>($"tv/{tmdbId}", cancellationToken: cancellationToken);
            if (dto == null) return null;

            var syncRes = new TmdbSyncResult();
            var series = await ProcessSeriesDtoAsync(dto, syncRes, cancellationToken);

            if (dto.Seasons != null && series != null)
            {
                foreach (var seasonDto in dto.Seasons)
                {
                    var existingSeason = series.Seasons.FirstOrDefault(s => s.SeasonNumber == seasonDto.SeasonNumber || (s.TmdbId.HasValue && s.TmdbId == seasonDto.Id));
                    if (existingSeason == null)
                    {
                        existingSeason = new Season
                        {
                            TmdbId = seasonDto.Id,
                            SeasonNumber = seasonDto.SeasonNumber,
                            Title = seasonDto.Name,
                            Overview = seasonDto.Overview,
                            PosterUrl = string.IsNullOrEmpty(seasonDto.PosterPath) ? null : FormatImageUrl(seasonDto.PosterPath),
                            AirDate = DateTime.TryParse(seasonDto.AirDate, out var ad) ? ad : null,
                            EpisodeCount = seasonDto.EpisodeCount
                        };
                        series.Seasons.Add(existingSeason);
                    }
                    else
                    {
                        existingSeason.TmdbId = seasonDto.Id;
                        existingSeason.Title = seasonDto.Name;
                        existingSeason.Overview = seasonDto.Overview;
                        existingSeason.PosterUrl = string.IsNullOrEmpty(seasonDto.PosterPath) ? existingSeason.PosterUrl : FormatImageUrl(seasonDto.PosterPath);
                    }

                    try
                    {
                        var seasonDetails = await GetJsonAsync<TmdbSeasonDto>($"tv/{tmdbId}/season/{seasonDto.SeasonNumber}", cancellationToken: cancellationToken);
                        if (seasonDetails?.Episodes != null)
                        {
                            foreach (var epDto in seasonDetails.Episodes)
                            {
                                var existingEp = existingSeason.Episodes.FirstOrDefault(e => e.EpisodeNumber == epDto.EpisodeNumber || (e.TmdbId.HasValue && e.TmdbId == epDto.Id));
                                if (existingEp == null)
                                {
                                    existingSeason.Episodes.Add(new Episode
                                    {
                                        TmdbId = epDto.Id,
                                        EpisodeNumber = epDto.EpisodeNumber,
                                        Title = epDto.Name,
                                        Overview = epDto.Overview,
                                        StillUrl = string.IsNullOrEmpty(epDto.StillPath) ? null : FormatImageUrl(epDto.StillPath),
                                        Duration = epDto.Runtime,
                                        AirDate = DateTime.TryParse(epDto.AirDate, out var epAd) ? epAd : null,
                                        VideoUrl = string.Empty
                                    });
                                }
                                else
                                {
                                    existingEp.TmdbId = epDto.Id;
                                    existingEp.Title = epDto.Name;
                                    existingEp.Overview = epDto.Overview;
                                    existingEp.StillUrl = string.IsNullOrEmpty(epDto.StillPath) ? existingEp.StillUrl : FormatImageUrl(epDto.StillPath);
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

            await _db.SaveChangesAsync(cancellationToken);
            return series;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل استيراد المسلسل رقم {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<TmdbSyncResult> SyncAllContentAsync(int maxPagesPerCategory = 5, CancellationToken cancellationToken = default)
    {
        var combinedResult = new TmdbSyncResult { OperationType = "SyncEverything" };
        
        await SyncGenresAsync(cancellationToken);
        
        var popularMovies = await SyncMoviesCategoryAsync("popular", maxPagesPerCategory, cancellationToken);
        AccumulateResults(combinedResult, popularMovies);

        var topRatedMovies = await SyncMoviesCategoryAsync("top_rated", maxPagesPerCategory, cancellationToken);
        AccumulateResults(combinedResult, topRatedMovies);

        var nowPlayingMovies = await SyncMoviesCategoryAsync("now_playing", maxPagesPerCategory, cancellationToken);
        AccumulateResults(combinedResult, nowPlayingMovies);

        var popularSeries = await SyncSeriesCategoryAsync("popular", maxPagesPerCategory, cancellationToken);
        AccumulateResults(combinedResult, popularSeries);

        var topRatedSeries = await SyncSeriesCategoryAsync("top_rated", maxPagesPerCategory, cancellationToken);
        AccumulateResults(combinedResult, topRatedSeries);

        combinedResult.Messages.Add("تم إنجاز المزامنة الشاملة بنجاح لكافة الفئات والأفلام والمسلسلات.");
        await LogImportResultAsync(combinedResult);
        return combinedResult;
    }

    public async Task<List<ImportLog>> GetRecentImportLogsAsync(int count = 10)
    {
        return await _db.ImportLogs
            .OrderByDescending(l => l.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    private void AccumulateResults(TmdbSyncResult total, TmdbSyncResult current)
    {
        total.ProcessedCount += current.ProcessedCount;
        total.ImportedCount += current.ImportedCount;
        total.UpdatedCount += current.UpdatedCount;
        total.SkippedCount += current.SkippedCount;
        total.FailedCount += current.FailedCount;
    }

    private async Task LogImportResultAsync(TmdbSyncResult result)
    {
        try
        {
            _db.ImportLogs.Add(new ImportLog
            {
                Type = result.OperationType,
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
                Processed = result.ProcessedCount,
                Imported = result.ImportedCount,
                Updated = result.UpdatedCount,
                Skipped = result.SkippedCount,
                Failed = result.FailedCount,
                Status = result.FailedCount == 0 ? "Completed" : "Partial",
                ErrorMessage = string.Join("; ", result.Messages)
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log import result to database.");
        }
    }

    private async Task<Movie> ProcessMovieDtoAsync(TmdbMovieDto dto, TmdbSyncResult result, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.TmdbId == dto.Id, cancellationToken);

        if (existing == null)
        {
            existing = new Movie
            {
                TmdbId = dto.Id,
                Title = string.IsNullOrEmpty(dto.Title) ? (dto.OriginalTitle ?? "عنوان غير معروف") : dto.Title,
                OriginalTitle = dto.OriginalTitle,
                Overview = dto.Overview,
                PosterUrl = FormatImageUrl(dto.PosterPath),
                BackdropUrl = FormatImageUrl(dto.BackdropPath, isBackdrop: true),
                ReleaseDate = DateTime.TryParse(dto.ReleaseDate, out var rd) ? rd : null,
                Runtime = dto.Runtime,
                Rating = dto.VoteAverage,
                VoteCount = dto.VoteCount,
                Popularity = dto.Popularity,
                OriginalLanguage = dto.OriginalLanguage,
                VideoUrl = string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            _db.Movies.Add(existing);
            result.ImportedCount++;
        }
        else
        {
            existing.Title = string.IsNullOrEmpty(dto.Title) ? existing.Title : dto.Title;
            existing.OriginalTitle = dto.OriginalTitle ?? existing.OriginalTitle;
            existing.Overview = string.IsNullOrEmpty(dto.Overview) ? existing.Overview : dto.Overview;
            existing.PosterUrl = string.IsNullOrEmpty(dto.PosterPath) ? existing.PosterUrl : FormatImageUrl(dto.PosterPath);
            existing.BackdropUrl = string.IsNullOrEmpty(dto.BackdropPath) ? existing.BackdropUrl : FormatImageUrl(dto.BackdropPath, isBackdrop: true);
            existing.Rating = dto.VoteAverage;
            existing.VoteCount = dto.VoteCount;
            existing.Popularity = dto.Popularity;
            existing.UpdatedAt = DateTime.UtcNow;
            result.UpdatedCount++;
        }

        if (dto.GenreIds != null && dto.GenreIds.Any())
        {
            var dbGenres = await _db.Genres
                .Where(g => g.TmdbId.HasValue && dto.GenreIds.Contains(g.TmdbId.Value))
                .ToListAsync(cancellationToken);

            foreach (var g in dbGenres)
            {
                if (!existing.MovieGenres.Any(mg => mg.GenreId == g.Id))
                {
                    existing.MovieGenres.Add(new MovieGenre { GenreId = g.Id });
                }
            }
        }

        return existing;
    }

    private async Task<Series> ProcessSeriesDtoAsync(TmdbTvShowDto dto, TmdbSyncResult result, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Series
            .Include(s => s.SeriesGenres)
            .Include(s => s.Seasons)
            .FirstOrDefaultAsync(s => s.TmdbId == dto.Id, cancellationToken);

        if (existing == null)
        {
            existing = new Series
            {
                TmdbId = dto.Id,
                Title = string.IsNullOrEmpty(dto.Name) ? (dto.OriginalName ?? "عنوان غير معروف") : dto.Name,
                OriginalTitle = dto.OriginalName,
                Overview = dto.Overview,
                PosterUrl = FormatImageUrl(dto.PosterPath),
                BackdropUrl = FormatImageUrl(dto.BackdropPath, isBackdrop: true),
                FirstAirDate = DateTime.TryParse(dto.FirstAirDate, out var ad) ? ad : null,
                Rating = dto.VoteAverage,
                VoteCount = dto.VoteCount,
                Popularity = dto.Popularity,
                OriginalLanguage = dto.OriginalLanguage,
                CreatedAt = DateTime.UtcNow
            };
            _db.Series.Add(existing);
            result.ImportedCount++;
        }
        else
        {
            existing.Title = string.IsNullOrEmpty(dto.Name) ? existing.Title : dto.Name;
            existing.OriginalTitle = dto.OriginalName ?? existing.OriginalTitle;
            existing.Overview = string.IsNullOrEmpty(dto.Overview) ? existing.Overview : dto.Overview;
            existing.PosterUrl = string.IsNullOrEmpty(dto.PosterPath) ? existing.PosterUrl : FormatImageUrl(dto.PosterPath);
            existing.BackdropUrl = string.IsNullOrEmpty(dto.BackdropPath) ? existing.BackdropUrl : FormatImageUrl(dto.BackdropPath, isBackdrop: true);
            existing.Rating = dto.VoteAverage;
            existing.VoteCount = dto.VoteCount;
            existing.Popularity = dto.Popularity;
            existing.UpdatedAt = DateTime.UtcNow;
            result.UpdatedCount++;
        }

        if (dto.GenreIds != null && dto.GenreIds.Any())
        {
            var dbGenres = await _db.Genres
                .Where(g => g.TmdbId.HasValue && dto.GenreIds.Contains(g.TmdbId.Value))
                .ToListAsync(cancellationToken);

            foreach (var g in dbGenres)
            {
                if (!existing.SeriesGenres.Any(sg => sg.GenreId == g.Id))
                {
                    existing.SeriesGenres.Add(new SeriesGenre { GenreId = g.Id });
                }
            }
        }

        return existing;
    }

    private List<TmdbGenreDto> GetFallbackGenres()
    {
        return new List<TmdbGenreDto>
        {
            new() { Id = 28, Name = "أكشن" },
            new() { Id = 12, Name = "مغامرة" },
            new() { Id = 16, Name = "أنيميشن" },
            new() { Id = 35, Name = "كوميديا" },
            new() { Id = 80, Name = "جريمة" },
            new() { Id = 99, Name = "وثائقي" },
            new() { Id = 18, Name = "دراما" },
            new() { Id = 10751, Name = "عائلي" },
            new() { Id = 14, Name = "فانتازيا" },
            new() { Id = 36, Name = "تاريخ" },
            new() { Id = 27, Name = "رعب" },
            new() { Id = 10402, Name = "موسيقى" },
            new() { Id = 9648, Name = "غموض" },
            new() { Id = 10749, Name = "رومانسية" },
            new() { Id = 878, Name = "خيال علمي" },
            new() { Id = 53, Name = "إثارة" },
            new() { Id = 10752, Name = "حرب" },
            new() { Id = 37, Name = "غرب أمريكي" }
        };
    }

    private List<TmdbMovieDto> GetFallbackMovies()
    {
        return new List<TmdbMovieDto>
        {
            new() { Id = 550, Title = "Fight Club", OriginalTitle = "Fight Club", Overview = "فيلم الإثارة والدراما الشهير.", PosterPath = "/pB8O2W16-e-161d.jpg", VoteAverage = 8.4, Popularity = 60.5, ReleaseDate = "1999-10-15", GenreIds = new() { 18, 53 } },
            new() { Id = 27205, Title = "Inception", OriginalTitle = "Inception", Overview = "فيلم الخيال العلمي والأكشن.", PosterPath = "/oYuLEIrLzY19B2L9z92.jpg", VoteAverage = 8.3, Popularity = 90.0, ReleaseDate = "2010-07-16", GenreIds = new() { 28, 878, 12 } },
            new() { Id = 157336, Title = "Interstellar", OriginalTitle = "Interstellar", Overview = "رحلة الفضاء المذهلة بين النجوم.", PosterPath = "/gEU2g-1-2g-1.jpg", VoteAverage = 8.6, Popularity = 95.0, ReleaseDate = "2014-11-05", GenreIds = new() { 12, 18, 878 } }
        };
    }

    private List<TmdbTvShowDto> GetFallbackSeries()
    {
        return new List<TmdbTvShowDto>
        {
            new() { Id = 1396, Name = "Breaking Bad", OriginalName = "Breaking Bad", Overview = "مسلسل الجريمة والدراما الأسطوري.", PosterPath = "/ztkUQFLyC19B2L.jpg", VoteAverage = 8.9, Popularity = 120.0, FirstAirDate = "2008-01-20", GenreIds = new() { 18, 80 } },
            new() { Id = 1399, Name = "Game of Thrones", OriginalName = "Game of Thrones", Overview = "صراع العروش في الممالك السبع.", PosterPath = "/u3bZf2n2j19.jpg", VoteAverage = 8.4, Popularity = 150.0, FirstAirDate = "2011-04-17", GenreIds = new() { 10765, 18 } }
        };
    }
}
