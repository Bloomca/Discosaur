using System.Collections.ObjectModel;
using Discosaur.Models;
using Discosaur.Services;

namespace Discosaur.Tests;

public class LibraryServiceTests
{
    private static Album MakeAlbum(string name, params string[] trackTitles)
    {
        var album = new Album { Name = name };
        foreach (var title in trackTitles)
            album.Tracks.Add(new Track { Title = title, FileName = title });
        return album;
    }

    private static ObservableCollection<Album> TwoAlbumLibrary() => new()
    {
        MakeAlbum("Album A", "A1", "A2", "A3"),
        MakeAlbum("Album B", "B1", "B2"),
    };

    // --- FindNextTrack ---

    [Fact]
    public void FindNextTrack_MiddleOfAlbum_ReturnsNext()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindNextTrack(lib[0].Tracks[0], lib);
        Assert.Equal("A2", result?.Title);
    }

    [Fact]
    public void FindNextTrack_EndOfAlbum_ReturnsFirstOfNextAlbum()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindNextTrack(lib[0].Tracks[2], lib);
        Assert.Equal("B1", result?.Title);
    }

    [Fact]
    public void FindNextTrack_LastTrackOverall_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindNextTrack(lib[1].Tracks[1], lib);
        Assert.Null(result);
    }

    [Fact]
    public void FindNextTrack_NullCurrent_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        Assert.Null(LibraryService.FindNextTrack(null, lib));
    }

    [Fact]
    public void FindNextTrack_EmptyLibrary_ReturnsNull()
    {
        var lib = new ObservableCollection<Album>();
        var orphan = new Track { Title = "X" };
        Assert.Null(LibraryService.FindNextTrack(orphan, lib));
    }

    [Fact]
    public void FindNextTrack_TrackNotInLibrary_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        var orphan = new Track { Title = "Orphan" };
        Assert.Null(LibraryService.FindNextTrack(orphan, lib));
    }

    [Fact]
    public void FindNextTrack_SingleTrackAlbum_CrossesToNextAlbum()
    {
        var lib = new ObservableCollection<Album>
        {
            MakeAlbum("Single", "S1"),
            MakeAlbum("Other", "O1"),
        };
        var result = LibraryService.FindNextTrack(lib[0].Tracks[0], lib);
        Assert.Equal("O1", result?.Title);
    }

    // --- FindPreviousTrack ---

    [Fact]
    public void FindPreviousTrack_MiddleOfAlbum_ReturnsPrevious()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindPreviousTrack(lib[0].Tracks[2], lib);
        Assert.Equal("A2", result?.Title);
    }

    [Fact]
    public void FindPreviousTrack_StartOfAlbum_ReturnsLastOfPreviousAlbum()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindPreviousTrack(lib[1].Tracks[0], lib);
        Assert.Equal("A3", result?.Title);
    }

    [Fact]
    public void FindPreviousTrack_FirstTrackOverall_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindPreviousTrack(lib[0].Tracks[0], lib);
        Assert.Null(result);
    }

    [Fact]
    public void FindPreviousTrack_NullCurrent_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        Assert.Null(LibraryService.FindPreviousTrack(null, lib));
    }

    [Fact]
    public void FindPreviousTrack_EmptyLibrary_ReturnsNull()
    {
        var lib = new ObservableCollection<Album>();
        var orphan = new Track { Title = "X" };
        Assert.Null(LibraryService.FindPreviousTrack(orphan, lib));
    }

    [Fact]
    public void FindPreviousTrack_TrackNotInLibrary_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        var orphan = new Track { Title = "Orphan" };
        Assert.Null(LibraryService.FindPreviousTrack(orphan, lib));
    }

    // --- FindAlbumForTrack ---

    [Fact]
    public void FindAlbumForTrack_ReturnsCorrectAlbum()
    {
        var lib = TwoAlbumLibrary();
        var result = LibraryService.FindAlbumForTrack(lib[1].Tracks[0], lib);
        Assert.Equal("Album B", result?.Name);
    }

    [Fact]
    public void FindAlbumForTrack_NullTrack_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        Assert.Null(LibraryService.FindAlbumForTrack(null, lib));
    }

    [Fact]
    public void FindAlbumForTrack_TrackNotInLibrary_ReturnsNull()
    {
        var lib = TwoAlbumLibrary();
        var orphan = new Track { Title = "Orphan" };
        Assert.Null(LibraryService.FindAlbumForTrack(orphan, lib));
    }

    // --- Navigation consistency ---

    [Fact]
    public void FindNextThenPrevious_ReturnsSameTrack()
    {
        var lib = TwoAlbumLibrary();
        var start = lib[0].Tracks[1]; // A2
        var next = LibraryService.FindNextTrack(start, lib);
        var backAgain = LibraryService.FindPreviousTrack(next, lib);
        Assert.Same(start, backAgain);
    }

    [Fact]
    public void FindNextThenPrevious_AcrossAlbums_ReturnsSameTrack()
    {
        var lib = TwoAlbumLibrary();
        var start = lib[0].Tracks[2]; // A3, last in Album A
        var next = LibraryService.FindNextTrack(start, lib);
        Assert.Equal("B1", next?.Title);
        var backAgain = LibraryService.FindPreviousTrack(next, lib);
        Assert.Same(start, backAgain);
    }

    [Fact]
    public void FullForwardTraversal_VisitsAllTracks()
    {
        var lib = TwoAlbumLibrary();
        var visited = new List<string>();
        var current = lib[0].Tracks[0];

        while (current != null)
        {
            visited.Add(current.Title);
            current = LibraryService.FindNextTrack(current, lib);
        }

        Assert.Equal(["A1", "A2", "A3", "B1", "B2"], visited);
    }

    [Fact]
    public void FullBackwardTraversal_VisitsAllTracks()
    {
        var lib = TwoAlbumLibrary();
        var visited = new List<string>();
        var current = lib[1].Tracks[1]; // B2, last track

        while (current != null)
        {
            visited.Add(current.Title);
            current = LibraryService.FindPreviousTrack(current, lib);
        }

        Assert.Equal(["B2", "B1", "A3", "A2", "A1"], visited);
    }
}
