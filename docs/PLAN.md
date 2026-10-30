# Schedule Shutdown — Project Plan

A Windows 11 desktop app with a modern Fluent UI that shuts the PC down at an exact time or after a countdown, with an optional **force shutdown** for when running apps block the shutdown.

---

## 1. Goals and scope

### Must have (v1.0)
1. **Two scheduling modes**
   - **At time**: pick a date and time (defaults to today; if the time has already passed today, offer tomorrow).
   - **Timer**: pick a duration (hours/minutes, plus quick presets: 15m, 30m, 1h, 2h).
2. **Force shutdown toggle**: closes apps without waiting for them, so a hung app or an unsaved-changes dialog can't block shutdown. Off by default, with a short warning that unsaved work will be lost.
3. **Live countdown** in the main window showing the target time and time remaining.
4. **Cancel** a scheduled shutdown at any time.
5. **Warnings before shutdown**: toast notifications at 10 min and 1 min, then a 60-second dialog with **Cancel** and **Postpone 10 min**.
6. **System tray**: closing the window minimizes to tray; the tray icon shows the remaining time in its tooltip; the tray menu has Open / Cancel shutdown / Exit.
7. **Persistence**: an active schedule survives an app restart (saved to disk and restored on launch).
8. **Installer**: a standard installer (MSI) that installs on Windows 11 without admin tricks, adds a Start-menu shortcut, and uninstalls cleanly.

### Nice to have (v1.x)
- Other actions: Restart, Sleep, Hibernate, Sign out.
- "Launch at Windows startup" option.
- Light/dark/system theme selection (follows system by default).
- Recent schedules / favourite presets.
- Backup schedule in Windows Task Scheduler so the shutdown still happens if the app is killed.

### Out of scope
- Recurring (daily/weekly) schedules, remote/network shutdown, Windows 10 and older support, macOS/Linux.

---

## 2. Tech stack

