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
App.ViewModel        → MainViewModel(AudioPlayer, LibraryService)
App.PlayerViewModel  → PlayerViewModel(AudioPlayer, DispatcherQueue)
App.MainWindow       → Window
App.MainWindowHandle → IntPtr (needed for native interop like FolderPicker)
```

Views access everything through these static properties (no DI container).

## Module purposes

| File | Purpose |
|------|---------|
| `Models/Track.cs` | Single audio track with metadata (title, artist, album, duration, etc.) |
| `Models/Album.cs` | Album grouping tracks; has computed `DisplayName` and `IsUncategorized` flag |
| `Services/AudioPlayerService.cs` | NAudio playback engine: async play, pause, stop, seek. Fires `PlaybackStateChanged` and `TrackEnded` events |
| `Services/LibraryService.cs` | Scans folders for audio files, parses metadata via TagLib, groups into albums, detects cover art |
| `ViewModels/MainViewModel.cs` | Central app state: library collection, selection, playback commands, track navigation helpers, auto-advance on track end |
| `ViewModels/PlayerViewModel.cs` | Progress tracking: polls AudioPlayer on 500ms timer, updates percent/time text, handles seek |
| `Views/Player.xaml/.cs` | Player controls UI: track name, progress slider with seek, play/pause + stop buttons |
| `Views/PlayList.xaml/.cs` | Library browser: folder picker button, album list, keyboard navigation (Up/Down/Home/End/Enter/Delete) |
| `Views/AlbumView.xaml/.cs` | Single album display: header + track list, tap-to-select, double-tap-to-play, selection highlight via VisualTreeHelper |
| `Views/Artwork.xaml/.cs` | Album cover art display (300x300 Image), updates when track changes |
| `Converters/BoolToPlayPauseGlyphConverter.cs` | IsPlaying bool → Segoe Fluent Icons pause/play glyph |
| `Converters/SliderPercentToTimeConverter.cs` | Slider percent → formatted time string using AudioPlayer duration |

## Event flow

1. **Folder scan**: PlayList → `MainViewModel.AddFolderToLibrary()` → LibraryService → albums added to `Library` observable collection → UI auto-updates
2. **Play track**: AlbumView double-tap → `MainViewModel.PlayTrackCommand` → `AudioPlayerService.PlayAsync()` → fires `PlaybackStateChanged` → MainViewModel updates `CurrentTrack`/`IsPlaying`/`CoverArtPath` → all views react via `PropertyChanged`
3. **Progress**: PlayerViewModel polls AudioPlayer on 500ms timer → updates `ProgressPercent`/time text → Player view updates slider
4. **Track ends**: NAudio `PlaybackStopped` → AudioPlayerService fires `TrackEnded` → MainViewModel calls `PlayNextTrack()` → cycle repeats

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

`dotnet build Discosaur/Discosaur.csproj`. Launch profiles in `Properties/launchSettings.json`: packaged (MSIX) or unpackaged. Platforms: x86, x64, ARM64.

## Implemented specs (1–4)

1. **Basic structure**: MVVM skeleton, services, vertical layout
2. **Metadata**: TagLib parsing, album grouping, cover art detection
3. **Playback**: async play, pause/seek, progress slider, auto-advance
4. **Metadata in UX**: display names, sorting, artwork, keyboard nav, selection, deletion

## Features to implement

Future additions with their own specifications:

- Tray menu on window close
- Persist playlist to JSON
- Library scanning, sorting/filtering by genre/artist
- Dominant colour extraction from artwork for theming
- Frequency/amplitude visualization on progress bar
- Theme support
