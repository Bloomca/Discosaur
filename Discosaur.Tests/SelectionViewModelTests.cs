using System.Collections.ObjectModel;
using Discosaur.Models;
using Discosaur.ViewModels;

namespace Discosaur.Tests;

public class SelectionViewModelTests
{
    private static Album MakeAlbum(string name, params string[] trackTitles)
    {
        var album = new Album { Name = name };
        foreach (var title in trackTitles)
            album.Tracks.Add(new Track { Title = title, FileName = title });
        return album;
    }

    private static (SelectionViewModel vm, ObservableCollection<Album> library) CreateWithTwoAlbums()
    {
        var library = new ObservableCollection<Album>
        {
            MakeAlbum("Album A", "A1", "A2", "A3"),
            MakeAlbum("Album B", "B1", "B2"),
        };
        return (new SelectionViewModel(library), library);
    }

    // --- SelectTrack ---

    [Fact]
    public void SelectTrack_AddsTrackToSelection()
    {
        var (vm, library) = CreateWithTwoAlbums();
        var track = library[0].Tracks[1];

        vm.SelectTrack(track);

        Assert.Single(vm.SelectedTracks);
        Assert.Same(track, vm.SelectedTracks[0]);
    }

    [Fact]
    public void SelectTrack_ReplacePreviousSelection()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]);
        vm.SelectTrack(library[0].Tracks[2]);

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A3", vm.SelectedTracks[0].Title);
    }

    // --- SelectExtraTrack (CTRL-click) ---

    [Fact]
    public void SelectExtraTrack_AddsToExistingSelection()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]);
        vm.SelectExtraTrack(library[0].Tracks[2]);

        Assert.Equal(2, vm.SelectedTracks.Count);
        Assert.Equal("A1", vm.SelectedTracks[0].Title);
        Assert.Equal("A3", vm.SelectedTracks[1].Title);
    }

    [Fact]
    public void SelectExtraTrack_TogglesOffIfAlreadySelected()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]);
        vm.SelectExtraTrack(library[0].Tracks[0]);

        Assert.Empty(vm.SelectedTracks);
    }

    // --- SelectNextTrack ---

    [Fact]
    public void SelectNextTrack_WithNoSelection_SelectsFirstTrack()
    {
        var (vm, library) = CreateWithTwoAlbums();

        vm.SelectNextTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A1", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectNextTrack_MovesWithinAlbum()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]);

        vm.SelectNextTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A2", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectNextTrack_CrossesAlbumBoundary()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[2]); // A3, last in Album A

        vm.SelectNextTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("B1", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectNextTrack_AtEnd_StaysOnLastTrack()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[1].Tracks[1]); // B2, last track overall

        vm.SelectNextTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("B2", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectNextTrack_CollapsesMultiSelection()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]);
        vm.SelectExtraTrack(library[0].Tracks[1]); // A1 + A2 selected

        vm.SelectNextTrack(); // anchor is A2 (last), next is A3

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A3", vm.SelectedTracks[0].Title);
    }

    // --- SelectPreviousTrack ---

    [Fact]
    public void SelectPreviousTrack_WithNoSelection_SelectsLastTrack()
    {
        var (vm, library) = CreateWithTwoAlbums();

        vm.SelectPreviousTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("B2", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectPreviousTrack_MovesWithinAlbum()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[2]);

        vm.SelectPreviousTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A2", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectPreviousTrack_CrossesAlbumBoundary()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[1].Tracks[0]); // B1, first in Album B

        vm.SelectPreviousTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A3", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectPreviousTrack_AtStart_StaysOnFirstTrack()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]); // A1, first track overall

        vm.SelectPreviousTrack();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A1", vm.SelectedTracks[0].Title);
    }

    // --- SelectFirstTrackOfNextAlbum (End key) ---

    [Fact]
    public void SelectFirstTrackOfNextAlbum_JumpsToNextAlbum()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[1]); // A2

        vm.SelectFirstTrackOfNextAlbum();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("B1", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectFirstTrackOfNextAlbum_AtLastAlbum_NoChange()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[1].Tracks[0]); // B1

        vm.SelectFirstTrackOfNextAlbum();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("B1", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectFirstTrackOfNextAlbum_NoSelection_SelectsFirst()
    {
        var (vm, library) = CreateWithTwoAlbums();

        vm.SelectFirstTrackOfNextAlbum();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A1", vm.SelectedTracks[0].Title);
    }

    // --- SelectFirstTrackOfPreviousAlbum (Home key) ---

    [Fact]
    public void SelectFirstTrackOfPreviousAlbum_JumpsToPreviousAlbum()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[1].Tracks[1]); // B2

        vm.SelectFirstTrackOfPreviousAlbum();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A1", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectFirstTrackOfPreviousAlbum_AtFirstAlbum_NoChange()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[1]); // A2

        vm.SelectFirstTrackOfPreviousAlbum();

        Assert.Single(vm.SelectedTracks);
        Assert.Equal("A2", vm.SelectedTracks[0].Title);
    }

    [Fact]
    public void SelectFirstTrackOfPreviousAlbum_NoSelection_DoesNothing()
    {
        var (vm, _) = CreateWithTwoAlbums();

        vm.SelectFirstTrackOfPreviousAlbum();

        Assert.Empty(vm.SelectedTracks);
    }

    // --- SelectAlbum ---

    [Fact]
    public void SelectAlbum_SelectsAllTracks()
    {
        var (vm, library) = CreateWithTwoAlbums();

        vm.SelectAlbum(library[0]);

        Assert.Equal(3, vm.SelectedTracks.Count);
        Assert.Equal("A1", vm.SelectedTracks[0].Title);
        Assert.Equal("A2", vm.SelectedTracks[1].Title);
        Assert.Equal("A3", vm.SelectedTracks[2].Title);
    }

    [Fact]
    public void SelectAlbum_ReplacesPreviousSelection()
    {
        var (vm, library) = CreateWithTwoAlbums();
        vm.SelectTrack(library[0].Tracks[0]);

        vm.SelectAlbum(library[1]);

        Assert.Equal(2, vm.SelectedTracks.Count);
        Assert.Equal("B1", vm.SelectedTracks[0].Title);
        Assert.Equal("B2", vm.SelectedTracks[1].Title);
    }

    // --- Edge cases ---

    [Fact]
    public void EmptyLibrary_SelectNextTrack_DoesNothing()
    {
        var library = new ObservableCollection<Album>();
        var vm = new SelectionViewModel(library);

        vm.SelectNextTrack();

        Assert.Empty(vm.SelectedTracks);
    }

    [Fact]
    public void EmptyLibrary_SelectPreviousTrack_DoesNothing()
    {
        var library = new ObservableCollection<Album>();
        var vm = new SelectionViewModel(library);

        vm.SelectPreviousTrack();

        Assert.Empty(vm.SelectedTracks);
    }

    [Fact]
    public void SingleTrackLibrary_NavigationStaysOnTrack()
    {
        var library = new ObservableCollection<Album> { MakeAlbum("Solo", "Only") };
        var vm = new SelectionViewModel(library);
        vm.SelectTrack(library[0].Tracks[0]);

        vm.SelectNextTrack();
        Assert.Equal("Only", vm.SelectedTracks[0].Title);

        vm.SelectPreviousTrack();
        Assert.Equal("Only", vm.SelectedTracks[0].Title);
    }
}
