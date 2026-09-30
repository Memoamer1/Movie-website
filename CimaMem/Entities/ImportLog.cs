using System.ComponentModel.DataAnnotations;

namespace CimaMem.Entities;

public class ImportLog
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty; // Movies_Popular, Movies_TopRated, Series_Popular, SyncEverything, etc.

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public int Processed { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Running"; // Running, Completed, Partial, Failed

    public string? ErrorMessage { get; set; }
}
