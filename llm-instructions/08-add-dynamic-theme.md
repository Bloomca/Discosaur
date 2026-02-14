# Add dynamic theme

Currently the application has only one theme: default, which is a Mica background with the default Text colour. This feature adds dynamic theming: extract the dominant colour from the currently playing album's artwork and derive an entire colour palette from it automatically.

## Feature details

When the current track changes (and the album changes), we extract the dominant colour from the cover art and use it as the **background colour**. All other colours are derived algorithmically:

- **Background**: dominant colour from artwork (can be anything including white or black)
- **Primary text**: white or near-black, chosen by luminance contrast against background
- **Secondary text**: primary text at 60% opacity
- **Selection highlight**: dominant colour shifted ±15% lightness in HSL, at 30% opacity
- **Playing highlight**: same hue as selection, at 45% opacity
- **Both (selected + playing)**: same hue, at 60% opacity

The dynamic theme is always active for now (no settings UI, no theme switching — that comes later when we have multiple presets). If there is no artwork, fall back to the default Mica look.

## Dominant colour extraction

Extract the dominant colour from the album cover art image. Use pixel sampling via `Windows.Graphics.Imaging.BitmapDecoder` — resize to a small sample (e.g. 50x50), count pixel colours (quantized to reduce noise), and pick the most frequent. No external NuGet dependency needed.

This runs on a background thread (`Task.Run`) to avoid blocking the UI. The result is dispatched back to the UI thread.

## Colour derivation

All derived from one dominant colour, using ~30-40 lines of helper methods:

1. **Relative luminance**: `L = 0.2126*R + 0.7152*G + 0.0722*B` (normalized 0-1)
2. **Text colour**: if `L > 0.179` → dark text (`#1A1A1A`), else → light text (`#F0F0F0`)
3. **Secondary text**: primary text colour with `Opacity = 0.6`
4. **Selection/playing**: convert dominant to HSL, shift lightness ±15% (lighten on dark bg, darken on light bg), apply varying opacity levels

## ThemeViewModel

Global VM exposed as `App.ThemeViewModel`. Observable properties for each colour (as `SolidColorBrush` or `Color`). All views subscribe to `PropertyChanged` and update their colours.

Properties:
- `Color BackgroundColor`
- `Color PrimaryTextColor`
- `Color SecondaryTextColor`
- `SolidColorBrush SelectionBrush`
- `SolidColorBrush PlayingBrush`
- `SolidColorBrush SelectedAndPlayingBrush`
- `bool IsDynamicThemeActive` (true when artwork-derived theme is applied)

Methods:
- `ApplyDynamicThemeFromArtwork(string coverArtPath)` — extracts dominant colour, derives palette, updates all properties
- `ResetToDefault()` — clears dynamic theme, returns to Mica defaults

## Integration points

1. **PlaybackViewModel**: when `CurrentAlbumCoverArtPath` changes, call `App.ThemeViewModel.ApplyDynamicThemeFromArtwork(path)`
2. **AlbumView.xaml.cs**: replace static brush fields with `App.ThemeViewModel` brushes; subscribe to theme changes and re-run `UpdateSelectionVisuals()`
3. **Player.xaml / Player.xaml.cs**: bind text colours to theme; volume icon colours remain independent (they indicate state, not theme)
4. **Artwork.xaml.cs**: text overlay colours if any
5. **PlayList.xaml.cs**: album header text colours
6. **MainWindow.xaml.cs**: apply background colour (either as a solid background behind Mica, or by switching backdrop)
7. **StatePersisterService**: no persistence needed yet (dynamic theme is always-on; settings come later)
8. **App.xaml.cs**: add `App.ThemeViewModel` singleton, initialize before views

## Background colour and Mica

When dynamic theme is active, set the window's root Grid background to the dominant colour. This effectively replaces Mica with a solid colour. When no artwork / default theme, set background back to Transparent (Mica shows through).

## Implementation plan

### Step 1: Colour helpers (`Helpers/ColorHelper.cs`)

Static helper class with:
- `Color GetDominantColor(string imagePath)` — async, uses BitmapDecoder to sample pixels
- `double GetLuminance(Color c)` — relative luminance formula
- `bool IsDark(Color c)` — luminance ≤ 0.179
- `Color GetTextColor(Color background)` — white or near-black
- `Color GetSecondaryTextColor(Color background)` — text colour with 60% effective opacity blended onto background
- `(float H, float S, float L) ToHsl(Color c)` and `Color FromHsl(float h, float s, float l)`
- `Color ShiftLightness(Color c, float amount)` — for selection/playing brush derivation

### Step 2: ThemeViewModel (`ViewModels/ThemeViewModel.cs`)

- `ObservableObject` with `[ObservableProperty]` fields for all theme colours/brushes
- `ApplyDynamicThemeFromArtwork(string path)`: calls helpers on background thread, dispatches results
- `ResetToDefault()`: sets `IsDynamicThemeActive = false`, clears colours to system defaults
- Needs `DispatcherQueue` reference for thread marshaling

### Step 3: Register in `App.xaml.cs`

- Add `public static ThemeViewModel ThemeViewModel { get; private set; }`
- Instantiate after MainViewModel (needs DispatcherQueue from window)

### Step 4: Wire trigger in `PlaybackViewModel`

- In `OnPlaybackStateChanged` or wherever `CurrentAlbumCoverArtPath` is set, call `App.ThemeViewModel.ApplyDynamicThemeFromArtwork(path)` if path is not null, else `ResetToDefault()`

### Step 5: Update `MainWindow.xaml.cs`

- Subscribe to `ThemeViewModel.PropertyChanged`
- When `BackgroundColor` changes, set the root Grid's `Background` to new colour
- When `IsDynamicThemeActive` is false, set background to Transparent (Mica shows through)

### Step 6: Update `AlbumView.xaml.cs`

- Remove static brush fields
- Read brushes from `App.ThemeViewModel` in `UpdateSelectionVisuals()`
- Subscribe to `ThemeViewModel.PropertyChanged` → re-run `UpdateSelectionVisuals()`
- Update album header and track text foreground colours from theme

### Step 7: Update `Player.xaml.cs`

- Update track name / album text foreground to `PrimaryTextColor` / `SecondaryTextColor`
- Subscribe to theme changes
- Volume icon colours stay independent (they indicate volume state, not theme)

### Step 8: Update `PlayList.xaml.cs` / `AlbumView.xaml`

- Album header text, track text, add-folder button — all use theme text colours

### File change summary

| File | Change |
|------|--------|
| `Helpers/ColorHelper.cs` | **New** — dominant colour extraction, luminance, HSL, contrast helpers |
| `ViewModels/ThemeViewModel.cs` | **New** — observable theme colours, dynamic theme application |
| `App.xaml.cs` | Add ThemeViewModel singleton |
| `ViewModels/PlaybackViewModel.cs` | Trigger theme update on artwork change |
| `MainWindow.xaml.cs` | Apply background colour from theme |
| `Views/AlbumView.xaml.cs` | Replace static brushes, subscribe to theme |
| `Views/AlbumView.xaml` | Bind text colours to theme (or update in code-behind) |
| `Views/Player.xaml.cs` | Update text foreground colours from theme |
| `Views/PlayList.xaml.cs` | Update add-folder button / header text colours |
| `Views/Artwork.xaml.cs` | Pin button colour from theme |