| Concern | Choice | Why |
|---|---|---|
| Language / runtime | **C# on .NET 9** | .NET 9 SDK and Visual Studio 2022 are already installed on the dev machine, so there's no new toolchain to set up. |
| UI framework | **WinUI 3 (Windows App SDK 2.6+)** | Microsoft's native Windows 11 UI: Fluent controls, Mica backdrop, rounded corners, automatic light/dark theme. Gives the best "nice UI" with the least custom styling. |
| Architecture | **MVVM** with **CommunityToolkit.Mvvm** | Source-generated `ObservableProperty`/`RelayCommand`, so view models stay small and testable. |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` | Standard wiring for services and view models. |
| System tray | **H.NotifyIcon.WinUI** | The maintained tray-icon library for WinUI 3 (WinUI has no built-in tray support). |
| Notifications | Windows App SDK `AppNotificationManager` | Native Windows 11 toast notifications. |
| Settings / state | JSON file in `%LOCALAPPDATA%\ScheduleShutdown\state.json` via `System.Text.Json` | Simple and transparent; works for unpackaged apps. |
| Tests | **xUnit** + **FluentAssertions** | Unit tests for the scheduling core, which has no UI dependency. |
| Packaging | **Unpackaged, self-contained** app (`WindowsAppSDKSelfContained=true`) + **WiX Toolset v5** MSI (installed as a `dotnet tool`) | No MSIX code-signing certificate needed for users to install; the MSI is a normal double-click installer. WiX v5 runs from the .NET CLI, so it fits the existing toolchain. |

### Alternatives considered
- **Tauri 2 (Rust + React)**: small installers and flexible web UI, but needs the Rust toolchain (not installed) and looks less native without extra work. A good fallback if cross-platform is ever wanted.
- **Electron**: easy, but an installer of roughly 100 MB is too heavy for a utility this small.
- **WPF + WPF-UI**: mature and simple to package, and the fallback if WinUI 3 unpackaged deployment causes trouble. Looks slightly less native.
- **MSIX packaging**: the cleanest install/uninstall, but sideloading needs a trusted signing certificate. Revisit if the app goes to the Microsoft Store.

---

## 3. How shutdown works (key technical decisions)

### 3.1 Running the shutdown
At the target moment the app runs:

```
shutdown.exe /s /t 0          # normal shutdown
shutdown.exe /s /t 0 /f       # force shutdown
```

It starts the process with `CreateNoWindow = true` and `UseShellExecute = false`. `shutdown.exe` needs no admin rights for the local machine.

> **Important:** `shutdown /s /t N` with `N > 0` **always implies `/f`** (per Microsoft's documentation). So we can't hand the whole countdown to `shutdown.exe /t` without silently forcing it. That's why the app keeps its own timer and always calls `/t 0`, adding `/f` only when the user enabled force.

As a safety net, on app start the app runs `shutdown /a` (failures ignored) only if its own state says a legacy OS-level timer may be pending. Normal operation never uses OS-level timers.

### 3.2 Timer engine (`ShutdownScheduler`, in Core)
- Stores an **absolute target `DateTimeOffset`**, even in Timer mode (`target = now + duration`). Comparing against the wall clock stays correct after sleep/hibernate, unlike counting down ticks.
- Ticks once per second (through an injectable `IClock` and timer abstraction, so tests run without real time) and raises:
  - `Tick(remaining)`: drives the UI countdown and tray tooltip.
  - `WarningReached(threshold)`: at 10 min and 1 min.
  - `FinalCountdownStarted`: at 60 s, which opens the warning dialog.
  - `DueReached`: calls `IShutdownExecutor.Execute(force)`.
- **Resume from sleep**: if the PC wakes up after the target has already passed, the app does **not** shut down instantly. It starts the 60-second final countdown dialog so the user can cancel.
- **Restored on launch**: same rule. If a saved target is already in the past, show the 60 s countdown. If it's more than 12 h in the past, discard it and notify instead.

### 3.3 Single instance
Uses `AppInstance.FindOrRegisterForKey("ScheduleShutdown")`. A second launch redirects activation to the running instance, which brings its window to the front. That way two timers can never compete.

---

## 4. Architecture and folder layout

```
schedule-shutdown/
├─ ScheduleShutdown.sln
├─ src/
│  ├─ ScheduleShutdown.Core/          # net9.0 class library, no UI and no Windows API
│  │  ├─ Models/        ScheduleRequest, ScheduleMode, ShutdownOptions, ScheduleState
│  │  ├─ Abstractions/  IClock, ITimerFactory, IShutdownExecutor, IStateStore
│  │  └─ Scheduling/    ShutdownScheduler, ScheduleCalculator (time/duration → target)
│  └─ ScheduleShutdown.App/           # WinUI 3, net9.0-windows10.0.22621.0
│     ├─ App.xaml(.cs)                # DI setup, single instance, tray
│     ├─ Views/         MainWindow, MainPage, FinalCountdownDialog, SettingsPage
│     ├─ ViewModels/    MainViewModel, CountdownViewModel, SettingsViewModel
│     ├─ Services/      ProcessShutdownExecutor, JsonStateStore, NotificationService, TrayService
│     └─ Assets/        app icon (.ico + PNG sizes)
├─ tests/
│  └─ ScheduleShutdown.Core.Tests/    # xUnit
├─ installer/
│  └─ ScheduleShutdown.Installer/     # WiX v5 project → .msi
└─ docs/PLAN.md
```

**Rule:** all scheduling logic lives in `Core` and is unit-tested. The App layer only wires UI and Windows APIs onto the Core interfaces.

---

## 5. UI design

A single compact window (about 460×620, not resizable below that) with a **Mica** backdrop and a custom title bar.

```
┌───────────────────────────────────────────┐
│  ⏻  Schedule Shutdown                ─  ✕ │
├───────────────────────────────────────────┤
│   [  At time  |  Timer  ]   (Segmented)   │
│                                           │
│   At time:   [ TimePicker ] [ DatePicker ]│
│   Timer:     [ 1 ] h  [ 30 ] min          │
│              (15m) (30m) (1h) (2h)        │
│                                           │
│   ☐ Force shutdown                        │
│     Closes apps without saving. Use if an │
│     app is blocking shutdown.             │
│                                           │
│        [  Schedule shutdown  ] (accent)   │
├───────────────────────────────────────────┤
│  When scheduled, this panel replaces the  │
│  form:                                    │
│        ◯ big progress ring                │
│          01:29:57                         │
│     Shuts down at 23:30 · Force: Off      │
│   [ +10 min ]   [ Cancel shutdown ]       │
└───────────────────────────────────────────┘
```

- Fluent controls throughout: `SelectorBar`/segmented toggle, `TimePicker`, `CalendarDatePicker`, `NumberBox`, `ToggleSwitch` (force), `ProgressRing`, `InfoBar` for errors.
- Follows the system light/dark theme and accent color.
- Final countdown: a `ContentDialog` (or a small always-on-top window if the main window is hidden) with a big seconds counter, **Cancel** and **Postpone 10 min**.
- Keyboard accessible, with AutomationProperties names on all controls.

---

## 6. Implementation phases

| Phase | Deliverable | Done when |
|---|---|---|
| **0. Setup** | Solution, 3 projects + test project, `.gitignore`, `git init`, build passes | `dotnet build` and `dotnet test` succeed |
| **1. Core logic** | Models, `ScheduleCalculator`, `ShutdownScheduler` with fake clock, `IStateStore` contract | Unit tests cover: time-in-past → tomorrow, duration → target, warning thresholds, cancel, postpone, overdue-on-resume, restore-from-state |
| **2. Shutdown executor** | `ProcessShutdownExecutor` (+ `/f` flag), dry-run mode via `SCHEDULE_SHUTDOWN_DRYRUN=1` env var that logs instead of shutting down | Dry run logs the right command for normal and force |
| **3. Main UI** | MainWindow with Mica, both modes, force toggle, schedule/cancel, live countdown | Can schedule and cancel in dry-run mode |
| **4. Warnings and tray** | Toasts at 10/1 min, final 60 s dialog, tray icon + menu, close-to-tray | All warning paths work in dry run |
| **5. Persistence and single instance** | `JsonStateStore`, restore on launch, single-instance redirect | Kill and relaunch restores the schedule; a second launch focuses the first |
| **6. Polish** | Icon, theme, accessibility pass, input validation, error `InfoBar`s, settings page | Manual QA checklist passes |
| **7. Installer** | WiX MSI: Program Files install, Start-menu shortcut, uninstall removes app + optional state | MSI installs/uninstalls cleanly on a clean Windows 11 machine/VM |
| **8. Real-shutdown test** | End-to-end test with a real shutdown, with and without force (Notepad with unsaved text as the blocker) | Normal shutdown is blocked by Notepad; force shutdown succeeds |

---

## 7. Testing strategy
- **Unit (Core)**: deterministic tests with a `FakeClock`/`FakeTimer`, with no real waiting.
- **Dry-run mode**: `SCHEDULE_SHUTDOWN_DRYRUN=1` swaps in a logging executor. **Always use this during development.** Never trigger a real shutdown while developing.
- **Manual QA checklist** (filled in during Phase 6): both modes, force on/off, cancel, postpone, sleep/resume, restart app mid-schedule, tray behavior, light/dark theme, 125%/150% DPI scaling.
- **Real shutdown**: only in Phase 8, on a VM or after saving all work.

## 8. Risks and mitigations
| Risk | Mitigation |
|---|---|
| WinUI 3 unpackaged deployment quirks (bootstrapper, runtime) | Use `WindowsAppSDKSelfContained=true`. Fall back to WPF + WPF-UI if it blocks progress. |
| App closed or killed means no shutdown | Close-to-tray by default; Exit asks for confirmation when a schedule is active; optional Task Scheduler backup in v1.x. |
| Sleep/hibernate during countdown | Absolute target time plus the overdue-on-resume rule (§3.2). |
| Force shutdown loses user data | Off by default, explicit warning text, 60 s final dialog. |
| SmartScreen warning for an unsigned MSI | Document it in the README; consider code signing before public release. |

## 9. Build and release commands (planned)
```powershell
dotnet build ScheduleShutdown.sln -c Debug
dotnet test
$env:SCHEDULE_SHUTDOWN_DRYRUN = "1"; dotnet run --project src/ScheduleShutdown.App
dotnet publish src/ScheduleShutdown.App -c Release -r win-x64 --self-contained
dotnet tool install --global wix
dotnet build installer/ScheduleShutdown.Installer -c Release   # → .msi
```
