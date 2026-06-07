# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## You Must

- Please respond in Japanese.
- Commit to Git only when instructed.
- If multiple requirements are given in a single instruction, divide the commits into appropriate sizes/granularities.

## Build & Development

This is a Windows Forms application (.NET 9.0 / C#) for Microsoft Store distribution.

**Solution structure:**

- `RunCat365.sln` - Main solution file
- `RunCat365/` - Main application project
- `WapForStore/` - Windows Application Packaging project for Microsoft Store

**Build:**

- Open `RunCat365.sln` in Visual Studio
- Supported platforms: x64, x86, ARM64
- Target framework: .NET 9.0 (Windows 10.0.26100.0)

**Version numbers** must be updated in two places when releasing:

1. `RunCat365/RunCat365.csproj` - `<Version>X.Y.Z</Version>` (3-digit)
2. `WapForStore/Package.appxmanifest` - `Version="X.Y.Z.0"` in `<Identity>` element (4-digit)

## Architecture

**Entry point:** `Program.cs` contains `RunCat365ApplicationContext` which manages the application lifecycle as a system tray application.

**Core components:**

- `ContextMenuManager` - Manages the system tray icon, context menu, and notification icon animation; uses `iconLock` for thread-safe icon updates
- `Runner` - Enum for animation types (Cat, Parrot, Horse) with frame counts
- `EndlessGameForm` - Mini-game featuring the running cat
- `LaunchAtStartupManager` - Startup registration via Windows App Runtime

**System information repositories (Repository pattern):**

- `CPURepository` - CPU usage via PerformanceCounter
- `GPURepository` - GPU usage monitoring
- `MemoryRepository` - Memory usage
- `StorageRepository` - Disk usage
- `NetworkRepository` - Network statistics

**Animation flow:**

1. `fetchTimer` (1s interval) updates system info into `*Info` structs (CPUInfo, GPUInfo, etc.)
2. `animateTimer` advances frames based on the selected `SpeedSource` (CPU/GPU/Memory)
3. `BitmapExtension` handles theme-aware icon recoloring and conversion

**EndlessGame components:**

- `Cat` - Running/Jumping state and collision frame data
- `Road` - Obstacle types (Flat/Hill/Crater/Sprout)
- `GameStatus` - Game state (NewGame/Playing/GameOver)

**Utilities:**

- `ByteFormatter` - Formats byte values to human-readable strings (B/KB/MB/GB/TB)
- `TreeFormatter` - Formats system info for context menu display (language-aware)

**Settings:**

- `Properties/UserSettings.settings` - User preferences (Runner, Theme, SpeedSource, FPSMaxLimit)
- `Properties/Resources.resx` - Embedded images and icons
- `Properties/Strings.resx` - Localized strings (English default); per-locale variants live alongside as `Properties/Strings.{locale}.resx`

**Localization notes:**

- Add new strings to every `Strings*.resx` file simultaneously
- Per-locale font and culture settings live in `SupportedLanguage.cs` (`GetFontName`, `GetDefaultCultureInfo`, `IsFullWidth`)
- When adding a brand-new locale (not just new strings), follow the `.claude/skills/add-locale` skill

## Coding Rules

- Do not write comments within the source code.
- Use naming conventions that clearly indicate the purpose of the code, even without comments.
- In C# code:
  - Abbreviations such as URL or ID should be written in all lowercase or all uppercase (do not use Upper Camel Case for these prefixes).
  - Do not use abbreviations such as `img` for `image` or `cnt` for `count`.
