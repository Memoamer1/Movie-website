using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Favorite
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int? MovieId { get; set; }
    public Movie? Movie { get; set; }

    public int? SeriesId { get; set; }
    public Series? Series { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
