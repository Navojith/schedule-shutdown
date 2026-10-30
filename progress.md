# Progress

Tracks implementation status against [docs/PLAN.md](docs/PLAN.md). Update this file at the end of every work session.

Legend: `[x]` done · `[~]` in progress · `[ ]` not started

## Status summary
- **Current phase:** 5 — Persistence & single instance (next up; Phase 4 done)
- **Last updated:** 2026-10-30

## Phases

### Phase 0 — Setup
- [x] Plan written (`docs/PLAN.md`)
- [x] `CLAUDE.md` and `progress.md` created
- [x] Dev environment checked: .NET SDK 9.0.300, Visual Studio Community 2022, Node 24 (not needed). Rust not installed (not needed).
- [x] `git init` (branch `main`) + `.gitignore` (`dotnet new gitignore`)
- [x] Create `ScheduleShutdown.sln` with Core, App, Core.Tests projects + shared `Directory.Build.props`
- [x] WinUI 3 app builds unpackaged + self-contained (Windows App SDK 2.6.4) and launches: Mica window "Schedule Shutdown", 460×620, not maximizable
- [x] `dotnet build` passes with 0 warnings; `dotnet test` runs (no tests yet, added in Phase 1)

### Phase 1 — Core logic
- [x] Models (`ScheduleMode`, `ScheduleRequest`, `ShutdownOptions`, `ScheduleState`, `ScheduleResult`/`ScheduleError`, `RestoreOutcome`)
- [x] Abstractions (`IClock`, `ITimerFactory`/`ITickTimer`, `IShutdownExecutor`, `IStateStore`) + `SystemClock`
- [x] `ScheduleCalculator` + tests
- [x] `ShutdownScheduler` (tick, warnings, final countdown, due, cancel, postpone, overdue, restore) + tests (49 tests passing)

### Phase 2 — Shutdown executor
- [x] `ShutdownCommand` (Core): `System32\shutdown.exe /s /t 0` (+ `/f`) + tests
- [x] `ProcessShutdownExecutor` (App): no window, waits up to 10 s, throws on non-zero exit code
- [x] `DryRunShutdownExecutor` (Core) + `DryRunMode` env-var check + tests; `ShutdownExecutorFactory` (App) picks one; window title shows "(dry run)"
- [x] `AppLog`: `%LOCALAPPDATA%\ScheduleShutdown\app.log` + debug output (dry-run commands land here)

### Phase 3 — Main UI
- [x] DI composition root in `App.xaml.cs`; `DispatcherQueueTimerFactory`; `MainViewModel` (CommunityToolkit.Mvvm partial properties)
- [x] `Views/MainWindow` with Mica + custom title bar ("(dry run)" suffix) hosting `Views/MainPage`
- [x] At-time mode (time picks today/tomorrow automatically) / Timer mode + presets; validation errors in an InfoBar
- [x] Force shutdown toggle with description + warning InfoBar
- [x] Active-schedule panel (progress ring, countdown, summary, +10 min, cancel); Schedule button pinned to bottom
- [x] `ScheduleFormatter` (Core) + tests; 73 tests pass
- [x] Verified in dry run via UI Automation: schedule, +10 min, cancel, and a 1-min forced timer logged `shutdown.exe /s /t 0 /f` on time

### Phase 4 — Warnings & tray
- [x] `NotificationService`: toasts at 10 min / 1 min with Postpone / Cancel buttons (tagged, so 1 min replaces 10 min; removed when the schedule ends), tray hint on first close, dry-run "no shutdown" toast
- [x] `FinalCountdownWindow` + `CountdownViewModel`: always-on-top, centred, Cancel focused, closes itself when the countdown ends
- [x] `TrayIconService` (H.NotifyIcon.WinUI 2.3.2): placeholder power icon, live countdown tooltip, Open / Cancel shutdown / Exit menu, left-click opens; clears stale icons left by killed instances
- [x] `AppShell`: close-to-tray, Exit confirmation ("Exit and cancel" / "Keep running"), `WindowFocus.BringToFront`
- [x] `ScheduleFormatter.CountdownSeconds` / `DescribeLeadTime` (Core) + tests; 84 tests pass
- [x] `WindowSizing`: windows sized in DIPs (fixes the high-DPI size issue)
- [x] Verified in dry run via UI Automation: 10-min and 1-min toasts, countdown window while hidden in tray, postpone from countdown, run to due (dry-run log + toast), tray Open, tray Exit -> confirm -> Keep running / Exit and cancel (icon removed)
- [ ] Toast button clicks (Postpone / Cancel on the toast itself): UIA can't reach toast buttons, so these need a manual click

### Phase 5 — Persistence & single instance
- [ ] `JsonStateStore` (`%LOCALAPPDATA%\ScheduleShutdown\state.json`)
- [ ] Restore schedule on launch (overdue rules)
- [ ] Single-instance redirect

### Phase 6 — Polish
- [ ] App icon, theme, accessibility, validation, error InfoBars, settings page
- [ ] Manual QA checklist written and passed

