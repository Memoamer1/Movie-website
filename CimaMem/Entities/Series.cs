using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Series
{
    public int Id { get; set; }

    public int? TmdbId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? OriginalTitle { get; set; }

    [MaxLength(255)]
    public string? ArabicTitle { get; set; }

    public string? Overview { get; set; }

    [MaxLength(500)]
    public string? PosterUrl { get; set; }

    [MaxLength(500)]
    public string? BackdropUrl { get; set; }

    public double Rating { get; set; }

    public int VoteCount { get; set; }

    public double Popularity { get; set; }

    public DateTime? FirstAirDate { get; set; }

    [MaxLength(50)]
    public string? OriginalLanguage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Season> Seasons { get; set; } = new List<Season>();
    public ICollection<SeriesGenre> SeriesGenres { get; set; } = new List<SeriesGenre>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
