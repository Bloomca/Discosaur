using System.Collections.Generic;
using Discosaur.Models;
using Discosaur.Services;

namespace Discosaur.Tests;

public class FilterTests
{
    private static Album MakeAlbum(string name, string? artist = null,
        uint? year = null, params (string title, string? genre)[] tracks)
    {
        var album = new Album { Name = name, Artist = artist, Year = year };
        foreach (var (title, genre) in tracks)
            album.Tracks.Add(new Track { Title = title, FileName = title, Genre = genre });
        return album;
    }

    // --- HasAnyCriteria ---

    [Fact]
    public void HasAnyCriteria_FalseWhenEmpty()
    {
        var config = new FilterConfiguration();
        Assert.False(config.HasAnyCriteria);
    }

    [Fact]
    public void HasAnyCriteria_TrueWithAlbumSearch()
    {
        var config = new FilterConfiguration { AlbumNameSearch = "test" };
        Assert.True(config.HasAnyCriteria);
    }

    [Fact]
    public void HasAnyCriteria_TrueWithSongSearch()
    {
        var config = new FilterConfiguration { SongNameSearch = "song" };
        Assert.True(config.HasAnyCriteria);
    }

    [Fact]
    public void HasAnyCriteria_TrueWithYearFrom()
    {
        var config = new FilterConfiguration { YearFrom = 2000 };
        Assert.True(config.HasAnyCriteria);
    }

    [Fact]
    public void HasAnyCriteria_TrueWithSelectedAlbums()
    {
        var config = new FilterConfiguration { SelectedAlbumNames = ["Album A"] };
        Assert.True(config.HasAnyCriteria);
    }

    [Fact]
    public void HasAnyCriteria_FalseWithEmptyLists()
    {
        var config = new FilterConfiguration
        {
            SelectedAlbumNames = [],
            SelectedGenres = []
        };
        Assert.False(config.HasAnyCriteria);
    }

    // --- Album name search ---

    [Fact]
    public void AlbumNameSearch_FiltersAlbums()
    {
        var library = new List<Album>
        {
            MakeAlbum("OK Computer", tracks: [("Airbag", null)]),
            MakeAlbum("The Bends", tracks: [("High and Dry", null)]),
        };
        var config = new FilterConfiguration { AlbumNameSearch = "Computer" };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("OK Computer", result[0].Name);
    }

    [Fact]
    public void AlbumNameSearch_CaseInsensitive()
    {
        var library = new List<Album>
        {
            MakeAlbum("OK Computer", tracks: [("Airbag", null)]),
        };
        var config = new FilterConfiguration { AlbumNameSearch = "computer" };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
    }

    // --- Song name search ---