### Phase 7 — Installer
- [ ] WiX v5 MSI project
- [ ] Install/uninstall verified on clean Windows 11

### Phase 8 — Real-shutdown test
- [ ] Normal shutdown (blocked by unsaved Notepad as expected)
- [ ] Force shutdown succeeds

## Session log
| Date | Summary |
|---|---|
| 2026-10-30 | Planning: wrote PLAN.md, CLAUDE.md, progress.md. Picked C# / .NET 9 / WinUI 3 / WiX v5. |
| 2026-10-30 | Phase 0 done: git repo, solution, 3 projects, hand-written WinUI 3 csproj, verified build + launch. |
| 2026-10-30 | Phase 1 done: Core models, abstractions, `ScheduleCalculator`, `ShutdownScheduler`, test fakes; 49 tests pass, 0 warnings. |
| 2026-10-30 | Phase 2 done: shutdown command + dry-run executor in Core (tested), process executor, factory and log in App; 63 tests pass, 0 warnings. |
| 2026-10-30 | Phase 3 done: DI, main window/page, view model, formatter; verified schedule/postpone/cancel/due in dry run. 73 tests pass, 0 warnings. |
| 2026-10-30 | Phase 4 done: toasts, final countdown window, tray icon/menu, close-to-tray, exit confirmation, DIP window sizing; verified in dry run. 84 tests pass, 0 warnings. Toast-button clicks still need a manual check. |

## Open questions / decisions
- No WinUI CLI template is installed, so the App project files were written by hand. Add new pages/windows by copying the existing XAML + code-behind pattern.
- FluentAssertions pinned to **7.2.0**: v8+ needs a paid commercial license. Don't upgrade.
- `AppWindow.Resize` uses physical pixels; fixed in Phase 4 with `WindowSizing.Resize` (DIPs via `GetDpiForWindow`). Windows don't re-size when moved to a monitor with another DPI (Phase 6).
- Timer abstraction is `ITickTimer`, not `ITimer`, because `System.Threading.ITimer` (implicit usings) would clash. No real timer implementation in Core: the App provides a `DispatcherQueue`-based `ITimerFactory` in Phase 3 (the scheduler isn't thread-safe and expects UI-thread ticks).
- Late final countdown rule: if the final countdown would start with < 55 s left (sleep/resume, overdue restore, at-time a few seconds away), the target moves to now + 60 s and is re-saved, so the user always gets about a full minute to cancel.
- Warnings: thresholds at or above the remaining time when scheduling/postponing never fire. After a sleep that crosses several thresholds, only the smallest one fires.
- Max lead time is 7 days in both modes; min timer is 1 min. Postpone resets warnings and leaves the final countdown.
- `ShutdownScheduler.Evaluate()` is public so the App can call it on resume from sleep (Phase 4/5).
- Nothing calls `ShutdownExecutorFactory` yet: Phase 3 wires it (and the scheduler) into DI. The real executor has no automated test on purpose; it is only exercised in Phase 8.
- Dry-run accepts `1` or `true`. The real executor is used for any other value, so always check the "(dry run)" window title before testing.
- App project uses `LangVersion=preview`: CommunityToolkit.Mvvm 8.4 needs it for `[ObservableProperty]` partial properties (field-based ones raise WinRT AOT warnings). Partial properties can't have initializers, so set defaults in the constructor.
- `IStateStore` is a temporary `InMemoryStateStore` in the App until Phase 5's `JsonStateStore`.
- Exit from the tray with an active schedule **cancels** it (after confirmation), so Phase 5's restore never brings back a schedule the user exited out of. Only kill/crash leaves a saved schedule to restore.
- Toasts show "ScheduleShutdown" (the exe name) as the app name, because `AppNotificationManager.Register()` has no display name or icon for unpackaged apps. Fix in Phase 6 with the app icon (`Register(displayName, iconUri)`).
- Tray icon is a placeholder drawn with GDI+ shapes (`PlaceholderTrayIcon`). H.NotifyIcon's `GeneratedIconSource` didn't render the Segoe Fluent glyph. Replace with the real .ico in Phase 6.
- H.NotifyIcon.WinUI is pinned to **2.3.2**: 2.4.x targets .NET 10 only. `ForceCreate(false)` keeps Efficiency Mode off (it would throttle the countdown timer).
- Tray icon GUID comes from the exe path (`CreateUniqueGuidForProcessPath`), and stale icons with that GUID are deleted on start (`NIM_DELETE`).
- A clicked toast when the app isn't running launches a new instance; Phase 5's single-instance redirect should route that activation to the running app.
- `dotnet build` on the App **project** outputs to `bin\Debug\...`; the **solution** build outputs to `bin\x64\Debug\...`. Launch the exe matching the build you just ran.
- UI-automation tests must not send keystrokes (`SendKeys {ESC}`): they land in the Claude Code terminal and interrupt the session. The tray menu is owner-drawn (no UIA items); click inside its `#32768` window rect instead.
- PowerShell 5.1 `Get-Content` reads UTF-8 files as ANSI and mangles `·`/`–`. Edit source files with the Edit tool, not PowerShell string replacement.
