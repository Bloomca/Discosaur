using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discosaur.Models;
using Discosaur.Services;

namespace Discosaur.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AudioPlayerService _audioPlayer;
    private readonly LibraryService _libraryService;

    public ObservableCollection<Album> Library { get; } = [];
    public ObservableCollection<Track> Uncategorized { get; } = [];
    public ObservableCollection<Track> SelectedTracks { get; } = [];

    [ObservableProperty]
    private Track? _currentTrack;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string? _currentAlbumCoverArtPath;

    public MainViewModel(AudioPlayerService audioPlayer, LibraryService libraryService)
    {
        _audioPlayer = audioPlayer;
        _libraryService = libraryService;

        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        _audioPlayer.TrackEnded += OnTrackEnded;
    }

    private void OnPlaybackStateChanged()
    {
        CurrentTrack = _audioPlayer.CurrentTrack;
        IsPlaying = _audioPlayer.IsPlaying;
        CurrentAlbumCoverArtPath = FindAlbumForTrack(CurrentTrack)?.CoverArtPath;
    }

    private void OnTrackEnded()
    {
        PlayNextTrack();
    }

    public void AddFolderToLibrary(string folderPath)
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
                Library.Add(album);
            }
        }
    }

    // --- Selection ---

    public void SelectTrack(Track track)
    {
        SelectedTracks.Clear();
        SelectedTracks.Add(track);
    }

    public void SelectNextTrack()
    {
        var anchor = SelectedTracks.LastOrDefault();
        if (anchor == null)
        {
            var first = Library.FirstOrDefault()?.Tracks.FirstOrDefault();
            if (first != null) SelectTrack(first);
            return;
        }

        var next = FindNextTrack(anchor);
        if (next != null) SelectTrack(next);
    }

    public void SelectPreviousTrack()
    {
        var anchor = SelectedTracks.LastOrDefault();
        if (anchor == null)
        {
            var last = Library.LastOrDefault()?.Tracks.LastOrDefault();
            if (last != null) SelectTrack(last);
            return;
        }

        var prev = FindPreviousTrack(anchor);
        if (prev != null) SelectTrack(prev);
    }

    public void SelectFirstTrackOfNextAlbum()
    {
        var anchor = SelectedTracks.LastOrDefault();
        if (anchor == null)
        {
            var first = Library.FirstOrDefault()?.Tracks.FirstOrDefault();
            if (first != null) SelectTrack(first);
            return;
        }

        for (int i = 0; i < Library.Count; i++)
        {
            if (Library[i].Tracks.Contains(anchor) && i + 1 < Library.Count)
            {
                var target = Library[i + 1].Tracks.FirstOrDefault();
                if (target != null) SelectTrack(target);
                return;
            }
        }
    }

    public void SelectFirstTrackOfPreviousAlbum()
    {
        var anchor = SelectedTracks.LastOrDefault();
        if (anchor == null) return;

        for (int i = 0; i < Library.Count; i++)
        {
            if (Library[i].Tracks.Contains(anchor) && i - 1 >= 0)
            {
                var target = Library[i - 1].Tracks.FirstOrDefault();
                if (target != null) SelectTrack(target);
                return;
            }
        }
    }

    [RelayCommand]
    private async Task PlaySelectedTrack()
    {
        var track = SelectedTracks.FirstOrDefault();
        if (track != null)
            await _audioPlayer.PlayAsync(track);
    }

    [RelayCommand]
    private async Task DeleteSelectedTracks()
    {
        if (SelectedTracks.Count == 0) return;

        var trackToDelete = SelectedTracks[0];
        bool wasPlaying = CurrentTrack == trackToDelete;

        var nextTrack = FindNextTrack(trackToDelete);
        var prevTrack = FindPreviousTrack(trackToDelete);

        // Remove from its album
        for (int i = 0; i < Library.Count; i++)
        {
            if (Library[i].Tracks.Remove(trackToDelete))
            {
                if (Library[i].Tracks.Count == 0)
                    Library.RemoveAt(i);
                break;
            }
        }

        // Select adjacent track
        var toSelect = nextTrack ?? prevTrack;
        SelectedTracks.Clear();
        if (toSelect != null)
            SelectedTracks.Add(toSelect);

        // Auto-advance if we deleted the playing track
        if (wasPlaying)
        {
            if (toSelect != null)
                await _audioPlayer.PlayAsync(toSelect);
            else
                _audioPlayer.Stop();
        }
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
            var firstTrack = Library.FirstOrDefault()?.Tracks.FirstOrDefault();
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
    }

    private async void PlayNextTrack()
    {
        var next = FindNextTrack(_audioPlayer.CurrentTrack);
        if (next != null)
            await _audioPlayer.PlayAsync(next);
        else
            _audioPlayer.Stop();
    }

    // --- Helpers ---

    private Track? FindNextTrack(Track? current)
    {
        if (current == null || Library.Count == 0) return null;

        for (int albumIdx = 0; albumIdx < Library.Count; albumIdx++)
        {
            var album = Library[albumIdx];
            var trackIdx = album.Tracks.IndexOf(current);
            if (trackIdx < 0) continue;

            if (trackIdx + 1 < album.Tracks.Count)
                return album.Tracks[trackIdx + 1];

            if (albumIdx + 1 < Library.Count && Library[albumIdx + 1].Tracks.Count > 0)
                return Library[albumIdx + 1].Tracks[0];

            return null;
        }
        return null;
    }

    private Track? FindPreviousTrack(Track? current)
    {
        if (current == null || Library.Count == 0) return null;

        for (int albumIdx = 0; albumIdx < Library.Count; albumIdx++)
        {
            var album = Library[albumIdx];
            var trackIdx = album.Tracks.IndexOf(current);
            if (trackIdx < 0) continue;

            if (trackIdx - 1 >= 0)
                return album.Tracks[trackIdx - 1];

            if (albumIdx - 1 >= 0)
            {
                var prevAlbum = Library[albumIdx - 1];
                if (prevAlbum.Tracks.Count > 0)
                    return prevAlbum.Tracks[^1];
            }

            return null;
        }
        return null;
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
