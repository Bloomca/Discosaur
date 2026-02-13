using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Discosaur.Models;

public class AppState
{
    [JsonPropertyName("settings")]
    public AppSettings Settings { get; set; } = new();

    [JsonPropertyName("playlist")]
    public List<PersistedAlbum> Playlist { get; set; } = [];

    [JsonPropertyName("currentTrack")]
    public PersistedCurrentTrack? CurrentTrack { get; set; }
}

public class AppSettings
{
    [JsonPropertyName("volumeLevel")]
    public int VolumeLevel { get; set; } = 100;

    [JsonPropertyName("reducedVolumeLevel")]
    public int ReducedVolumeLevel { get; set; } = 30;
}

public class PersistedAlbum
{
    [JsonPropertyName("folderToken")]
    public string FolderToken { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("year")]
    public uint? Year { get; set; }

    [JsonPropertyName("coverArtFileName")]
    public string? CoverArtFileName { get; set; }

    [JsonPropertyName("tracks")]
    public List<PersistedTrack> Tracks { get; set; } = [];
}

public class PersistedTrack
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    [JsonPropertyName("albumTitle")]
    public string? AlbumTitle { get; set; }

    [JsonPropertyName("trackNumber")]
    public uint? TrackNumber { get; set; }

    [JsonPropertyName("year")]
    public uint? Year { get; set; }

    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    [JsonPropertyName("durationTicks")]
    public long? DurationTicks { get; set; }
}

public class PersistedCurrentTrack
{
    [JsonPropertyName("folderToken")]
    public string FolderToken { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;
}
