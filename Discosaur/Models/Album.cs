using System.Collections.ObjectModel;

namespace Discosaur.Models;

public class Album
{
    /// <summary>
    /// Internal name used for albums containing tracks with no parseable metadata.
    /// Used to identify and merge uncategorized tracks across multiple folder scans.
    /// </summary>
    public static string UncategorizedName => "Uncategorized";

    public bool IsUncategorized => Name == UncategorizedName;

    public string Name { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public uint? Year { get; set; }
    public string? CoverArtPath { get; set; }
    public ObservableCollection<Track> Tracks { get; set; } = [];
}
