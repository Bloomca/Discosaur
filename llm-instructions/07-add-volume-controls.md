# Add volume controls

Currently, there is no way to control the volume, which is a core feature for a music player. Since the core identity of this application is to have a more minimal-style application, we'll the volume bar.

> Note: this spec uses % for volume, maybe it makes more sense to move to DBs

## Feature details

As I mentioned, for absolute simplicity, only one button will be visible. But default, the player starts at 100% volume, and when the button is pressed, we will lower volume to ~30% (we'll play with the number, the idea is to clearly put music in the background, not to fine-tune). When that mode is activated, we'll need to show that it is active by probably changing the icon and probably rendering with ~blue colour.

Finally, to allow fine-tuning, we'll open a flyout menu on the right click on that button.

## Icon

We'll use Fluent UI icons (native Windows icons), specifically the volume control. There are 2 aspects here, the icon itself and the colour.

First, the icon:

- when the volume is 90-100%, we show Volume3 (code glyph `\uE995`)
- when the volume is 60-89%, we show Volume2 (code glyph `\uE994`)
- when the volume is 20-59%, we show Volume1 (code glyph `\uE993`)
- when the volume is 1-19%, we show Volume0 (code glyph `\uE992`)
- when the volume is 0%, we show Mute (code glyph `\uE74F`)

Second, the colour:

- by default there is no colour styling (so it is the same colour as other icons)
- after a press, it changes the colour to some sort of light blue
- finally, if the user manually changed the volume level, it is displayed in orange

Finally, the tooltip will always display:

```
Left-click: change volume to %reducedVolumeLevel%
Right-click: open volume menu
```

The icon will be located on the same line as Track name/Band + Album title, all the way to the right.

## Clicking the icon

When the volume is full (default):

- left click applies the current value for "reducedVolumeLevel"
- right click opens the Volume control menu (see more in a separate section)

When the volume is not full regardless of the value

- left click changes it to the full value, regardless of what is the current level
- right click opens the Volume control menu (see more in a separate section)

## Right click menu

Right click on the icon always opens the volume menu in a flyout menu. It will have 2 sliders:

1. Slider for general volume (0-100, or -Infinity to 0 DBs)
2. Slider for the reducedVolumeLevel

The first slider applies the volume change immediately, while the second one is a purely configurational choice.

## Persistence

Both the current value and the "reducedVolumeLevel" slider value need to be persisted in the AppState in "Settings". No need to create a nested object, just "reducedVolumeLevel" and "volumeLevel" are enough. I think storing them as Int between 0 and 100 is the best approach.

## Implementation notes

To implement it cleanly, we need to work a bit on the MainViewModel. I already did a small refactor for the SelectionVM, and we need to do a similar thing for the Playback, and Volume Values will be stored and changed on that level.