    [Fact]
    public void SongNameSearch_ProducesPartialAlbum()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", tracks: [("Track A", null), ("Track B", null), ("Other", null)]),
        };
        var config = new FilterConfiguration { SongNameSearch = "Track" };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal(2, result[0].Tracks.Count);
    }

    [Fact]
    public void SongNameSearch_SharedTrackReferences()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", tracks: [("Track A", null)]),
        };
        var config = new FilterConfiguration { SongNameSearch = "Track" };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Same(library[0].Tracks[0], result[0].Tracks[0]);
    }

    [Fact]
    public void SongNameSearch_NoMatchingTracks_ExcludesAlbum()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", tracks: [("Song A", null), ("Song B", null)]),
        };
        var config = new FilterConfiguration { SongNameSearch = "XYZ" };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Empty(result);
    }

    // --- Year range ---

    [Fact]
    public void YearRange_FiltersAlbums()
    {
        var library = new List<Album>
        {
            MakeAlbum("A", year: 1990, tracks: [("T1", null)]),
            MakeAlbum("B", year: 2000, tracks: [("T2", null)]),
            MakeAlbum("C", year: 2010, tracks: [("T3", null)]),
        };
        var config = new FilterConfiguration { YearFrom = 1995, YearTo = 2005 };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("B", result[0].Name);
    }

    [Fact]
    public void YearFrom_Only()
    {
        var library = new List<Album>
        {
            MakeAlbum("Old", year: 1980, tracks: [("T1", null)]),
            MakeAlbum("New", year: 2020, tracks: [("T2", null)]),
        };
        var config = new FilterConfiguration { YearFrom = 2000 };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("New", result[0].Name);
    }

    [Fact]
    public void YearTo_Only()
    {
        var library = new List<Album>
        {
            MakeAlbum("Old", year: 1980, tracks: [("T1", null)]),
            MakeAlbum("New", year: 2020, tracks: [("T2", null)]),
        };
        var config = new FilterConfiguration { YearTo = 2000 };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("Old", result[0].Name);
    }

    [Fact]
    public void YearRange_ExcludesAlbumsWithoutYear()
    {
        var library = new List<Album>
        {
            MakeAlbum("No Year", year: null, tracks: [("T1", null)]),
            MakeAlbum("Has Year", year: 2000, tracks: [("T2", null)]),
        };
        var config = new FilterConfiguration { YearFrom = 1990, YearTo = 2010 };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("Has Year", result[0].Name);
    }

    // --- Specific albums ---

    [Fact]
    public void SelectedAlbumNames_FiltersToSpecificAlbums()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album A", tracks: [("T1", null)]),
            MakeAlbum("Album B", tracks: [("T2", null)]),
            MakeAlbum("Album C", tracks: [("T3", null)]),
        };
        var config = new FilterConfiguration { SelectedAlbumNames = ["Album A", "Album C"] };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Equal(2, result.Count);
        Assert.Equal("Album A", result[0].Name);
        Assert.Equal("Album C", result[1].Name);
    }

    // --- Genre filter ---

    [Fact]
    public void GenreFilter_ProducesPartialAlbum()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", tracks: [("Rock Song", "Rock"), ("Jazz Song", "Jazz")]),
        };
        var config = new FilterConfiguration { SelectedGenres = ["Rock"] };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Single(result[0].Tracks);
        Assert.Equal("Rock Song", result[0].Tracks[0].Title);
    }

    [Fact]
    public void GenreFilter_MultipleGenres()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", tracks: [("Rock Song", "Rock"), ("Jazz Song", "Jazz"), ("Pop Song", "Pop")]),
        };
        var config = new FilterConfiguration { SelectedGenres = ["Rock", "Jazz"] };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal(2, result[0].Tracks.Count);
    }

    // --- AND logic ---

    [Fact]
    public void AllCriteriaAreAnded()
    {
        var library = new List<Album>
        {
            MakeAlbum("Rock Album", year: 2000, tracks: [("Rock Track", "Rock")]),
            MakeAlbum("Jazz Album", year: 2000, tracks: [("Jazz Track", "Jazz")]),
        };
        var config = new FilterConfiguration
        {
            AlbumNameSearch = "Rock",
            SelectedGenres = ["Rock"]
        };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("Rock Album", result[0].Name);
    }

    [Fact]
    public void AlbumAndSongSearchCombined()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album A", tracks: [("Good Song", null), ("Bad Song", null)]),
            MakeAlbum("Album B", tracks: [("Good Song", null)]),
        };
        var config = new FilterConfiguration
        {
            AlbumNameSearch = "Album A",
            SongNameSearch = "Good"
        };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Single(result);
        Assert.Equal("Album A", result[0].Name);
        Assert.Single(result[0].Tracks);
        Assert.Equal("Good Song", result[0].Tracks[0].Title);
    }

    // --- New album objects ---

    [Fact]
    public void NewAlbumObjects_CreatedForFilteredResults()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", tracks: [("T1", null)]),
        };
        var config = new FilterConfiguration { AlbumNameSearch = "Album" };

        var result = LibraryService.ApplyFilter(library, config);

        Assert.NotSame(library[0], result[0]);
    }

    [Fact]
    public void FilteredAlbum_PreservesMetadata()
    {
        var library = new List<Album>
        {
            MakeAlbum("Album", artist: "Artist", year: 2020, tracks: [("T1", null)]),
        };
        library[0].FolderToken = "token123";
        library[0].CoverArtPath = "/art/cover.jpg";

        var config = new FilterConfiguration { AlbumNameSearch = "Album" };
        var result = LibraryService.ApplyFilter(library, config);

        Assert.Equal("Artist", result[0].Artist);
        Assert.Equal((uint)2020, result[0].Year);
        Assert.Equal("token123", result[0].FolderToken);
        Assert.Equal("/art/cover.jpg", result[0].CoverArtPath);
    }

    // --- Empty / no-filter cases ---

    [Fact]
    public void EmptyConfig_ReturnsAllAlbumsWithAllTracks()
    {
        var library = new List<Album>
        {
            MakeAlbum("A", tracks: [("T1", null), ("T2", null)]),
            MakeAlbum("B", tracks: [("T3", null)]),
        };
        var config = new FilterConfiguration();

        var result = LibraryService.ApplyFilter(library, config);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Tracks.Count);
        Assert.Single(result[1].Tracks);
    }
}
