using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discosaur.Models;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Discosaur.Services;

public class StatePersisterService
{
    private const string StateFileName = "appstate.json";
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private CancellationTokenSource? _debounceCts;
    private bool _isReady;
    private bool _isShutDown;

    public StatePersisterService()
    {
        _filePath = Path.Combine(
            ApplicationData.Current.LocalFolder.Path,
            StateFileName);
    }

    public void SetReady() => _isReady = true;

    // --- Save (debounced) ---

    public void ScheduleSave()
    {
        if (!_isReady || _isShutDown) return;

        var state = CaptureState();

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var ct = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, ct);
                var json = JsonSerializer.Serialize(state, JsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (OperationCanceledException) { }
        }, ct);
    }

    public void Flush()
    {
        _isShutDown = true;
        _debounceCts?.Cancel();
        _debounceCts = null;

        if (!_isReady) return;

        var state = CaptureState();
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    // --- Load / Restore ---

    public async Task LoadAndRestoreAsync()
    {
        if (!File.Exists(_filePath))
            return;

        AppState? state;
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            state = JsonSerializer.Deserialize<AppState>(json, JsonOptions);
        }
        catch
        {
            return;
        }

        if (state == null)
            return;

        await RestoreStateAsync(state);
    }

    private async Task RestoreStateAsync(AppState state)
    {
        var vm = App.ViewModel;
        var futureAccessList = StorageApplicationPermissions.FutureAccessList;

        // Batch-resolve unique tokens to folder paths
        var tokenToPath = new Dictionary<string, string>();
        var uniqueTokens = state.Playlist
            .Select(a => a.FolderToken)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct();

        foreach (var token in uniqueTokens)
        {
            try
            {
                var folder = await futureAccessList.GetFolderAsync(token);
                tokenToPath[token] = folder.Path;
            }
            catch
            {
                // Token invalid or folder inaccessible — skip
            }
        }

        // Rebuild albums and tracks
        Track? trackToSelect = null;

        foreach (var pa in state.Playlist)
        {
            if (!tokenToPath.TryGetValue(pa.FolderToken, out var folderPath))
                continue;

            var album = new Album
            {
                Name = pa.Name,
                Artist = pa.Artist,
                Year = pa.Year,
                FolderToken = pa.FolderToken,
                CoverArtPath = !string.IsNullOrEmpty(pa.CoverArtFileName)
                    ? Path.Combine(folderPath, pa.CoverArtFileName)
                    : null
            };

            foreach (var pt in pa.Tracks)
            {
                var track = new Track
                {
                    FilePath = Path.Combine(folderPath, pt.FileName),
                    FileName = pt.FileName,
                    Title = pt.Title,
                    Artist = pt.Artist,
                    AlbumTitle = pt.AlbumTitle,
                    TrackNumber = pt.TrackNumber,
                    Year = pt.Year,
                    Genre = pt.Genre,
                    Duration = pt.DurationTicks.HasValue
                        ? TimeSpan.FromTicks(pt.DurationTicks.Value)
                        : null
                };

                album.Tracks.Add(track);

                if (state.CurrentTrack != null
                    && state.CurrentTrack.FolderToken == pa.FolderToken
                    && state.CurrentTrack.FileName == pt.FileName)
                {
                    trackToSelect = track;
                }
            }

            if (album.Tracks.Count > 0)
                vm.Library.Add(album);
        }

        if (trackToSelect != null)
            vm.PlaybackViewModel.SetCurrentTrackDisplay(trackToSelect);

        // Restore volume settings
        vm.PlaybackViewModel.SetVolume(state.Settings.VolumeLevel);
        vm.PlaybackViewModel.SetReducedVolumeLevel(state.Settings.ReducedVolumeLevel);
    }

    // --- State capture ---

    private static AppState CaptureState()
    {
        var vm = App.ViewModel;
        var state = new AppState();

        foreach (var album in vm.Library)
        {
            if (string.IsNullOrEmpty(album.FolderToken))
                continue;

            var persistedAlbum = new PersistedAlbum
            {
                FolderToken = album.FolderToken,
                Name = album.Name,
                Artist = album.Artist,
                Year = album.Year,
                CoverArtFileName = !string.IsNullOrEmpty(album.CoverArtPath)
                    ? Path.GetFileName(album.CoverArtPath)
                    : null,
                Tracks = album.Tracks.Select(t => new PersistedTrack
                {
                    FileName = t.FileName,
                    Title = t.Title,
                    Artist = t.Artist,
                    AlbumTitle = t.AlbumTitle,
                    TrackNumber = t.TrackNumber,
                    Year = t.Year,
                    Genre = t.Genre,
                    DurationTicks = t.Duration?.Ticks
                }).ToList()
            };

            state.Playlist.Add(persistedAlbum);
        }

        var currentTrack = vm.PlaybackViewModel.CurrentTrack;
        if (currentTrack != null)
        {
            var album = vm.Library.FirstOrDefault(a => a.Tracks.Contains(currentTrack));
            if (album != null && !string.IsNullOrEmpty(album.FolderToken))
            {
                state.CurrentTrack = new PersistedCurrentTrack
                {
                    FolderToken = album.FolderToken,
                    FileName = currentTrack.FileName
                };
            }
        }

        // Volume settings
        state.Settings.VolumeLevel = vm.PlaybackViewModel.VolumeLevel;
        state.Settings.ReducedVolumeLevel = vm.PlaybackViewModel.ReducedVolumeLevel;

        return state;
    }
}
