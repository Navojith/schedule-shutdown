# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project
**Schedule Shutdown**: a Windows 11 desktop app that shuts the PC down at an exact time or after a countdown timer, with an optional force-shutdown mode for when apps block shutdown.

- Full plan, architecture and UI spec: `docs/PLAN.md`. **Read it before starting any phase.**
- Current status: `progress.md`. **Read it at the start of every session and update it at the end** (tick checkboxes, set "Current phase", add a row to the session log, note open questions).

## Tech stack
- C# / **.NET 9**, **WinUI 3** (Windows App SDK 2.6.x), unpackaged + self-contained (`WindowsAppSDKSelfContained=true`), x64 only (`RuntimeIdentifier` win-x64 set in the csproj)
- No WinUI `dotnet new` template is installed: App project files are hand-written, so create new XAML pages by copying the existing pattern (`.xaml` + `.xaml.cs` partial class)
- MVVM with **CommunityToolkit.Mvvm**, DI via `Microsoft.Extensions.DependencyInjection`
- Tray: **H.NotifyIcon.WinUI**. Toasts: `AppNotificationManager`
- Tests: **xUnit** + FluentAssertions **7.2.0** (pinned: v8+ is commercially licensed, so never upgrade it)
- Shared MSBuild settings (Nullable, ImplicitUsings, LangVersion) live in `Directory.Build.props`
- Installer: **WiX Toolset v5** MSI (`dotnet tool install --global wix`)
- Target: Windows 11, x64 (TFM `net9.0-windows10.0.22621.0`, min version 10.0.22000.0)

## Layout
```
src/ScheduleShutdown.Core/        scheduling logic: pure .NET, no UI, no Windows APIs
src/ScheduleShutdown.App/         WinUI 3 app: Views, ViewModels, Services
tests/ScheduleShutdown.Core.Tests xUnit tests for Core
installer/ScheduleShutdown.Installer  WiX MSI project
tools/make-icon.ps1               regenerates the app icon (Assets/AppIcon.ico + .png)
docs/PLAN.md, docs/QA.md          plan; manual QA checklist
```

## Commands
```powershell
dotnet build ScheduleShutdown.sln
dotnet test
$env:SCHEDULE_SHUTDOWN_DRYRUN = "1"; dotnet run --project src/ScheduleShutdown.App
dotnet publish src/ScheduleShutdown.App -c Release -r win-x64 --self-contained
dotnet build installer/ScheduleShutdown.Installer -c Release
```
The shell is Windows PowerShell 5.1: chain commands with `;`, not `&&`.

## Critical safety rules
- **Never trigger a real shutdown during development.** Always run the app with `SCHEDULE_SHUTDOWN_DRYRUN=1`, which makes the executor log the command instead of running it. Never run `shutdown.exe /s` (or `/r`, `/p`, `/h`) from the terminal. `shutdown /a` (abort) is harmless.
- Real-shutdown testing (Phase 8) happens only when the user explicitly asks for it and confirms their work is saved.

## Key design rules
- Shutdown is executed as `shutdown.exe /s /t 0`, plus `/f` only when force is enabled. **Never use `/t N` with N > 0**: Windows then implies `/f` automatically, which would silently force-close apps.
- The app owns the countdown. The scheduler stores an **absolute target `DateTimeOffset`** (Timer mode too) and compares it against `IClock`, so sleep/hibernate don't skew it.
- If the target has already passed (resume from sleep, or restored on launch), never shut down instantly. Show the 60 s final countdown dialog first. Discard targets more than 12 h old.
- All scheduling logic belongs in `Core` behind interfaces (`IClock`, `ITimerFactory`, `IShutdownExecutor`, `IStateStore`) and must be unit-tested with fakes, with no real `Task.Delay`/sleeps in tests.
- The App layer stays thin: views bind to view models, and view models call Core and services. No logic in code-behind beyond UI glue.
- Force shutdown is **off by default** and always shows its data-loss warning.
- One app instance only (`AppInstance.FindOrRegisterForKey`). Closing the window hides to tray; Exit with an active schedule asks for confirmation.
- State lives in `%LOCALAPPDATA%\ScheduleShutdown\state.json`.

## Conventions
- Nullable reference types enabled; treat warnings as errors in Core.
- File-scoped namespaces, `PascalCase` public members, `_camelCase` private fields.
- Use `[ObservableProperty]` / `[RelayCommand]` source generators rather than hand-written INotifyPropertyChanged.
- Use Fluent/WinUI built-in controls and theme resources (`{ThemeResource ...}`); don't hard-code colors, so light and dark themes both work.
- Add or adjust tests alongside every Core change; `dotnet test` must pass before a phase is marked done in `progress.md`.
