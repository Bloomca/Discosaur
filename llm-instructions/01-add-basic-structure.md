# Add Basic Structure Spec

Currently, this project is almost stock, the only thing added is an example of playback via NAudio to validate that it works. Let's make a basic structure.

## Instructions

- add a shared VM (use MVVM Toolkit library to make properties subscribable)
- move the audio player to a separate service (for now no reason to move the playback to a separate thread)
- add a data reader service; this service will read selected folders (for now only folders) and save it to the library observable collection. Create a data model for a track and an album -- a library contains albums, and each album contains tracks (don't parse metadata for now, you can hardcode the same album for all tracks)
- playlist view should render the library from the shared VM as a List (or maybe a List of Lists). Create a separate component for an album

## App Layout

For now, let's use the following layout -- the app will be vertical by default, and it will be a vertical stack of components:

- top level: will be a spinning image of the artwork (like a CD). You can create a placeholder view for it, but not critical right now (we'll tackle that during metadata) -- will be foldable
- middle level: player controls and currently played track info. "Player" view will be that
- bottom level: actual playlist (will be foldable)

## Album component

Top line will be the album name. For now just the folder name is enough (we'll fix it during the metadata update). After that, a list of all tracks, just their names in the order of reading from the file system.