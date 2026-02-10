# Persist data

Currently, the application does not persist any data, which is extremely inconvenient as the users expect the playlist to persist. Not only that, but we'll also add some settings which needs to survive restarts.

## Feature details

The implementation itself is relatively simple -- we are going to maintain a JSON file with a few properties. For now, we will have 3 properties: `settings` (won't be used right now, just an empty object), `playlist` and `currentTrack`. While it does sound straightforward, it does have a few tricky parts.

1. Access: the application is going to be packaged, so having just a folder/file path is not enough. When the user adds it from a file picker, the OS permits our process to access it, but not on the subsequent restarts. Windows has an API to store that.
2. Users can delete tracks from the playlist and reorder them, so we can't just store a folder path and rehydrate from there.

## Implementation details

- when the user changes the playlist (add a new folder, delete a track, reorder in the future), we write current state to the app data file
- when the current track changes (we don't track the progress, so on restart it will be at the beginning), we write current state to the app data file
- when we start the application, we try to read the app data file. If we can, we rehydrate the state -- restore the playlist and select the track
- when we start the application and the current track is there, we automatically prefill the track name the artwork (but we don't start playback)

---

## Implementation plan

### JSON structure

```json
{
  "settings": {},
  "playlist": [
    {
      "folderToken": "...",
      "name": "Album Name",
      "artist": "Artist",
      "year": 2020,
      "coverArtFileName": "cover.jpg",
      "tracks": [
        {
          "fileName": "01-song.mp3",
          "title": "Song",
          "artist": "Artist",
          "albumTitle": "Album Name",
          "trackNumber": 1,
          "year": 2020,
          "genre": "Rock",
          "durationTicks": 1234567890
        }
      ]
    }
  ],
  "currentTrack": { "folderToken": "...", "fileName": "01-song.mp3" }
}
```

Key choices: filenames only (not full paths) per track, `FutureAccessList` token per album, Duration as Ticks (long). System.Text.Json (built into .NET 8, no new dependency). JSON stored in `ApplicationData.Current.LocalFolder`.

### 1. NEW: `Models/AppState.cs` — DTO classes

Classes: `AppState`, `AppSettings` (empty placeholder), `PersistedAlbum`, `PersistedTrack`, `PersistedCurrentTrack`. Use `[JsonPropertyName]` attributes for camelCase JSON.

### 2. MODIFY: `Models/Album.cs` — Add FolderToken

Add `public string? FolderToken { get; set; }` alongside existing properties.

### 3. NEW: `Services/StatePersisterService.cs` — Core persistence service

**Save (debounced):**
- `ScheduleSave()`: captures state from `App.ViewModel` immediately (UI thread), then debounces 500ms before writing JSON via `File.WriteAllText` to `ApplicationData.Current.LocalFolder.Path`
- `Flush()`: synchronous immediate save — cancels pending debounce, captures and writes now

**Load/Restore:**
- `LoadAndRestoreAsync()`: reads JSON from file, resolves FutureAccessList tokens to folder paths (batch-dedup tokens), reconstructs `Album`/`Track` objects with `FilePath = Path.Combine(resolvedFolderPath, fileName)`, adds to `App.ViewModel.Library`, calls `SetCurrentTrackDisplay` if saved track exists
- Skip albums whose tokens fail to resolve (folder removed/inaccessible)

**CaptureState helper:** iterates `App.ViewModel.Library`, maps to DTOs. Finds current track's album by iterating Library (private helper, avoids making MainViewModel.FindAlbumForTrack public).

### 4. MODIFY: `App.xaml.cs`

- Add `public static StatePersisterService StatePersister { get; private set; } = null!;`
- Make `OnLaunched` `async void` (safe — it's an event handler override)
- Create `StatePersisterService` and `await LoadAndRestoreAsync()` before window creation so UI never flickers empty

### 5. MODIFY: `ViewModels/MainViewModel.cs`

- **`AddFolderToLibrary(string folderPath, string? folderToken = null)`** — set `album.FolderToken = folderToken` for each non-uncategorized album, call `App.StatePersister.ScheduleSave()` at end
- **`DeleteSelectedTracks`** — after removing an album, check if any other album uses same FolderToken; if not, `FutureAccessList.Remove(token)`. Add `App.StatePersister.ScheduleSave()` at end
- **`OnPlaybackStateChanged`** — add `App.StatePersister.ScheduleSave()` at end
- **`Stop()`** — add `App.StatePersister.ScheduleSave()` at end
- **New method `SetCurrentTrackDisplay(Track)`** — sets `CurrentTrack`, `CurrentAlbumCoverArtPath`, raises `NextTrackTitle`/`PreviousTrackTitle` PropertyChanged. Used by restore to show track info without starting playback.

### 6. MODIFY: `Views/PlayList.xaml.cs`

In `AddFolder_Click`, after `PickSingleFolderAsync`:
```csharp
var token = StorageApplicationPermissions.FutureAccessList.Add(folder);
App.ViewModel.AddFolderToLibrary(folder.Path, token);
```
Add `using Windows.Storage.AccessCache;`

### 7. MODIFY: `MainWindow.xaml.cs`

In constructor, add: `Closed += MainWindow_Closed;`

Handler: `App.StatePersister.Flush();` (synchronous, fast for typical library sizes)

### File change summary

| File | Change |
|------|--------|
| `Models/AppState.cs` | **New** — JSON DTO classes |
| `Models/Album.cs` | Add `FolderToken` property |
| `Services/StatePersisterService.cs` | **New** — debounced save, load/restore, FutureAccessList token resolution |
| `App.xaml.cs` | Add StatePersister property, make OnLaunched async, load state before window |
| `ViewModels/MainViewModel.cs` | AddFolderToLibrary signature change, SetCurrentTrackDisplay, ScheduleSave calls, token cleanup on delete |
| `Views/PlayList.xaml.cs` | Add FutureAccessList.Add() call, pass token to AddFolderToLibrary |
| `MainWindow.xaml.cs` | Add Closed handler with Flush |