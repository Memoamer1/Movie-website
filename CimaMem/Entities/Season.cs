using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Season
{
    public int Id { get; set; }

    public int? TmdbId { get; set; }

    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;

    public int SeasonNumber { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    public string? Overview { get; set; }

    [MaxLength(500)]
    public string? PosterUrl { get; set; }

    public DateTime? AirDate { get; set; }

    public int EpisodeCount { get; set; }

    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
}
