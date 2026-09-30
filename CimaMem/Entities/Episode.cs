using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Episode
{
    public int Id { get; set; }

    public int? TmdbId { get; set; }

    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public int EpisodeNumber { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    public string? Overview { get; set; }

    [MaxLength(500)]
    public string? StillUrl { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    public int? Duration { get; set; }

    public DateTime? AirDate { get; set; }

    public double Rating { get; set; }

    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
