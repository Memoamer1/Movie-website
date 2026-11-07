using CimaMem.Entities;

namespace CimaMem.Services;

public class TmdbSyncResult
{
    public string OperationType { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string Status { get; set; } = "Completed";
    public List<string> Messages { get; set; } = new();
}

public interface ITmdbService
{
    Task<TmdbSyncResult> SyncGenresAsync(CancellationToken cancellationToken = default);
    Task<TmdbSyncResult> SyncMoviesCategoryAsync(string categoryType, int maxPages = 1, CancellationToken cancellationToken = default);
    Task<TmdbSyncResult> DiscoverMoviesAsync(int maxPages = 1, string? sortBy = "popularity.desc", int? genreId = null, CancellationToken cancellationToken = default);
    Task<TmdbSyncResult> SyncSeriesCategoryAsync(string categoryType, int maxPages = 1, CancellationToken cancellationToken = default);
    Task<TmdbSyncResult> DiscoverSeriesAsync(int maxPages = 1, string? sortBy = "popularity.desc", int? genreId = null, CancellationToken cancellationToken = default);
    Task<Movie?> ImportMovieByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default);
    Task<Series?> ImportSeriesByTmdbIdAsync(int tmdbId, CancellationToken cancellationToken = default);
    Task<TmdbSyncResult> SyncAllContentAsync(int maxPagesPerCategory = 5, CancellationToken cancellationToken = default);
    Task<List<ImportLog>> GetRecentImportLogsAsync(int count = 10);
}
