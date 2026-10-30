# Progress

Tracks implementation status against [docs/PLAN.md](docs/PLAN.md). Update this file at the end of every work session.

Legend: `[x]` done · `[~]` in progress · `[ ]` not started

## Status summary
- **Current phase:** 8 done — all phases built and verified. Open: Phase 6 manual QA items, SmartScreen (7.10), Settings-UI uninstall click-through (7.7).
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
- [x] `JsonStateStore` (Core, `Persistence/`): versioned camelCase JSON, enum names, atomic temp-file write, never throws (errors go to `app.log`); invalid/unknown files load as null + tests; 103 tests pass
- [x] Restore schedule on launch (overdue rules) with an InfoBar per outcome (restored / overdue / stale discarded); overdue opens the 60 s countdown on top of the main window
- [x] Single-instance redirect: custom `Program.Main` (`DISABLE_XAML_GENERATED_MAIN`), `AppInstance.FindOrRegisterForKey`, second launch redirects and exits; running instance shows its window (even from the tray) or applies a redirected toast action
- [x] Toast clicked while the app isn't running: launch activation's action (Postpone / Cancel) is applied after restore
- [x] Verified in dry run via UI Automation: kill + relaunch restores (same target), second launch exits with code 0 and brings the hidden window to the foreground, overdue restore shows countdown and Cancel clears the state, stale (13 h) discarded with InfoBar, corrupt file ignored and logged
- [ ] Toast-launch activation (click a toast's Cancel after killing the app): needs a manual click, like Phase 4's toast buttons

### Phase 6 — Polish
- [x] App icon: `tools/make-icon.ps1` generates `Assets/AppIcon.ico` (16–64 px DIB + 256 px PNG) and `AppIcon.png`; used for the exe, window title bars, tray (placeholder removed) and toasts (`Register(displayName, iconUri)`)
- [x] Theme: `AppSettings` + `JsonSettingsStore` (Core, tested; shared `JsonFile` helper with `JsonStateStore`); `ThemeService` applies System/Light/Dark to all windows (content + caption buttons via `TitleBar.PreferredTheme`)
- [x] Settings page: theme, About (version 1.0.0), log folder; WinUI `TitleBar` with gear + back button and "Dry run" subtitle, `Frame` navigation
- [x] Accessibility: explicit names for presets, +10 min, force toggle (was "Force shutdown Off"); headings on Settings; decorative images hidden from UIA
- [x] Layout: closed InfoBars collapsed (removed the gap above the form); minimum window size 460 × 620 DIPs
- [x] Manual QA checklist written (`docs/QA.md`); automatable items checked in dry run; 113 tests pass
- [ ] Manual QA items marked ⬜ in `docs/QA.md` (toast buttons, keyboard, Narrator, high contrast, 150 % / multi-monitor, sleep/resume, theme restart)

### Phase 7 — Installer
- [x] WiX v5 MSI project (`installer/ScheduleShutdown.Installer`, WixToolset.Sdk 5.0.2 + UI/Util extensions; not in the .sln so normal builds stay fast). Its build runs `dotnet publish` (self-contained) into `obj\<Config>\app\` and harvests it with `<Files>`; output `bin\Release\ScheduleShutdown-x64.msi` (72 MB)
- [x] Per-machine install to `Program Files\Schedule Shutdown`, Start-menu shortcut, ARP icon, version bound from the exe, MajorUpgrade, WixUI_InstallDir without the license page, "Launch" checkbox on the last page
- [x] Uninstall/upgrade end the running app first (`taskkill` before `InstallValidate`); user data kept unless `REMOVEDATA=1`; `MSIFASTINSTALL=1` skips the restore point
- [x] Verified on this PC: UI pages via UIA, install (528 files, shortcut, Apps entry), installed exe runs in dry run, uninstall while running, `REMOVEDATA=1`
- [x] Clean Windows 11 Home VM (VirtualBox, no .NET): install, Start-menu launch + toast identity, upgrade 1.0.0 → 1.0.1 while running, uninstall while running (QA 7.4, 7.5, 7.7 via `UninstallString`, 7.8, 7.9)
- [ ] SmartScreen on a web-downloaded MSI (7.10); uninstall clicked through Settings → Apps (7.7 UI path)

### Phase 8 — Real-shutdown test
- [x] Normal shutdown: `shutdown.exe /s /t 0` ran at the target second; a blocker app (unsaved work) stopped it with Windows' "Closing 1 app" screen and the VM stayed on. Modern Notepad doesn't block (it keeps unsaved tabs).
- [x] Force shutdown: `shutdown.exe /s /t 0 /f` ran on time and the VM powered off within 8 s despite the blocker
- [x] Fix: main window overflows a 1024 × 768 screen at 125 % (QA 4.9): `WindowSizing` now caps the size and minimum size at the work area and moves the main window inside it (the form already scrolls above the pinned Schedule button)
- [x] Re-checked QA 4.9 in the VM (installed MSI, 1024 × 768 at 125 %): window 575 × 708 px at (96, 0), exactly the work-area height; Schedule button above the taskbar

## Session log
| Date | Summary |
|---|---|
| 2026-10-30 | Planning: wrote PLAN.md, CLAUDE.md, progress.md. Picked C# / .NET 9 / WinUI 3 / WiX v5. |
| 2026-10-30 | Phase 0 done: git repo, solution, 3 projects, hand-written WinUI 3 csproj, verified build + launch. |
| 2026-10-30 | Phase 1 done: Core models, abstractions, `ScheduleCalculator`, `ShutdownScheduler`, test fakes; 49 tests pass, 0 warnings. |
| 2026-10-30 | Phase 2 done: shutdown command + dry-run executor in Core (tested), process executor, factory and log in App; 63 tests pass, 0 warnings. |
| 2026-10-30 | Phase 3 done: DI, main window/page, view model, formatter; verified schedule/postpone/cancel/due in dry run. 73 tests pass, 0 warnings. |
| 2026-10-30 | Phase 4 done: toasts, final countdown window, tray icon/menu, close-to-tray, exit confirmation, DIP window sizing; verified in dry run. 84 tests pass, 0 warnings. Toast-button clicks still need a manual check. |
| 2026-10-30 | Phase 5 done: `JsonStateStore` + tests, restore on launch with InfoBars, single-instance redirect via custom Main, separate dry-run state file; verified in dry run. 103 tests pass, 0 warnings. |
| 2026-10-30 | Phase 6 built: icon + generator, settings page (theme, about), TitleBar navigation, theme service, accessibility names, min window size, QA checklist; verified in dry run. 113 tests pass, 0 warnings. Manual QA items still open. |
| 2026-10-30 | Phase 7 built: WiX v5 MSI (per-machine, shortcut, launch checkbox, kill-before-validate, `REMOVEDATA`); install/uninstall verified on this PC. First uninstall attempt hung on a Restart Manager "files in use" prompt; fixed. Clean-VM and upgrade checks open. |
| 2026-10-30 | Phase 7 VM checks + Phase 8 done in a VirtualBox Win11 Home VM (unattended install; VBoxSVGA showed a black screen, VMSVGA fixed it; live snapshots hang on top of Hyper-V, offline ones work). Clean install/upgrade/uninstall pass; real normal shutdown blocked as expected, forced shutdown powered the VM off. Found: window overflows small screens. |
| 2026-10-30 | Fixed QA 4.9: window size, minimum size and position clamped to the display's work area. Build 0 warnings, 113 tests pass; normal-size launch unchanged. VM re-check passed (MSI install, 575 × 708 px window in a 708 px work area). |

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
- `JsonStateStore` lives in Core (pure `System.Text.Json` + file I/O), so it's unit-tested against a temp directory. The App passes the path.
- Dry run uses **`state.dryrun.json`**, normal runs `state.json`: a schedule saved during development can never be restored by a normal launch and become a real shutdown.
- Single-instance key is `ScheduleShutdown` for both dry-run and normal launches, so a dry-run launch while a normal instance runs just focuses that instance (check the "(dry run)" title).
- Redirected activations arriving before `AppShell.Start` subscribes are dropped (only possible in the first moments of startup).
- Exit from the tray with an active schedule **cancels** it (after confirmation), so Phase 5's restore never brings back a schedule the user exited out of. Only kill/crash leaves a saved schedule to restore.
- Toast name/icon come from `Register(displayName, iconUri)`. Windows caches them per notification identity, which comes from the exe path: the dev build path still shows the old "ScheduleShutdown" header, while a fresh path (and the installed app) shows "Schedule Shutdown" + icon.
- ICO entries below 256 px must be BMP/DIB, not PNG: `System.Drawing.Icon` (used for the tray) shows PNG entries as noise.
- Settings live in `settings.json`, shared by dry-run and normal runs. The page has only the theme and About for now: launch at startup and other actions are v1.x nice-to-haves in PLAN.
- `OverlappedPresenter.PreferredMinimumWidth/Height` take physical pixels; `WindowSizing.SetMinimumSize` converts from DIPs.
- H.NotifyIcon.WinUI is pinned to **2.3.2**: 2.4.x targets .NET 10 only. `ForceCreate(false)` keeps Efficiency Mode off (it would throttle the countdown timer).
- Tray icon GUID comes from the exe path (`CreateUniqueGuidForProcessPath`), and stale icons with that GUID are deleted on start (`NIM_DELETE`).
- `dotnet build` on the App **project** outputs to `bin\Debug\...`; the **solution** build outputs to `bin\x64\Debug\...`. Launch the exe matching the build you just ran.
- UI-automation tests must not send keystrokes (`SendKeys {ESC}`): they land in the Claude Code terminal and interrupt the session. The tray menu is owner-drawn (no UIA items); click inside its `#32768` window rect instead.
- Installer `SuppressIces`: ICE03 (Microsoft.ui.xaml.dll's 85-language list overflows File.Language; two `.mui` files use LCID 1169), ICE43/ICE57 (false positives for a per-machine Start-menu shortcut). WiX has no switch to ignore file language info.
- The tray app ignores WM_CLOSE, so `util:CloseApplication` can't close it, and it runs after `InstallValidate` anyway, where Restart Manager blocks on a "files in use" prompt (an invisible hang under `/qb`). Uninstall/upgrade instead run `taskkill /F /IM ScheduleShutdown.exe` before `InstallValidate`. It kills every process with that name, including a dev build, but only when the product is installed or being upgraded.
- `REMOVEDATA=1` removes `%LOCALAPPDATA%\ScheduleShutdown` only for the user running the uninstall. Toast registration left by `AppNotificationManager.Register` under HKCU isn't cleaned up by the MSI.
- MSI `UpgradeCode` is `8B3A9ABE-6315-42E3-A2EC-317959024A93`: never change it. Bump `<Version>` in the App csproj for each release (the MSI version is bound from the exe).
- WiX CLI 5.0.2 installed as a global dotnet tool (only needed for ad-hoc `wix` commands; the build uses the SDK package). MSI dialog buttons have no UIA Invoke pattern: click them with `PostMessage(hwnd, BM_CLICK)`.
- Test VM: VirtualBox 7.2.22, VM `SchedShutdownTest` (Win11 Home, user `tester` / `test1234`, built-in Administrator enabled with the same password, snapshot `clean-win11` = before any install). `guestcontrol` runs non-elevated as tester, so elevated steps run as `Administrator`; desktop/UI steps run through a scheduled task in tester's session. Use the VMSVGA graphics controller, and take snapshots only while the VM is off.
- PowerShell 5.1 `Get-Content` reads UTF-8 files as ANSI and mangles `·`/`–`. Edit source files with the Edit tool, not PowerShell string replacement.
