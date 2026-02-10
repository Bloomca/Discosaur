# Add Playback

We need to add the playback of the selected track. Currently the AudioPlayerService implements the basics of the playback of the passed file, but it actually does not work: it is both not connected and the play is invoked in the UI thread.

## Features

The main idea is to keep the functionality relatively simple. A list of things needed to be added:

- pressing "play" with no selected track starts playing the first track in the library (first album -> first track)
- playing should work correctly and not block the main UI thread
- pausing should work
- there needs to a progress bar component added. For now we can skip the handle dragging, but clicking should cause "seek" action
- there should be a component for the total time/currently played time
- when the track ends, next track should start playing automatically

## Implementation details

Player functionality needs to happen in the AudioPlayerService; all the UI related changes need to be implemented in the Player view component. We probably should create a dedicate VM for it to track the current progress of the track.

### Files to modify

| File | Changes |
|------|---------|
| `Services/AudioPlayerService.cs` | Add async Play, position/duration props, Seek(), TrackEnded event |
| `ViewModels/PlayerViewModel.cs` | **New file** — progress timer, formatted time, seek command |
| `ViewModels/MainViewModel.cs` | Add PlayNextTrack, play-first-track-if-none logic, wire TrackEnded |
| `Views/Player.xaml` | Add Slider (progress bar) + time TextBlocks |
| `Views/Player.xaml.cs` | Bind to PlayerViewModel, handle slider seek |
| `Views/AlbumView.xaml` | Replace TextBlock with a clickable container for double-click |
| `Views/AlbumView.xaml.cs` | Add double-click handler that calls PlayTrackCommand |
| `App.xaml.cs` | Create and expose PlayerViewModel singleton |

### Step 1: AudioPlayerService — fix playback & add progress/seek

- Make `Play(Track)` async (`Task PlayAsync(Track)`) so `MediaFoundationReader` initialization happens off the UI thread via `Task.Run`
- Add read-only properties: `TimeSpan CurrentPosition` (from `_reader.CurrentTime`), `TimeSpan TotalDuration` (from `_reader.TotalTime`)
- Add `Seek(TimeSpan position)` — sets `_reader.CurrentTime`
- Add `event Action? TrackEnded` — fired from `OnPlaybackStopped` only when the track reached the end naturally (not from a manual Stop). Distinguish by checking if `_reader.Position >= _reader.Length`
- Keep the existing `PlaybackStateChanged` event as-is

### Step 2: New PlayerViewModel — progress tracking

- `ObservableObject` with `[ObservableProperty]` fields:
  - `double ProgressPercent` (0–100)
  - `string CurrentTimeText` (e.g. "1:23")
  - `string TotalTimeText` (e.g. "4:56")
- Uses a `DispatcherTimer` (1 second interval) to poll `AudioPlayerService.CurrentPosition` / `TotalDuration` and update the properties
- Start/stop timer when playback state changes
- `SeekToPercent(double percent)` method — computes target `TimeSpan` from percent and calls `AudioPlayerService.Seek()`

### Step 3: MainViewModel — play-first-track & auto-advance

- Modify `TogglePlayPause`: if nothing is playing and nothing is paused, play the first track in the library (`Library[0].Tracks[0]`)
- Add `PlayNextTrack()`: find the current track in the library, advance to the next one (within album, then cross to next album). If at the very end, stop
- Subscribe to `AudioPlayerService.TrackEnded` → call `PlayNextTrack()`
- Change `PlayTrack` to `async` to call `PlayAsync`

### Step 4: Player.xaml — progress bar & time display

- Add between the track name and buttons:
  - A `Grid` with a `Slider` (Min=0, Max=100) for the progress bar
  - Two `TextBlock`s flanking it: current time (left) and total time (right)
- The Slider's value-changed event triggers seek via PlayerViewModel

### Step 5: Player.xaml.cs — bind to PlayerViewModel

- Subscribe to `PlayerViewModel.PropertyChanged` to update Slider value, time texts
- Handle Slider interaction to call `PlayerViewModel.SeekToPercent()`
- Distinguish between user-dragging and programmatic updates (use a `_isSeeking` flag)

### Step 6: AlbumView — double-click to play

- Replace the track `TextBlock` with a container that supports `DoubleTapped` event
- On `DoubleTapped`, call `App.ViewModel.PlayTrackCommand.Execute(track)`