# Discosaur

A WinUI 3 desktop audio player. .NET 8, targeting Windows 10/11.

## Tech stack

- **WinUI 3** (Microsoft.WindowsAppSDK 1.8) with **Mica** backdrop
- **CommunityToolkit.Mvvm 8.4.0** for MVVM data binding
- **NAudio 2.2.1** for audio playback (`WaveOutEvent` + `MediaFoundationReader`)
- **TagLibSharp 2.3.0** for metadata parsing
- Target: net8.0-windows10.0.19041.0, nullable enabled, MSIX packaging

## Architecture

Service Locator via static properties on `App`. All singletons created in `App.OnLaunched()`:

```
App.AudioPlayer      → AudioPlayerService
App.LibraryService   → LibraryService
App.StatePersister   → StatePersisterService
App.ViewModel        → MainViewModel(AudioPlayer, LibraryService)
  └─ .PlaybackViewModel → PlaybackViewModel(AudioPlayer, Library)
  └─ .SelectionViewModel → SelectionViewModel(Library)
App.PlayerViewModel  → PlayerViewModel(AudioPlayer, DispatcherQueue)
App.MainWindow       → Window
App.MainWindowHandle → IntPtr (needed for native interop like FolderPicker)
```

Views access everything through these static properties (no DI container).

## Module purposes

| File | Purpose |
|------|---------|
| `Models/Track.cs` | Single audio track with metadata (title, artist, album, duration, etc.) |
| `Models/Album.cs` | Album grouping tracks; has computed `DisplayName`, `IsUncategorized` flag, and `FolderToken` for FutureAccessList persistence |
| `Models/AppState.cs` | JSON DTO classes for persistence: `AppState`, `PersistedAlbum`, `PersistedTrack`, `PersistedCurrentTrack` |
| `Services/AudioPlayerService.cs` | NAudio playback engine: async play, pause, stop, seek. Fires `PlaybackStateChanged` and `TrackEnded` events |
| `Services/LibraryService.cs` | Scans folders for audio files, parses metadata via TagLib, groups into albums, detects cover art |
| `Services/StatePersisterService.cs` | Persists playlist and current track to JSON in LocalFolder; debounced save (500ms), sync flush on close, async load/restore with FutureAccessList token resolution |
| `ViewModels/MainViewModel.cs` | Central app state: library collection, selection/playback sub-VMs, library management, delete/play-selected bridging |
| `ViewModels/PlaybackViewModel.cs` | Playback state: current track, play/pause/stop/next/prev commands, repeat/shuffle, volume (level, reduced level, color mode), auto-advance on track end |
| `ViewModels/PlayerViewModel.cs` | Progress tracking: polls AudioPlayer on 500ms timer, updates percent/time text, handles seek |
| `Views/Player.xaml/.cs` | Player controls UI: track name, progress slider with seek, play/pause + stop buttons |
| `Views/PlayList.xaml/.cs` | Library browser: folder picker button, album list, keyboard navigation (Up/Down/Home/End/Enter/Delete) |
| `Views/AlbumView.xaml/.cs` | Single album display: header + track list, tap-to-select, double-tap-to-play, selection highlight via VisualTreeHelper |
| `Views/Artwork.xaml/.cs` | Album cover art display (300x300 Image), updates when track changes |
| `Converters/BoolToPlayPauseGlyphConverter.cs` | IsPlaying bool → Segoe Fluent Icons pause/play glyph |
| `Converters/SliderPercentToTimeConverter.cs` | Slider percent → formatted time string using AudioPlayer duration |

## Event flow

1. **Folder scan**: PlayList → FutureAccessList stores token → `MainViewModel.AddFolderToLibrary(path, token)` → LibraryService → albums (with FolderToken) added to `Library` observable collection → UI auto-updates → StatePersister.ScheduleSave()
2. **Play track**: AlbumView double-tap → `MainViewModel.PlayTrackCommand` → `AudioPlayerService.PlayAsync()` → fires `PlaybackStateChanged` → MainViewModel updates `CurrentTrack`/`IsPlaying`/`CoverArtPath` → all views react via `PropertyChanged`
3. **Progress**: PlayerViewModel polls AudioPlayer on 500ms timer → updates `ProgressPercent`/time text → Player view updates slider
4. **Track ends**: NAudio `PlaybackStopped` → AudioPlayerService fires `TrackEnded` → MainViewModel calls `PlayNextTrack()` → cycle repeats
5. **Persistence**: State changes (add folder, delete track, playback change, stop) → `StatePersister.ScheduleSave()` (debounced 500ms) → JSON written to `ApplicationData.Current.LocalFolder`. On close → `Flush()` (sync). On startup → `LoadAndRestoreAsync()` resolves FutureAccessList tokens → rebuilds Library → restores current track display without playback

## Window layout (MainWindow.xaml)

Three-row vertical Grid (540x800, `ExtendsContentIntoTitleBar`):
- **Row 0 (Auto)**: Artwork
- **Row 1 (Auto)**: Player
- **Row 2 (*)**: PlayList (scrollable)

## Conventions

- **MVVM Toolkit**: `[ObservableProperty]` on fields (`_foo` → `Foo`), `[RelayCommand]` on methods (`Foo()` → `FooCommand`)
- **UI thread**: `DispatcherQueue.TryEnqueue()` for marshaling from event handlers
- **Async I/O**: `Task.Run` for MediaFoundationReader init to avoid blocking UI
- **File-scoped namespaces** everywhere (e.g., `namespace Discosaur.Models;`)
- **Collection expressions**: `[]` for empty collections
- **Icon glyphs**: Segoe Fluent Icons via Unicode escapes (e.g., `\uE768` = Play)
- **Selection state**: `ObservableCollection<Track>` in MainViewModel; views react to `CollectionChanged`; AlbumView uses `VisualTreeHelper` to walk the visual tree for highlight

## Build and run

`dotnet build Discosaur/Discosaur.csproj -p:Platform=x64`. Launch profiles in `Properties/launchSettings.json`: packaged (MSIX) or unpackaged. Platforms: x86, x64, ARM64.

## Implemented specs (1–7)

1. **Basic structure**: MVVM skeleton, services, vertical layout
2. **Metadata**: TagLib parsing, album grouping, cover art detection
3. **Playback**: async play, pause/seek, progress slider, auto-advance
4. **Metadata in UX**: display names, sorting, artwork, keyboard nav, selection, deletion
5. **Improve UI**: prev/next/repeat/shuffle buttons, collapse/expand, always-on-top pin, playing-track highlight
6. **Persist data**: JSON state file in LocalFolder with debounced writes, FutureAccessList tokens for folder access across restarts, current track restore without playback
7. **Volume controls**: single-button volume toggle (full/reduced), right-click flyout with volume + reduced-level sliders, icon glyph/color reflects state, persisted in AppSettings. Refactored playback state into PlaybackViewModel

## Features to implement

Future additions with their own specifications:

- allow dragging a folder into the app to add as an album
- allow drag and drop to reorder tracks
- add nested library scanning
- save settings, collapsed/expanded state, window position to the JSON file (under `settings`)
- render track number and track length
- calculate track length correctly
- allow to select entire album (ability to duplicate/delete)
- handle individual tracks
- Tray menu on window close
- Library scanning, sorting/filtering by genre/artist
- Dominant colour extraction from artwork for theming
- Frequency/amplitude visualization on progress bar
- Theme support
- add mini mode
- add settings