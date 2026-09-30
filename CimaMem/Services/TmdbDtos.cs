using System.Text.Json.Serialization;

namespace CimaMem.Services;

public class TmdbPagedResult<T>
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}

public class TmdbGenreDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class TmdbGenreListResponse
{
    [JsonPropertyName("genres")]
    public List<TmdbGenreDto> Genres { get; set; } = new();
}

public class TmdbMovieDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string? OriginalTitle { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("original_language")]
    public string? OriginalLanguage { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("genre_ids")]
    public List<int> GenreIds { get; set; } = new();
}

public class TmdbTvShowDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("original_name")]
    public string? OriginalName { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("first_air_date")]
    public string? FirstAirDate { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("vote_count")]
    public int VoteCount { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("original_language")]
    public string? OriginalLanguage { get; set; }

    [JsonPropertyName("genre_ids")]
    public List<int> GenreIds { get; set; } = new();
}

public class TmdbTvDetailDto : TmdbTvShowDto
{
    [JsonPropertyName("number_of_seasons")]
    public int NumberOfSeasons { get; set; }

    [JsonPropertyName("seasons")]
    public List<TmdbSeasonDto> Seasons { get; set; } = new();
}

public class TmdbSeasonDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("season_number")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("air_date")]
    public string? AirDate { get; set; }

    [JsonPropertyName("episode_count")]
    public int EpisodeCount { get; set; }

    [JsonPropertyName("episodes")]
    public List<TmdbEpisodeDto>? Episodes { get; set; }
}

public class TmdbEpisodeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("episode_number")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("still_path")]
    public string? StillPath { get; set; }

    [JsonPropertyName("air_date")]
    public string? AirDate { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }
}
