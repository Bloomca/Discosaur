using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discosaur.Models;
using Discosaur.Services;
using Windows.Storage.AccessCache;

namespace Discosaur.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AudioPlayerService _audioPlayer;
    private readonly LibraryService _libraryService;
    private static readonly Random _random = new();

    public SelectionViewModel SelectionViewModel { get; private set; }

    public ObservableCollection<Album> Library { get; } = [];
    public ObservableCollection<Track> Uncategorized { get; } = [];

    [ObservableProperty]
    private Track? _currentTrack;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string? _currentAlbumCoverArtPath;

    [ObservableProperty]
    private RepeatMode _repeatMode;

    [ObservableProperty]
    private bool _isShuffleEnabled;

    [ObservableProperty]
    private bool _isLibraryExpanded = true;

    [ObservableProperty]
    private bool _isAlwaysOnTop;

    public string? NextTrackTitle => LibraryService.FindNextTrack(CurrentTrack, Library)?.Title;
    public string? PreviousTrackTitle => LibraryService.FindPreviousTrack(CurrentTrack, Library)?.Title;

    public MainViewModel(AudioPlayerService audioPlayer, LibraryService libraryService)
    {
        _audioPlayer = audioPlayer;
        _libraryService = libraryService;

        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        _audioPlayer.TrackEnded += OnTrackEnded;

        this.SelectionViewModel = new SelectionViewModel(Library);
    }

    private void OnPlaybackStateChanged()
    {
        CurrentTrack = _audioPlayer.CurrentTrack;
        IsPlaying = _audioPlayer.IsPlaying;
        CurrentAlbumCoverArtPath = FindAlbumForTrack(CurrentTrack)?.CoverArtPath;
        OnPropertyChanged(nameof(NextTrackTitle));
        OnPropertyChanged(nameof(PreviousTrackTitle));
        App.StatePersister.ScheduleSave();
    }

    private async void OnTrackEnded()
    {
        if (RepeatMode == RepeatMode.Track)
        {
            if (_audioPlayer.CurrentTrack != null)
                await _audioPlayer.PlayAsync(_audioPlayer.CurrentTrack);
            return;
        }

        if (IsShuffleEnabled)
        {
            await PlayRandomTrackAsync();
            return;
        }

        var next = LibraryService.FindNextTrack(_audioPlayer.CurrentTrack, Library);

        if (next == null && RepeatMode == RepeatMode.Album)
        {
            var album = FindAlbumForTrack(_audioPlayer.CurrentTrack);
            next = album?.Tracks.FirstOrDefault();
        }

        if (next != null)
            await _audioPlayer.PlayAsync(next);
        else
            _audioPlayer.Stop();
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

        App.StatePersister.ScheduleSave();
    }

    [RelayCommand]
    private async Task PlaySelectedTrack()
    {
        var track = SelectionViewModel.SelectedTracks.FirstOrDefault();
        if (track != null)
            await _audioPlayer.PlayAsync(track);
    }

    [RelayCommand]
    private void DeleteSelectedTracks()
    {
        if (SelectionViewModel.SelectedTracks.Count == 0) return;

        bool wasPlaying = CurrentTrack == null
            ? false
            : SelectionViewModel.SelectedTracks.Contains(CurrentTrack);

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

        if (wasPlaying)
        {
            // For simplicity, maybe will revise in the future
            _audioPlayer.Stop();
        }

        App.StatePersister.ScheduleSave();
    }

    // --- Playback ---

    [RelayCommand]
    private async Task PlayTrack(Track track)
    {
        await _audioPlayer.PlayAsync(track);
    }

    [RelayCommand]
    private async Task TogglePlayPause()
    {
        if (_audioPlayer.IsPlaying)
        {
            _audioPlayer.Pause();
        }
        else if (_audioPlayer.IsPaused)
        {
            _audioPlayer.Resume();
        }
        else
        {
            var firstTrack = CurrentTrack ?? Library.FirstOrDefault()?.Tracks.FirstOrDefault();
            if (firstTrack != null)
            {
                await _audioPlayer.PlayAsync(firstTrack);
            }
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _audioPlayer.Stop();
        App.StatePersister.ScheduleSave();
    }

    [RelayCommand]
    private async Task PlayNext()
    {
        if (IsShuffleEnabled)
        {
            await PlayRandomTrackAsync();
            return;
        }

        var next = LibraryService.FindNextTrack(_audioPlayer.CurrentTrack, Library);

        if (next == null && RepeatMode == RepeatMode.Album)
        {
            var album = FindAlbumForTrack(_audioPlayer.CurrentTrack);
            next = album?.Tracks.FirstOrDefault();
        }

        if (next != null)
            await _audioPlayer.PlayAsync(next);
    }

    [RelayCommand]
    private async Task PlayPrevious()
    {
        var prev = LibraryService.FindPreviousTrack(_audioPlayer.CurrentTrack, Library);

        if (prev == null && RepeatMode == RepeatMode.Album)
        {
            var album = FindAlbumForTrack(_audioPlayer.CurrentTrack);
            prev = album?.Tracks.LastOrDefault();
        }

        if (prev != null)
            await _audioPlayer.PlayAsync(prev);
    }

    [RelayCommand]
    private void CycleRepeatMode()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.Album,
            RepeatMode.Album => RepeatMode.Track,
            RepeatMode.Track => RepeatMode.Off,
            _ => RepeatMode.Off
        };
    }

    [RelayCommand]
    private async Task ToggleShuffle()
    {
        IsShuffleEnabled = !IsShuffleEnabled;

        if (IsShuffleEnabled)
            await PlayRandomTrackAsync();
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
        _audioPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        _audioPlayer.TrackEnded -= OnTrackEnded;
    }

    // --- Display (used by restore) ---

    public void SetCurrentTrackDisplay(Track track)
    {
        CurrentTrack = track;
        CurrentAlbumCoverArtPath = FindAlbumForTrack(track)?.CoverArtPath;
        OnPropertyChanged(nameof(NextTrackTitle));
        OnPropertyChanged(nameof(PreviousTrackTitle));
    }

    // --- Helpers ---

    private async Task PlayRandomTrackAsync()
    {
        var allTracks = Library.SelectMany(a => a.Tracks).ToList();
        if (allTracks.Count == 0) return;

        var candidates = allTracks.Where(t => t != _audioPlayer.CurrentTrack).ToList();
        if (candidates.Count == 0) candidates = allTracks;

        var pick = candidates[_random.Next(candidates.Count)];
        await _audioPlayer.PlayAsync(pick);
    }

    private Album? FindAlbumForTrack(Track? track)
    {
        if (track == null) return null;

        foreach (var album in Library)
        {
            if (album.Tracks.Contains(track))
                return album;
        }
        return null;
    }
}
