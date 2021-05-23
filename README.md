# FunChartEvents - a [BepInEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.11) plugin for Clone Hero v23.2.2

**FunChartEvents** lets you add wacky effects to your Clone Hero charts!

## Installation

In the root of your Clone Hero installation folder, install [BepInEx v5.4.11](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.11), followed by [the latest version of BiendeoCHLib](https://github.com/Biendeo/My-Clone-Hero-Tweaks/releases).

Once those are installed, extract the contents of the .zip of the [latest release of FunChartEvents](https://github.com/YoshiOG1/FunChartEvents/releases) to the Clone Hero folder just like you did with BepInEx and BiendeoCHLib.

## Charting

When creating your own chart of a song using [Moonscraper Chart Editor](https://github.com/FireFox2000000/Moonscraper-Chart-Editor/releases/latest), click the globe icon on the Tools box to switch to Global Event mode.  This mod uses Global Events in the `notes.chart` file to function, so **DO NOT convert your chart to `notes.mid`!!**

To add a Global Event, click the blue flag in the toolbox.  Click on where in the chart you want to add the event.

Before you release your chart, **make sure to put something in your chart's `song.ini` to indicate that it requires this mod!**  Usually, this means putting big text in the `loading_phrase`.  For example:

```
loading_phrase = some info about the song... <br><br><size=28>Please install FunChartEvents mod v1.0.0 or newer!</size>
```

### Global Events

- **`gemsize x`** : Changes the size of the notes on the highway to a specified multiplier.
  - `x` (float) : The size to change the gems to.  For example, `gemsize 1.0`resets the gem size to the default gem size (taking into account the gem size in the game's settings).
- **`hudtext TEXT`** : Makes a message appear above the highway, until either the end of the song or the next `hudtext`/`hudtext_off` event.
  - `TEXT` (string) : The message to display.  Supports rich text and custom variable tags (see the Tags section below).  Does not support line breaks, unfortunately.
- **`hudtext_off`** : Hides the HUD text until the next `hudtext` event.
- **`highwaywidth x`** : Changes the width of the highway, taking the time of a single song beat to do so.
  - `x` (float) : The width of the highway.  Cannot be zero.
- **`gemcolor #RRGGBB`** : Changes the color of all the notes on the highway, except open notes.
  - `#RRGGBB` (Color) : A hexadecimal color value to change the notes' colors to.  Some *note*-worthy examples: *(pun intended)*
    - `#00ff00`: green
    - `#ff0000`: red
    - `#ffff00`: yellow
    - `#008aff`: blue
    - `#ffb300`: orange
    - `#bb00ff`: purple (open note color)
    - `#00ffff`: cyan (Star Power color)
- **`gemcolor_off`** : Resets the note colors back to normal.

### Tags

Any custom event that has string arguments will support the following variable tags:

- **`{HeldButtons}`** : Colored text showing Player 1's held frets.  Displays as `GRYBO` but each letter whose corresponding fret isn't being held is rendered invisible.  Currently only supports 5-fret Guitar.
- **`{Player}`** : The name of Player 1.  Pretty self-explanatory.
- **`{Combo}`** : Player 1's current streak.
- **`{FC}`text`{/FC}`** : If the player is on an FC run *and* is not a bot, the text between these tags will be shown.  Otherwise, it will be hidden.
- **`{-FC}`text`{/-FC}`** : If the player is no longer on an FC run *and* is not a bot, the text between these tags will be shown.  Otherwise, it will be hidden.
- `{FretsToBits}` : A technical tag that shows binary bits indicating which frets are being held.
- `{SongTick}` : A technical tag that shows the current song tick position.  You should probably avoid using this except for debug purposes, unless you specifically want this in your chart.

**Note that all of these tags are case-sensitive!**

## Usage for the end-user

If a chart says that it uses FunChartEvents (typically specified in the song's `loading_phrase` in the `song.ini` file), please download the latest compatible version from the **[Releases page](https://github.com/YoshiOG1/FunChartEvents/releases)**.  Have fun!
