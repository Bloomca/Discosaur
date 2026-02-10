# Improve user interface

The application has core functionality now (although barebones, but you can add music and play it), but it is not super tidy and a lot of helpful features are not there. The main goal is to add extra buttons (previous track/next track) and to optimize available space.

## New features

- add several new buttons to the Views/Player controls
    - previous track and next track. They will work the same way as selecting previous/next tracks works. Hovering over them will show the track's title which will be played if the button is pressed
    - repeating; pressing it once will repeat the album, pressing it again will repeat the song. Pressing it the 3rd time removes repeating
    - collapse/expand icon. When pressed to collapse, it will set the height so the library is not visible, and when expanding, it will increase the height by ~400px (not sure how to implement this part technically). By default, it starts in the expanded state. The artwork is not affected (should stay visible)
    - play random song. When we press it, it immediately starts to play a different song, but the icon becomes activated, so the next song will be random as well. Pressing it again simply deactivates the random mode, but the current song keeps playing. Random picks a song from an entire library.
- currently played track should have a special background (match secondary WinUI accent colour) -- it has to be recognizeable as being played. When it both being played and selected, the background should be a blend of them
- add a button to keep the application on top. Pressing it again deactivates the mode.

---

## Implementation plan

### 1. Add `RepeatMode` enum

Create `Models/RepeatMode.cs`:

```
namespace Discosaur.Models;

public enum RepeatMode { Off, Album, Track }
```

### 2. Add new state to `MainViewModel`

New observable properties:

- `RepeatMode _repeatMode` (default `Off`)
- `bool _isShuffleEnabled` (default `false`)
- `bool _isLibraryExpanded` (default `true`)
- `bool _isAlwaysOnTop` (default `false`)

New helper properties (not observable, computed on demand for tooltips):

- `string? NextTrackTitle` → calls `FindNextTrack(CurrentTrack)?.Title`
- `string? PreviousTrackTitle` → calls `FindPreviousTrack(CurrentTrack)?.Title`

New relay commands:

- `PlayNextTrackCommand` — calls the existing `PlayNextTrack()` logic (make it a command)
- `PlayPreviousTrackCommand` — mirrors it using `FindPreviousTrack`
- `CycleRepeatModeCommand` — cycles Off → Album → Track → Off
- `ToggleShuffleCommand` — toggles `IsShuffleEnabled`; on activation, immediately plays a random track from the entire library
- `ToggleLibraryExpandedCommand` — toggles `IsLibraryExpanded`
- `ToggleAlwaysOnTopCommand` — toggles `IsAlwaysOnTop`

### 3. Modify `OnTrackEnded` in `MainViewModel`

Replace the current unconditional `PlayNextTrack()` with repeat/shuffle-aware logic:

```
private async void OnTrackEnded()
{
    if (RepeatMode == RepeatMode.Track)
    {
        // Replay the same track
        if (_audioPlayer.CurrentTrack != null)
            await _audioPlayer.PlayAsync(_audioPlayer.CurrentTrack);
        return;
    }

    if (IsShuffleEnabled)
    {
        PlayRandomTrack();
        return;
    }

    var next = FindNextTrack(_audioPlayer.CurrentTrack);

    if (next == null && RepeatMode == RepeatMode.Album)
    {
        // Wrap to first track of the current album
        var album = FindAlbumForTrack(_audioPlayer.CurrentTrack);
        next = album?.Tracks.FirstOrDefault();
    }

    if (next != null)
        await _audioPlayer.PlayAsync(next);
    else
        _audioPlayer.Stop();
}
```

Add `PlayRandomTrack()` helper:

```
private static readonly Random _random = new();

private async void PlayRandomTrack()
{
    var allTracks = Library.SelectMany(a => a.Tracks).ToList();
    if (allTracks.Count == 0) return;

    // Avoid replaying the same track if possible
    var candidates = allTracks.Where(t => t != _audioPlayer.CurrentTrack).ToList();
    if (candidates.Count == 0) candidates = allTracks;

    var pick = candidates[_random.Next(candidates.Count)];
    await _audioPlayer.PlayAsync(pick);
}
```

### 4. Modify repeat-album logic for `FindNextTrack`

The `RepeatMode.Album` case in `OnTrackEnded` handles wrapping. But we also need `FindNextTrack` to be aware of repeat-album when used by `PlayNextTrackCommand` — if the current track is the last in its album and repeat-album is on, wrap to the album's first track. Extend `FindNextTrack` to accept an optional repeat-mode-aware flag, or handle it at the call site.

Decision: handle at the `OnTrackEnded` call site (shown above) and in `PlayNextTrackCommand`. Keep `FindNextTrack` pure (no repeat awareness) so tooltip logic stays simple.

### 5. Update `Player.xaml` button layout

Replace the current two-button StackPanel with:

```
[Previous] [Play/Pause] [Next] [Stop]  |  [Repeat] [Shuffle]  |  [Collapse]
```

All buttons 40x40 (slightly smaller than current 48x48 to fit). Use Segoe Fluent Icons:

| Button | Glyph | Notes |
|--------|-------|-------|
| Previous | `\uE892` (SkipBack) | ToolTip bound to previous track title |
| Play/Pause | existing converter | |
| Next | `\uE893` (SkipForward) | ToolTip bound to next track title |
| Stop | `\uE71A` (existing) | |
| Repeat Off | `\uE8EE` (RepeatAll) | Default opacity 0.5 |
| Repeat Album | `\uE8EE` (RepeatAll) | Full opacity |
| Repeat Track | `\uE8ED` (RepeatOne) | Full opacity |
| Shuffle | `\uE8B1` (Shuffle) | Opacity 0.5 when off, 1.0 when on |
| Collapse | `\uE70D` (ChevronUp) | Toggles to `\uE70E` (ChevronDown) when collapsed |

