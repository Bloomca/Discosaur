using System.Collections.ObjectModel;
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

    [ObservableProperty]
    private Track? _currentTrack;

    [ObservableProperty]
    private bool _isPlaying;

    public MainViewModel(AudioPlayerService audioPlayer, LibraryService libraryService)
    {
        _audioPlayer = audioPlayer;
        _libraryService = libraryService;

        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    private void OnPlaybackStateChanged()
    {
        CurrentTrack = _audioPlayer.CurrentTrack;
        IsPlaying = _audioPlayer.IsPlaying;
    }

    public void AddFolderToLibrary(string folderPath)
    {
        var album = _libraryService.ScanFolder(folderPath);
        if (album.Tracks.Count > 0)
        {
            Library.Add(album);
        }
    }

    [RelayCommand]
    private void PlayTrack(Track track)
    {
        _audioPlayer.Play(track);
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (_audioPlayer.IsPlaying)
        {
            _audioPlayer.Pause();
        }
        else if (_audioPlayer.IsPaused)
        {
            _audioPlayer.Resume();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _audioPlayer.Stop();
    }
}
