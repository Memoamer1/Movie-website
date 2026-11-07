using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CimaMem.Entities;

namespace CimaMem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<SeriesGenre> SeriesGenres => Set<SeriesGenre>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<WatchHistory> WatchHistories => Set<WatchHistory>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // MovieGenre Many-to-Many Composite Key
        builder.Entity<MovieGenre>()
            .HasKey(mg => new { mg.MovieId, mg.GenreId });

        builder.Entity<MovieGenre>()
            .HasOne(mg => mg.Movie)
            .WithMany(m => m.MovieGenres)
            .HasForeignKey(mg => mg.MovieId);

        builder.Entity<MovieGenre>()
            .HasOne(mg => mg.Genre)
            .WithMany(g => g.MovieGenres)
            .HasForeignKey(mg => mg.GenreId);

        // SeriesGenre Many-to-Many Composite Key
        builder.Entity<SeriesGenre>()
            .HasKey(sg => new { sg.SeriesId, sg.GenreId });

        builder.Entity<SeriesGenre>()
            .HasOne(sg => sg.Series)
            .WithMany(s => s.SeriesGenres)
            .HasForeignKey(sg => sg.SeriesId);

        builder.Entity<SeriesGenre>()
            .HasOne(sg => sg.Genre)
            .WithMany(g => g.SeriesGenres)
            .HasForeignKey(sg => sg.GenreId);

        // Indexes for Optimization (PHASE 24)
        builder.Entity<Movie>()
            .HasIndex(m => m.TmdbId)
            .IsUnique()
            .HasFilter("[TmdbId] IS NOT NULL");

        builder.Entity<Movie>()
            .HasIndex(m => m.ReleaseDate);

        builder.Entity<Movie>()
            .HasIndex(m => m.Popularity);

        builder.Entity<Movie>()
            .HasIndex(m => m.Rating);

        builder.Entity<Series>()
            .HasIndex(s => s.TmdbId)
            .IsUnique()
            .HasFilter("[TmdbId] IS NOT NULL");

        builder.Entity<Series>()
            .HasIndex(s => s.FirstAirDate);

        builder.Entity<Series>()
            .HasIndex(s => s.Popularity);

        builder.Entity<Series>()
            .HasIndex(s => s.Rating);

        builder.Entity<Season>()
            .HasIndex(s => s.TmdbId);

        builder.Entity<Episode>()
            .HasIndex(e => e.TmdbId);

        builder.Entity<Genre>()
            .HasIndex(g => g.TmdbId);

        // Relationships & Foreign Key Constraints
        builder.Entity<Season>()
            .HasOne(s => s.Series)
            .WithMany(s => s.Seasons)
            .HasForeignKey(s => s.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Episode>()
            .HasOne(e => e.Season)
            .WithMany(s => s.Episodes)
            .HasForeignKey(e => e.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Avoid multiple cascade paths in SQL Server
        builder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.Movie)
            .WithMany(m => m.Reviews)
            .HasForeignKey(r => r.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.Series)
            .WithMany(s => s.Reviews)
            .HasForeignKey(s => s.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Favorite>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Favorite>()
            .HasOne(f => f.Movie)
            .WithMany(m => m.Favorites)
            .HasForeignKey(f => f.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Favorite>()
            .HasOne(f => f.Series)
            .WithMany(s => s.Favorites)
            .HasForeignKey(s => s.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WatchHistory>()
            .HasOne(w => w.User)
            .WithMany(u => u.WatchHistories)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WatchHistory>()
            .HasOne(w => w.Movie)
            .WithMany(m => m.WatchHistories)
            .HasForeignKey(w => w.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WatchHistory>()
            .HasOne(w => w.Episode)
            .WithMany(e => e.WatchHistories)
            .HasForeignKey(w => w.EpisodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
