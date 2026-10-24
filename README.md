# SB

SB is a small soundboard for Windows. It plays sound effects from keyboard shortcuts, the mouse wheel and
mouse buttons, even while another application (for example a game) is focused.

It is a modified version of [Deathcounter and Soundboard](https://github.com/Kalejin/DCSB) by Kalejin
(GPLv3). The death counter has been removed, and the app was extended and renamed.

## Features
- **Add sounds by drag and drop** - drop audio files (several at once) or folders on the window; one sound per file.
- **Select a sound with the mouse wheel** - either always, or only while a modifier key (default: Alt) is held.
  The selected sound's name is shown briefly on screen.
- **Play the selected sound** with one key or mouse button (default: middle mouse button).
- Mouse middle/side buttons can be bound like any key, also to individual sounds.
- Multi-column view (button next to "Sounds"): the same rows as the list, but also continuing to the right, so hundreds of sounds need less scrolling.
- Keys for the next / previous preset (Settings > Keyboard / Mouse), besides a key for each preset.
- Select several sounds (Ctrl/Shift+click, Ctrl+A) and press Delete to remove them together.
- Per sound: name, one or more files (a random one is played), key or key combination, volume, loop.
- Presets to switch between lists of sounds, even with a keyboard shortcut.
- Pause / continue / stop shortcuts, overlap sounds, one or two output devices, minimize to tray.
- English and Japanese UI (Settings > Other > Language).

Supported audio formats: wma, mp3, wav, ogg, m4a, aiff, flac.

## Building
```
dotnet build SB/SB.csproj -c Release
```
The result is `SB/bin/Release/SB.exe`.

## Notes
- Settings are stored in `C:\ProgramData\SB\config.xml`. On first start, settings of the original DCSB
  (`C:\ProgramData\DCSB`) are copied over.
- If key bindings stop working while another application is focused, run SB as administrator.

## License
SB is licensed under the **GNU General Public License v3.0** (see [LICENSE](LICENSE)), the same license as the
original project.

- Original work: [Deathcounter and Soundboard](https://github.com/Kalejin/DCSB), copyright (C) Kalejin (Jan Hricko), GPLv3.
- This repository is a modified version. Notable changes from the original (2026):
  death counter removed, mouse-wheel sound selection, mouse buttons as shortcuts, drag and drop to add sounds,
  English/Japanese UI, Raw Input for mouse, new name and icon, build converted to SDK-style projects.
  The full history of changes is in the git log.
- Third-party libraries (NuGet): NAudio, NVorbis, NAudio.Vorbis, MvvmLightLibs, CommonServiceLocator,
  Expression.Blend.Sdk / System.Windows.Interactivity - each under its own license.

