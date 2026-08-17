# Changelog

All notable changes to Ctrl+Alt+Stand will be documented here.

## [Unreleased]

### Fixed

- Acknowledging a transition cue now also stops the taskbar flash. The taskbar button previously kept
  flashing after the cue was dismissed and only cleared once the window was brought to the foreground and
  minimized again ([#2](https://github.com/rxavier1979/ctrl-alt-stand/issues/2)).

## [0.3.0] - 2026-07-29

This release replaces the WinForms interface with a redesigned WPF interface. The timer behavior and the settings file are unchanged: schedules stay start-relative, and preferences and schedule memories are still read from and written to `%LOCALAPPDATA%\CtrlAltStand\settings.ini`. The legacy WinForms source remains in `src/` and is frozen at 0.2.0.

### Added

- Redesigned WPF interface as the shipping application.
- Refined dark theme across the window, controls, and cue.
- Circular progress ring showing the remaining time in the current phase.
- Accent color that follows the active phase.
- Persistent transition cue that stays on screen until it is acknowledged.
- Taskbar and system-tray icon.
- Automatic window sizing to fit the available screen space.

## [0.2.0] - 2026-07-22

### Added

- Two persistent car-seat-style schedule memories using **Set**, **1**, and **2** controls.
- **Defaults** control for restoring the original 30 / 20 / 3 routine.
- Preferences and schedule memories stored in `%LOCALAPPDATA%\CtrlAltStand\settings.ini`, independently of the executable.
- MIT License with copyright credit to Raul Soto.

### Fixed

- Prevented phase labels, countdown text, buttons, and duration controls from being clipped at elevated Windows display-scaling levels.
- Improved high-DPI layout for the timer, controls, Start-with selector, and Schedule frame.

## [0.1.0] - 2026-07-22

First public Windows release.

### Added

- Start-relative Sit, Stand, and optional Move routine.
- Adjustable durations with locally persisted settings.
- Persisted **Start with Sit / Stand** selector for choosing the first phase of each work session.
- Color, sound, notification, and taskbar transition cues.
- Always-on-top option and system-tray controls.
- High-DPI Windows layout support.
- Native Windows executable with no third-party runtime dependencies.
- No accounts, network access, or telemetry.
