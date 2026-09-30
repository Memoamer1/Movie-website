using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Review
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? SeriesId { get; set; }
    public Series? Series { get; set; }

    [Range(1, 10)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
