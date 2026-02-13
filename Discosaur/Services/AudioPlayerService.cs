using System;
using System.Threading.Tasks;
using Discosaur.Models;
using NAudio.Wave;

namespace Discosaur.Services;

public class AudioPlayerService
{
    private WaveOutEvent? _waveOut;
    private MediaFoundationReader? _reader;
    private bool _stoppingManually;
    private float _volume = 1.0f;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_waveOut != null)
                _waveOut.Volume = _volume;
        }
    }

    public Track? CurrentTrack { get; private set; }
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public bool IsPaused => _waveOut?.PlaybackState == PlaybackState.Paused;

    public TimeSpan CurrentPosition => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan TotalDuration => _reader?.TotalTime ?? TimeSpan.Zero;

    public event Action? PlaybackStateChanged;
    public event Action? TrackEnded;

    public async Task PlayAsync(Track track)
    {
        StopPlayback();

        CurrentTrack = track;

        // Initialize the reader off the UI thread since MediaFoundationReader does I/O
        var reader = await Task.Run(() => new MediaFoundationReader(track.FilePath));

        _reader = reader;
        _waveOut = new WaveOutEvent();
        _waveOut.Volume = _volume;
        _waveOut.Init(_reader);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        _waveOut.Play();

        PlaybackStateChanged?.Invoke();
    }

    public void Pause()
    {
        _waveOut?.Pause();
        PlaybackStateChanged?.Invoke();
    }

    public void Resume()
    {
        _waveOut?.Play();
        PlaybackStateChanged?.Invoke();
    }

    public void Stop()
    {
        _stoppingManually = true;
        StopPlayback();

        CurrentTrack = null;
        _stoppingManually = false;
        PlaybackStateChanged?.Invoke();
    }

    private void StopPlayback()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }

        if (_reader != null)
        {
            _reader.Dispose();
            _reader = null;
        }
    }

    public void Seek(TimeSpan position)
    {
        if (_reader != null)
        {
            _reader.CurrentTime = position;
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (!_stoppingManually)
        {
            TrackEnded?.Invoke();
        }

        PlaybackStateChanged?.Invoke();
    }
}
