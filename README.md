# Ctrl+Alt+Stand

**Get up. Stand up. Sit smart.**

A lightweight Windows standing-desk timer for building better sit, stand, and movement routines. The schedule begins when you press **Start**—it never depends on the time of day.

## Features

- Defaults to 30 minutes sitting, 20 minutes standing, and 3 minutes moving.
- Adjustable phase durations from 1 to 180 minutes.
- Choose whether each work session starts with sitting or standing.
- Optional movement phase for a simple Sit → Stand routine.
- Start, pause, reset, or skip the current phase at any time.
- Large color-coded desktop cue, Windows sound, notification, and taskbar flash at transitions.
- Optional always-on-top window.
- Local settings saved under `%LOCALAPPDATA%\CtrlAltStand\settings.ini`.
- No accounts, network access, telemetry, or cloud services.

## Download

The first public Windows release will be published after hands-on testing is complete.

## Run locally

Open `dist\CtrlAltStand.exe`, adjust the routine if desired, and press **Start**.

## Build

Ctrl+Alt+Stand uses the C# compiler included with Windows .NET Framework. No package manager or third-party dependencies are required.

```powershell
.\build.ps1
```

The executable is written to `dist\CtrlAltStand.exe`.

Run the built-in checks with:

```powershell
.\dist\CtrlAltStand.exe --self-test
.\dist\CtrlAltStand.exe --smoke-test
```

## Privacy

Ctrl+Alt+Stand runs entirely on the local computer. It stores only the selected timer preferences in the current Windows user's local application-data folder.

## Health note

Ctrl+Alt+Stand is a reminder utility, not medical advice. Static standing is not a substitute for movement. Change position when uncomfortable and consult a clinician about symptoms or conditions that affect circulation, balance, joints, or the cardiovascular system.

## License

No open-source license has been selected yet. A license will be chosen before the first public release.
