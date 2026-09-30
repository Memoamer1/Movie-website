using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
