using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Discosaur.Models;

public class FilterConfiguration
{
    [JsonPropertyName("albumNameSearch")]
    public string? AlbumNameSearch { get; set; }

    [JsonPropertyName("songNameSearch")]
    public string? SongNameSearch { get; set; }

    [JsonPropertyName("yearFrom")]
    public uint? YearFrom { get; set; }

    [JsonPropertyName("yearTo")]
    public uint? YearTo { get; set; }

    [JsonPropertyName("selectedAlbumNames")]
    public List<string>? SelectedAlbumNames { get; set; }

    [JsonPropertyName("selectedGenres")]
    public List<string>? SelectedGenres { get; set; }

    [JsonIgnore]
    public bool HasAnyCriteria =>
        !string.IsNullOrWhiteSpace(AlbumNameSearch) ||
        !string.IsNullOrWhiteSpace(SongNameSearch) ||
        YearFrom.HasValue ||
        YearTo.HasValue ||
        (SelectedAlbumNames is { Count: > 0 }) ||
        (SelectedGenres is { Count: > 0 });
}
