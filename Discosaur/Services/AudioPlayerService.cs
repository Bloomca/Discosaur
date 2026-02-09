using System;
using Discosaur.Models;
using NAudio.Wave;

namespace Discosaur.Services;

public class AudioPlayerService
{
    private WaveOutEvent? _waveOut;
    private MediaFoundationReader? _reader;

    public Track? CurrentTrack { get; private set; }
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;
    public bool IsPaused => _waveOut?.PlaybackState == PlaybackState.Paused;

    public event Action? PlaybackStateChanged;

    public void Play(Track track)
    {
        Stop();

        CurrentTrack = track;
        _reader = new MediaFoundationReader(track.FilePath);
        _waveOut = new WaveOutEvent();
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

        CurrentTrack = null;
        PlaybackStateChanged?.Invoke();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackStateChanged?.Invoke();
    }
}
