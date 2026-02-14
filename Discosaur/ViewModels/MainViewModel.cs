using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discosaur.Models;
using Discosaur.Services;
using Windows.Storage.AccessCache;

namespace Discosaur.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LibraryService _libraryService;

    public SelectionViewModel SelectionViewModel { get; private set; }
    public PlaybackViewModel PlaybackViewModel { get; private set; }

    public ObservableCollection<Album> Library { get; } = [];
    public ObservableCollection<Track> Uncategorized { get; } = [];

    // --- Filtering ---

    [ObservableProperty]
    private bool _isFilteringApplied;

    [ObservableProperty]
    private FilterConfiguration? _filterConfiguration;

    public ObservableCollection<Album> FilteredLibrary { get; } = [];

    public IReadOnlyList<Album> GetActiveLibrary()
        => IsFilteringApplied ? FilteredLibrary : Library;

    // --- UI state ---

    [ObservableProperty]
    private bool _isLibraryExpanded = true;

    [ObservableProperty]
    private bool _isAlwaysOnTop;

    public MainViewModel(AudioPlayerService audioPlayer, LibraryService libraryService)
    {
        _libraryService = libraryService;

        SelectionViewModel = new SelectionViewModel(GetActiveLibrary);
        PlaybackViewModel = new PlaybackViewModel(audioPlayer, GetActiveLibrary);
    }

    public void AddFolderToLibrary(string folderPath, string? folderToken = null)
    {
        var albums = _libraryService.ScanFolder(folderPath);

        foreach (var album in albums)
        {
            if (album.Tracks.Count == 0)
                continue;

            if (album.IsUncategorized)
            {
                foreach (var track in album.Tracks)
                    Uncategorized.Add(track);
            }
            else
            {
                album.FolderToken = folderToken;
                Library.Add(album);
            }
        }

        if (IsFilteringApplied)
            RebuildFilteredLibrary();

        App.StatePersister.ScheduleSave();
    }

    // --- Filter commands ---

    public void ApplyFilter(FilterConfiguration config)
    {
        FilterConfiguration = config;
        RebuildFilteredLibrary();
        IsFilteringApplied = true;
        App.StatePersister.ScheduleSave();
    }

    public void ClearFilter()
    {
        FilterConfiguration = null;
        FilteredLibrary.Clear();
        IsFilteringApplied = false;
        App.StatePersister.ScheduleSave();
    }

    public void RebuildFilteredLibrary()
    {
        if (FilterConfiguration == null || !FilterConfiguration.HasAnyCriteria)
        {
            FilteredLibrary.Clear();
            IsFilteringApplied = false;
            return;
        }

        var filtered = LibraryService.ApplyFilter(Library, FilterConfiguration);
        FilteredLibrary.Clear();
        foreach (var album in filtered)
            FilteredLibrary.Add(album);
    }

    // --- Selection / Playback ---

    [RelayCommand]
    private void PlaySelectedTrack()
    {
        var track = SelectionViewModel.SelectedTracks.FirstOrDefault();
        if (track != null)
            PlaybackViewModel.PlayTrackCommand.Execute(track);
    }

    [RelayCommand]
    private void DeleteSelectedTracks()
    {
        if (SelectionViewModel.SelectedTracks.Count == 0) return;

        bool wasPlaying = PlaybackViewModel.CurrentTrack != null
            && SelectionViewModel.SelectedTracks.Contains(PlaybackViewModel.CurrentTrack);

        // so we can remove it from selected tracks collection in place
        var selectedTracksCopy = SelectionViewModel.SelectedTracks.ToList();

        for (int i = 0; i < Library.Count; i++)
        {
            if (SelectionViewModel.SelectedTracks.Count == 0) break;
            foreach (var selectedTrack in selectedTracksCopy) {
                if (SelectionViewModel.SelectedTracks.Count == 0) break;
                if (Library[i].Tracks.Remove(selectedTrack))
                {
                    if (Library[i].Tracks.Count == 0)
                    {
                        var removedAlbum = Library[i];
                        Library.RemoveAt(i);

                        // Release FutureAccessList token if no other album uses it
                        if (!string.IsNullOrEmpty(removedAlbum.FolderToken)
                            && !Library.Any(a => a.FolderToken == removedAlbum.FolderToken))
                        {
                            try { StorageApplicationPermissions.FutureAccessList.Remove(removedAlbum.FolderToken); }
                            catch { }
                        }
                    }
                    SelectionViewModel.SelectedTracks.Remove(selectedTrack);
                }
            }
        }

        if (IsFilteringApplied)
            RebuildFilteredLibrary();

        if (wasPlaying)
        {
            PlaybackViewModel.StopCommand.Execute(null);
        }

        App.StatePersister.ScheduleSave();
    }

    [RelayCommand]
    private void ToggleLibraryExpanded()
    {
        IsLibraryExpanded = !IsLibraryExpanded;
    }

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
    }

    // --- Lifecycle ---

    public void Shutdown()
    {
        PlaybackViewModel.Shutdown();
    }
}
