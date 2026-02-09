using System.Collections.ObjectModel;

namespace Discosaur.Models;

public class Album
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<Track> Tracks { get; set; } = [];
}
