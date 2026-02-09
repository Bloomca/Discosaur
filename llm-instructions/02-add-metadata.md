# Add metadata

To make it a proper music player, we need to parse all the available metadata from the music. There are 2 possibilities: either we are successfull or we are not.

## If we can parse metadata

We need to use some library to have a single unified interface for metadata parsing. The goal is to parse most common formats like MP3 and Flac metadata. The logic to parse should be the following:

- receive a folder and filter all the non-music files
- get the metadata for each music file, add it to the Track Model directly -- year, track number, track name, album title, etc
- after parsing each track, iterate over them and create as many albums as needed (usually it should be 1, but we need to make sure they all belong there). Fill the album metadata from the tracks (name, year, band, etc)
- check the folder for typical picture metadata, like `cover.jpg/png`, `artwork`, etc, add the file path to the album metadata

Let's use TagLibSharp (TagLib) to handle it.

## If we can't parse metadata

If we cannot parse metadata, we need to add it to the "Uncategorized" album. The metadata will simply be the filename; that album should be a single instance.

## Scanning

For now, let's not scan recursively -- only analyze files (not folders) at the top level. We might revisit this in the future (add an option to "scan music library", but not now).

## Implementation steps

### 1. Add TagLibSharp NuGet package

Install `TagLibSharp` to the project.

### 2. Extend the Track model

Add metadata properties to `Track.cs`: `Title`, `Artist`, `AlbumTitle`, `TrackNumber`, `Year`, `Genre`, `Duration`. Keep existing `FilePath` and `FileName`. When metadata is unavailable, `Title` should fall back to `FileName`.

### 3. Extend the Album model

Add metadata properties to `Album.cs`: `Artist`, `Year`, `CoverArtPath`. `Name` already exists and will hold the album title.

### 4. Update LibraryService

Rework `ScanFolder` (or replace with a new method) to:

1. **Scan top-level only** — enumerate files in the folder (non-recursive), filter by supported audio extensions.
2. **Parse metadata** — for each music file, use TagLib to read metadata and populate the Track properties. Wrap in try/catch per file: if parsing fails, mark the track as uncategorized (leave AlbumTitle empty/null).
3. **Group into albums** — group the parsed tracks by `AlbumTitle`. Each unique album title becomes an `Album` with its metadata filled from the first track in the group (artist, year). Tracks with no album title go into a single "Uncategorized" album.
4. **Detect cover art** — scan the folder for common cover art filenames (`cover.jpg`, `cover.png`, `artwork.jpg`, `artwork.png`, `folder.jpg`, `folder.png`, `front.jpg`, `front.png`). If found, set `CoverArtPath` on all albums from that folder.
5. **Sort tracks** — within each album, sort tracks by `TrackNumber`, falling back to `FileName`.
6. **Return `List<Album>`** instead of a single `Album`.

### 5. Update MainViewModel

Update `AddFolderToLibrary` to handle the new return type (`List<Album>`). Add all returned albums to the `Library` collection. Ensure the "Uncategorized" album is a singleton across multiple folder scans (merge tracks into the existing one if it already exists).