# Day Scheduler (Planer)

A simple tray-based Windows scheduler that cycles between work and break periods.

Features
- Configurable work and break durations (minutes) via `config.json`
- System notifications (Windows Toasts) at transitions
- Customizable sounds for events (wav/mp3)
- Tray icon with context menu
- Localized UI strings (English and Polish)

Getting started
1. Build the project using .NET 8 (Visual Studio or `dotnet build`).
2. Run the application; it will create a default `config.json` and `sounds/` folder in the application directory.
3. Use the tray icon to open the schedule window or edit the configuration file.

Configuration (`config.json`)
- `WorkMinutes` (int): length of work period in minutes (default 90)
- `BreakMinutes` (int): length of break period in minutes (default 30)
- `Language` (string): `en` or `pl` - controls UI language and labels
- `DevMode` (bool): shows developer buttons in UI for testing sounds
- `Sounds` (object): relative paths to sound files for events
- `Notifications` (object): titles and messages used for toast notifications

Example:

```json
{
  "WorkMinutes": 90,
  "BreakMinutes": 30,
  "Language": "en",
  "DevMode": false,
  "Sounds": {
    "EndOfWork": "sounds/end_of_work.wav",
    "WorkEndingSoon": "sounds/work_ending_soon.wav",
    "StartBreak": "sounds/start_break.wav",
    "BreakEndingSoon": "sounds/break_ending_soon.wav",
    "StartWork": "sounds/start_work.wav"
  },
  "Notifications": {
    "StartBreakTitle": "Now break",
    "StartBreakMessage": "Enjoy your break",
    "StartWorkTitle": "Now work",
    "StartWorkMessage": "Back to work"
  }
}
```

Notes
- For Windows toast notifications to work reliably for desktop apps, your application should have an AppUserModelID and a Start Menu shortcut registered. The project currently attempts to show toast notifications using the Windows Community Toolkit. If toasts don't appear, the code falls back to tray balloon tips.

Extending localization
- The app includes a small `Localizer` helper. For larger projects replace it with .resx resources or a localization framework.

License
- This project code is provided as-is for demonstration and personal use.
