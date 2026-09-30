using CimaMem.Entities;

namespace CimaMem.Services;

public class TmdbSyncResult
{
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Messages { get; set; } = new();
}

public interface ITmdbService
{
    Task<TmdbSyncResult> SyncGenresAsync();
    Task<TmdbSyncResult> SyncMoviesCategoryAsync(string categoryType, int page = 1);
    Task<TmdbSyncResult> SyncSeriesCategoryAsync(string categoryType, int page = 1);
    Task<Movie?> ImportMovieByTmdbIdAsync(int tmdbId);
    Task<Series?> ImportSeriesByTmdbIdAsync(int tmdbId);
}
