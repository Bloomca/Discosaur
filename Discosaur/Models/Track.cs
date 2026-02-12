using System;

namespace Discosaur.Models;

public class Track
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public string? AlbumTitle { get; set; }
    public uint? TrackNumber { get; set; }

    public string TrackNumberDisplay => TrackNumber == null ? "" : $"#{TrackNumber}";
    public uint? Year { get; set; }
    public string? Genre { get; set; }
    public TimeSpan? Duration { get; set; }

    public string DurationDisplay => Duration switch
    {
        null => "",
        { Hours: > 0 } d => d.ToString(@"h\:mm\:ss"),
        { } d => d.ToString(@"m\:ss")
    };
}
