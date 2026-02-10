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

    [ObservableProperty]
    private Track? _currentTrack;

    [ObservableProperty]
    private bool _isPlaying;

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
            // Nothing playing or paused — start first track in library
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
        var current = _audioPlayer.CurrentTrack;
        if (current == null || Library.Count == 0)
        {
            return;
        }

        // Find the current track's position in the library
        for (int albumIdx = 0; albumIdx < Library.Count; albumIdx++)
        {
            var album = Library[albumIdx];
            var trackIdx = album.Tracks.IndexOf(current);
            if (trackIdx < 0) continue;

            // Next track in the same album
            if (trackIdx + 1 < album.Tracks.Count)
            {
                await _audioPlayer.PlayAsync(album.Tracks[trackIdx + 1]);
                return;
            }

            // First track in the next album
            if (albumIdx + 1 < Library.Count)
            {
                var nextAlbum = Library[albumIdx + 1];
                if (nextAlbum.Tracks.Count > 0)
                {
                    await _audioPlayer.PlayAsync(nextAlbum.Tracks[0]);
                    return;
                }
            }

            // End of library — stop
            _audioPlayer.Stop();
            return;
        }
    }
}
