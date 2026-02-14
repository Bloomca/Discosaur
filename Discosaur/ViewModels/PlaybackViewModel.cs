using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discosaur.Models;
using Discosaur.Services;

namespace Discosaur.ViewModels;

public enum VolumeColorMode
{
    Default,
    Reduced,
    Manual
}

public partial class PlaybackViewModel : ObservableObject
{
    private readonly AudioPlayerService _audioPlayer;
    private readonly ObservableCollection<Album> _library;
    private static readonly Random _random = new();

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

    // --- Volume ---

    [ObservableProperty]
    private int _volumeLevel = 100;

    [ObservableProperty]
    private int _reducedVolumeLevel = 30;

    [ObservableProperty]
    private VolumeColorMode _volumeColorMode;

    public string? NextTrackTitle => LibraryService.FindNextTrack(CurrentTrack, _library)?.Title;
    public string? PreviousTrackTitle => LibraryService.FindPreviousTrack(CurrentTrack, _library)?.Title;

    public PlaybackViewModel(AudioPlayerService audioPlayer, ObservableCollection<Album> library)
    {
        _audioPlayer = audioPlayer;
        _library = library;

        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        _audioPlayer.TrackEnded += OnTrackEnded;
    }

    private void OnPlaybackStateChanged()
    {
        CurrentTrack = _audioPlayer.CurrentTrack;
        IsPlaying = _audioPlayer.IsPlaying;
        CurrentAlbumCoverArtPath = LibraryService.FindAlbumForTrack(CurrentTrack, _library)?.CoverArtPath;
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

        var next = LibraryService.FindNextTrack(_audioPlayer.CurrentTrack, _library);

        if (next == null && RepeatMode == RepeatMode.Album)
        {
            var album = LibraryService.FindAlbumForTrack(_audioPlayer.CurrentTrack, _library);
            next = album?.Tracks.FirstOrDefault();
        }

        if (next != null)
            await _audioPlayer.PlayAsync(next);
        else
            _audioPlayer.Stop();
    }

    // --- Playback commands ---

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
            var firstTrack = CurrentTrack ?? _library.FirstOrDefault()?.Tracks.FirstOrDefault();
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

        var next = LibraryService.FindNextTrack(_audioPlayer.CurrentTrack, _library);

        if (next == null && RepeatMode == RepeatMode.Album)
        {
            var album = LibraryService.FindAlbumForTrack(_audioPlayer.CurrentTrack, _library);
            next = album?.Tracks.FirstOrDefault();
        }

        if (next != null)
            await _audioPlayer.PlayAsync(next);
    }

    [RelayCommand]
    private async Task PlayPrevious()
    {
        var prev = LibraryService.FindPreviousTrack(_audioPlayer.CurrentTrack, _library);

        if (prev == null && RepeatMode == RepeatMode.Album)
        {
            var album = LibraryService.FindAlbumForTrack(_audioPlayer.CurrentTrack, _library);
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

    // --- Volume ---

    public void ToggleVolume()
    {
        if (VolumeLevel == 100)
        {
            VolumeLevel = ReducedVolumeLevel;
            VolumeColorMode = VolumeColorMode.Reduced;
        }
        else
        {
            VolumeLevel = 100;
            VolumeColorMode = VolumeColorMode.Default;
        }

        ApplyVolume();
    }

    public void SetVolume(int level)
    {
        VolumeLevel = Math.Clamp(level, 0, 100);
        VolumeColorMode = VolumeLevel == 100 ? VolumeColorMode.Default : VolumeColorMode.Manual;
        ApplyVolume();
    }

    public void SetReducedVolumeLevel(int level)
    {
        ReducedVolumeLevel = Math.Clamp(level, 0, 100);
        App.StatePersister.ScheduleSave();
    }

    private void ApplyVolume()
    {
        _audioPlayer.Volume = VolumeLevel / 100f;
        App.StatePersister.ScheduleSave();
    }

    partial void OnCurrentAlbumCoverArtPathChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            App.ThemeViewModel.ApplyDynamicThemeFromArtwork(value);
        else
            App.ThemeViewModel.ResetToDefault();
    }

    // --- Display (used by restore) ---

    public void SetCurrentTrackDisplay(Track track)
    {
        CurrentTrack = track;
        CurrentAlbumCoverArtPath = LibraryService.FindAlbumForTrack(track, _library)?.CoverArtPath;
        OnPropertyChanged(nameof(NextTrackTitle));
        OnPropertyChanged(nameof(PreviousTrackTitle));
    }

    // --- Lifecycle ---

    public void Shutdown()
    {
        _audioPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        _audioPlayer.TrackEnded -= OnTrackEnded;
    }

    // --- Helpers ---

    private async Task PlayRandomTrackAsync()
    {
        var allTracks = _library.SelectMany(a => a.Tracks).ToList();
        if (allTracks.Count == 0) return;

        var candidates = allTracks.Where(t => t != _audioPlayer.CurrentTrack).ToList();
        if (candidates.Count == 0) candidates = allTracks;

        var pick = candidates[_random.Next(candidates.Count)];
        await _audioPlayer.PlayAsync(pick);
    }
}
