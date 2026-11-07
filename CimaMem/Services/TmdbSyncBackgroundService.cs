using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CimaMem.Services;

public class TmdbSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TmdbSyncBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public TmdbSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TmdbSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TMDB Sync Background Service has started.");

        int intervalHours = _configuration.GetValue<int>("TmdbSettings:SyncIntervalHours", 12);
        if (intervalHours <= 0) intervalHours = 12;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait interval before first automatic sync or between runs
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);

                _logger.LogInformation("Starting scheduled automatic TMDB Content Background Sync...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var tmdbService = scope.ServiceProvider.GetRequiredService<ITmdbService>();
                    var result = await tmdbService.SyncAllContentAsync(maxPagesPerCategory: 2, cancellationToken: stoppingToken);
                    _logger.LogInformation("TMDB Background Sync completed. Processed: {Processed}, Imported: {Imported}, Updated: {Updated}",
                        result.ProcessedCount, result.ImportedCount, result.UpdatedCount);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("TMDB Sync Background Service is stopping due to task cancellation.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during scheduled TMDB Background Sync.");
            }
        }
    }
}
