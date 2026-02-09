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
    public uint? Year { get; set; }
    public string? Genre { get; set; }
    public TimeSpan? Duration { get; set; }
}
