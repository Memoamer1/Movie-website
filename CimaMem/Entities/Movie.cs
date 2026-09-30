using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Movie
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

    [MaxLength(500)]
    public string? TrailerUrl { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? Runtime { get; set; }

    public double Rating { get; set; }

    public int VoteCount { get; set; }

    public double Popularity { get; set; }

    [MaxLength(50)]
    public string? OriginalLanguage { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(255)]
    public string? Director { get; set; }

    public string? Cast { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
