using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Discosaur.Services;
using Microsoft.UI.Dispatching;

namespace Discosaur.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly AudioPlayerService _audioPlayer;
    private readonly DispatcherQueueTimer _timer;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _currentTimeText = "0:00";

    [ObservableProperty]
    private string _totalTimeText = "0:00";

    public PlayerViewModel(AudioPlayerService audioPlayer, DispatcherQueue dispatcherQueue)
    {
        _audioPlayer = audioPlayer;

        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += (_, _) => UpdateProgress();

        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    private void OnPlaybackStateChanged()
    {
        if (_audioPlayer.IsPlaying)
        {
            _timer.Start();
            UpdateProgress();
        }
        else
        {
            _timer.Stop();

            if (_audioPlayer.CurrentTrack == null)
            {
                ProgressPercent = 0;
                CurrentTimeText = "0:00";
                TotalTimeText = "0:00";
            }
        }
    }

    private void UpdateProgress()
    {
        var total = _audioPlayer.TotalDuration;
        var current = _audioPlayer.CurrentPosition;

        if (total.TotalSeconds > 0)
        {
            ProgressPercent = current.TotalSeconds / total.TotalSeconds * 100;
        }
        else
        {
            ProgressPercent = 0;
        }

        CurrentTimeText = FormatTime(current);
        TotalTimeText = FormatTime(total);
    }

    public void SeekToPercent(double percent)
    {
        var total = _audioPlayer.TotalDuration;
        var target = TimeSpan.FromSeconds(total.TotalSeconds * percent / 100);
        _audioPlayer.Seek(target);
        UpdateProgress();
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return time.ToString(@"h\:mm\:ss");
        }

        return time.ToString(@"m\:ss");
    }
}