Implementation: use two new converters (`RepeatModeToGlyphConverter`, `RepeatModeToOpacityConverter`) or handle in code-behind. Code-behind is simpler — listen to `RepeatMode` PropertyChanged and update glyph/opacity directly, same as existing pattern for track name.

### 6. Update `Player.xaml.cs`

Add click handlers for each new button that call the corresponding MainViewModel commands. Update tooltip text by listening to `CurrentTrack` PropertyChanged (reuse existing handler) and setting `ToolTipService.SetToolTip` on the prev/next buttons.

### 7. Currently-playing track highlight in `AlbumView`

Modify `AlbumView.UpdateSelectionVisuals` to apply three possible backgrounds:

- **Selected only**: existing `CornflowerBlue` at 0.3 opacity
- **Playing only**: `SystemAccentColorLight2` resource at 0.3 opacity
- **Both selected and playing**: blend — use `SystemAccentColorLight2` at 0.5 opacity (slightly more prominent)
- **Neither**: Transparent

To detect the playing track, also subscribe to `MainViewModel.PropertyChanged` for `CurrentTrack` changes, and call `UpdateSelectionVisuals()`. In the update loop, check both `selectedTracks.Contains(track)` and `track == App.ViewModel.CurrentTrack`.

Add brushes:

```csharp
private static readonly Brush PlayingBrush; // resolved from theme resource at runtime
private static readonly Brush SelectedAndPlayingBrush;
```

Resolve `SystemAccentColorLight2` via `Application.Current.Resources` in the constructor (after `InitializeComponent`), falling back to a hardcoded accent if not available.

### 8. Collapse/expand (window resize)

In `MainWindow.xaml.cs`:

- Subscribe to `MainViewModel.PropertyChanged` for `IsLibraryExpanded`
- When collapsing: store current window height, then set `PlayListRow.Height = new GridLength(0)` and `AppWindow.Resize(width, currentHeight - playlistHeight)`
- When expanding: set `PlayListRow.Height = new GridLength(1, GridUnitStar)` and `AppWindow.Resize(width, storedHeight)`

Give `x:Name="PlayListRow"` to Row 2's RowDefinition in `MainWindow.xaml`. Default expanded height stays 800. Collapsed height = 800 - ~400 = ~400 (artwork 300 + player ~100).

### 9. Always-on-top (pin button on artwork)

**Artwork.xaml**: wrap the existing `Border` in a `Grid` and overlay a small pin `Button`:

```xml
<Grid PointerEntered="Artwork_PointerEntered" PointerExited="Artwork_PointerExited">
    <Border ...>  <!-- existing artwork -->
        <Image x:Name="CoverArtImage" ... />
    </Border>

    <Button x:Name="PinButton"
            Click="Pin_Click"
            Opacity="0"
            HorizontalAlignment="Left"
            VerticalAlignment="Top"
            Margin="8,8,0,0"
            Width="32" Height="32"
            Background="{ThemeResource SystemControlBackgroundChromeMediumLowBrush}">
        <FontIcon x:Name="PinIcon" Glyph="&#xE718;" FontSize="14" />
    </Button>
</Grid>
```

**Artwork.xaml.cs**:

- `Artwork_PointerEntered`: animate `PinButton.Opacity` to 1 (or set directly)
- `Artwork_PointerExited`: if not pinned, animate opacity to 0
- `Pin_Click`: call `App.ViewModel.ToggleAlwaysOnTopCommand`
- Listen to `IsAlwaysOnTop` changes: when active, keep pin visible always and use filled pin glyph (`\uE841`); when inactive, use outline pin (`\uE718`) and respect hover behavior

**MainWindow.xaml.cs** (or MainViewModel): implement the actual always-on-top via `OverlappedPresenter`:

```csharp
var presenter = AppWindow.Presenter as OverlappedPresenter;
if (presenter != null)
    presenter.IsAlwaysOnTop = App.ViewModel.IsAlwaysOnTop;
```

Since `ExtendsContentIntoTitleBar` is true, the pin button at the top-left of the artwork area sits in the content region. No drag-region conflicts because the artwork area is below where the system title bar would be — the whole content is custom. If there is a drag region issue, exclude the button rect via `InputNonClientPointerSource.SetRegionRects`.

### File change summary

| File | Change |
|------|--------|
| `Models/RepeatMode.cs` | **New** — enum with Off, Album, Track |
| `ViewModels/MainViewModel.cs` | Add properties, commands, modify `OnTrackEnded`, add `PlayRandomTrack` |
| `Views/Player.xaml` | Redesign button bar with 7 buttons |
| `Views/Player.xaml.cs` | Add click handlers, tooltip updates, glyph/opacity updates for repeat/shuffle |
| `Views/AlbumView.xaml.cs` | Add playing-track brush logic, subscribe to `CurrentTrack` changes |
| `Views/Artwork.xaml` | Wrap in Grid, add overlay pin button |
| `Views/Artwork.xaml.cs` | Add hover show/hide, pin click, always-on-top glyph toggle |
| `MainWindow.xaml` | Name the PlayList row definition |
| `MainWindow.xaml.cs` | Add collapse/expand resize logic, always-on-top presenter logic |