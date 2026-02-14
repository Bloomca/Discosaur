using Discosaur.Models;

namespace Discosaur.Tests;

public class AlbumDisplayNameTests
{
    [Fact]
    public void Uncategorized_ReturnsUncategorized()
    {
        var album = new Album { Name = Album.UncategorizedName };
        Assert.Equal("Uncategorized", album.DisplayName);
    }

    [Fact]
    public void NameOnly_ReturnsName()
    {
        var album = new Album { Name = "OK Computer" };
        Assert.Equal("OK Computer", album.DisplayName);
    }

    [Fact]
    public void NameAndArtist_ReturnsNameWithArtist()
    {
        var album = new Album { Name = "OK Computer", Artist = "Radiohead" };
        Assert.Equal("OK Computer Radiohead", album.DisplayName);
    }

    [Fact]
    public void NameAndYear_ReturnsNameWithYear()
    {
        var album = new Album { Name = "OK Computer", Year = 1997 };
        Assert.Equal("OK Computer (1997)", album.DisplayName);
    }

    [Fact]
    public void NameArtistAndYear_ReturnsFullFormat()
    {
        var album = new Album { Name = "OK Computer", Artist = "Radiohead", Year = 1997 };
        Assert.Equal("OK Computer (Radiohead, 1997)", album.DisplayName);
    }

    [Fact]
    public void EmptyArtist_TreatedAsMissing()
    {
        var album = new Album { Name = "OK Computer", Artist = "", Year = 1997 };
        Assert.Equal("OK Computer (1997)", album.DisplayName);
    }
}
