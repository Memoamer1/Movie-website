using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Genre
{
    public int Id { get; set; }

    public int? TmdbId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<SeriesGenre> SeriesGenres { get; set; } = new List<SeriesGenre>();
}
