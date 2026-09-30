using Microsoft.AspNetCore.Identity;

namespace CimaMem.Entities;

public class ApplicationUser : IdentityUser
{
    public string? AvatarUrl { get; set; }

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
