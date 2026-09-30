using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class WatchHistory
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    public double ProgressSeconds { get; set; }
    public double DurationSeconds { get; set; }

    public bool IsFinished { get; set; }

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
}
