# Discosaur

This is a audio player application written in WinUI. It uses NAudio for working with audio, some library to get metadata, and MVVM Toolkit for 2-way binding.

## Structure

Top-level folders:

- **Views**: contains view components. Most of the logic should be in the view model, and typically a VM would call a service, but some logic is okay, and stuff like animations is preferable to keep in the component's class. Views should be single-purpose.
- **ViewModels**: containts view models. There should be one shared VM with the app state (what track is playing, playlist, etc); ideally VMs are not specific to a view.
- **Services**: helper logic, e.g. the AudioPlayer itself (not the UI part, but actually calling the Windows API to pass data to the sound card)