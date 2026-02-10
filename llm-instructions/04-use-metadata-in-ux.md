# Use metadata in the UX

We need to use the collected metadata and actually display it in the user interface. Currently we rely on the filenames, but we potentially have lots of info we can utilize.

## Features

- automatically sort tracks when adding a new album by track order (if present). If some tracks don't have the track number, don't sort them -- leave them in the same order they were read from the OS
- display the artwork of the currently played album. For now, just simply put it into a rectangle
- make each track selectable (no need for albums for now). When we select it, pressing "Enter" starts playback, and pressing "Delete" removes the track (just from the playlist)
- use album name from the metadata. Use the format: "%ALBUM_NAME% by %ARTIST_NAME% (%YEAR%)". If something is missing, omit it
- add navigation with the keyboard: when pressing up/down, navigate to the next/previous song. When pressing Home/End, navigate to the next/previous album. When pressing down while having the last track of an album selected, select the first track of the next album (same with the opposite direction)
- use track name from the metadata

## Implementation details

Selection state lives in MainViewModel as an ObservableCollection<Track> SelectedTracks (array for future multi-select). A convenience SelectedTrack property keeps track of the single active selection and syncs the array. Track gets an IsSelected observable property for UI binding.

When deleting the currently playing track, auto-advance to the next track. If no next track exists, stop playback. Empty albums are removed after deletion.

For sorting: tracks with TrackNumber are sorted normally; tracks without go at the bottom preserving filesystem order.

## File changes

Files to modify:
 File: Models\Track.cs
 Changes: Inherit ObservableObject, add [ObservableProperty] bool _isSelected
 ────────────────────────────────────────
 File: Models\Album.cs
 Changes: Add computed DisplayName property
 ────────────────────────────────────────
 File: Services\LibraryService.cs
 Changes: Fix sorting in GroupIntoAlbums — split with/without TrackNumber
 ────────────────────────────────────────
 File: ViewModels\MainViewModel.cs
 Changes: Add SelectedTrack, SelectedTracks, CurrentAlbumCoverArtPath, navigation methods, delete/play-selected
   commands; refactor PlayNextTrack with shared helpers
 ────────────────────────────────────────
 File: Converters\BoolToSelectionBrushConverter.cs
 Changes: New file — converts IsSelected to highlight brush
 ────────────────────────────────────────
 File: Views\Artwork.xaml
 Changes: Replace placeholder with Image control in a collapsible Border
 ────────────────────────────────────────
 File: Views\Artwork.xaml.cs
 Changes: Subscribe to MainViewModel.PropertyChanged, update image from CurrentAlbumCoverArtPath
 ────────────────────────────────────────
 File: Views\AlbumView.xaml
 Changes: Header → DisplayName, track text → Title, bind Background to IsSelected, add Tapped handler
 ────────────────────────────────────────
 File: Views\AlbumView.xaml.cs
 Changes: Add Track_Tapped handler for single-click selection
 ────────────────────────────────────────
 File: Views\PlayList.xaml
 Changes: Add IsTabStop="True" and KeyDown to Grid
 ────────────────────────────────────────
 File: Views\PlayList.xaml.cs
 Changes: Add PlayList_KeyDown — dispatch Up/Down/Home/End/Enter/Delete
 ────────────────────────────────────────
 File: Views\Player.xaml.cs
 Changes: Change FileName → Title in UpdateTrackName

## Implementation plan

### Step 1: Album model — add DisplayName

Add a computed DisplayName property. Format: "Name by Artist (Year)". Omit " by Artist" if Artist is null/empty. Omit " (Year)" if Year is null. Return "Uncategorized" for uncategorized albums.

### Step 2: Fix track sorting in LibraryService

In GroupIntoAlbums, replace the current universal OrderBy with: separate tracks into those with and without TrackNumber. Sort the first group by TrackNumber then FileName. Keep the second group in original enumeration order. Concatenate: sorted first, unsorted at bottom.

### Step 3: MainViewModel — selection, navigation, cover art

 Properties:
 - ObservableCollection<Track> SelectedTracks — array to keep all selected tracks
 - [ObservableProperty] string? _currentAlbumCoverArtPath — set in OnPlaybackStateChanged by looking up the current
 track's album

 Helpers (private):
 - FindNextTrack(Track?) — returns next track in album, or first track of next album, or null
 - FindPreviousTrack(Track?) — returns previous track in album, or last track of previous album, or null
 - FindAlbumForTrack(Track?) — returns the album containing the track

 Public methods (called by PlayList view):
 - SelectTrack(Track) — sets SelectedTrack
 - SelectNextTrack() / SelectPreviousTrack() — delegates to FindNextTrack/FindPreviousTrack
 - SelectFirstTrackOfNextAlbum() / SelectFirstTrackOfPreviousAlbum() — jumps to adjacent album's first track

 Commands:
 - PlaySelectedTrackCommand — plays SelectedTrack
 - DeleteSelectedTracksCommand — removes track from album, removes empty album from Library, auto-advances if was
 playing, selects adjacent track

 Refactor: rewrite PlayNextTrack() to use FindNextTrack.

### Step 4: BoolToSelectionBrushConverter (new file)

 true → SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.3 }, false → Transparent.

### Step 5: Artwork view — display cover art

 Replace placeholder with Image (MaxHeight/MaxWidth 300, Stretch=Uniform) inside a Border that collapses when no art is
  available. Code-behind subscribes to MainViewModel.PropertyChanged and sets BitmapImage source from
 CurrentAlbumCoverArtPath.

### Step 6: AlbumView — selection, metadata display

 - Header: Album.Name → Album.DisplayName
 - Track text: FileName → Title
 - Track Border: add Tapped="Track_Tapped", bind Background="{x:Bind IsSelected, Converter={StaticResource
 SelectionBrushConverter}, Mode=OneWay}"
 - Code-behind: add Track_Tapped → App.ViewModel.SelectTrack(track)

### Step 7: PlayList — keyboard navigation

 - XAML: add IsTabStop="True" and KeyDown="PlayList_KeyDown" to the Grid
 - Code-behind: handle Down/Up/Home/End/Enter/Delete, dispatch to MainViewModel methods, set e.Handled = true

### Step 8: Player view — use track title

 Change UpdateTrackName from CurrentTrack?.FileName to CurrentTrack?.Title.