# Add filtering

This application is not intended to be used with multiple playlists. Instead, it is intended to create them on the fly using filtering.

## Feature details

When we add an album, we try to extract metadata. While the application can work without it, it heavily benefits from it due to artwork (and default dynamic theme calculation based on the dominant colour), duration, artist, album name, etc. So overall it assumes you have it. We'll work on improving "uncategorized" section later, but we won't be able to help much with filtering here.

So we need to create a relationship between albums (since tracks belong to albums) and the artist name + genre + years. After that, there will be a filter button (next to "Add folder"), which will open a flyout menu and allow to select options.

## Filter button

We will add a new button called "filters" next to "add folder" (no icon there). It will use default text colour; however, if a filter is currently applied, it will change the colour. Since we use dynamic theme, I think it will need to be calculated dynamically. Maybe we can simply invert? Either way, it needs to be clear that the button is "active".

## Filters

Filtering menu will be located in a flyout menu which is triggered by pressing the "filters" button. The menu will be pretty extensive with multiple rows:

- search album names (will need to be shorter somehow) + text field
- search song names (same) + text field
- years: field for "FROM" and field for "TO" (either one can be empty)
- specific albums (dropdown with all parsed albums, including uncategorized)
- genres (if any are parsed)

At the bottom, 2 or 3 buttons:

1. Reset (if any filters are applied)
2. Cancel (close the menu)
3. Apply (disabled if nothing is selected)

Filters will need to be saved, so they survive the restarts. Let's add a nullable property to "Settings": "filtering". If null, it is not applied, otherwise it is. We will not store the filtered library, we'll create the filtered version on the app start.

## Implementation details

Since we want to be able to reset filtering, we can't touch "Library" property in the MainViewModel, we'll need to introduce 3 new properties:

1. isFilteringApplied
2. FilteredLibrary
3. FilterConfiguration

We'll need to create a new model for the filters configuration. SelectionViewModel relies on the passed Library, ideally we need to change it to a callable function "getActiveLibrary", which will return either full or filtered library (SelectionVM does not care about it much).


