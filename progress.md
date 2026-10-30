# Progress

Tracks implementation status against [docs/PLAN.md](docs/PLAN.md). Update this file at the end of every work session.

Legend: `[x]` done · `[~]` in progress · `[ ]` not started

## Status summary
- **Current phase:** 1 — Core logic (next up; Phase 0 done)
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
- [ ] Models (`ScheduleMode`, `ScheduleRequest`, `ShutdownOptions`, `ScheduleState`)
- [ ] Abstractions (`IClock`, `ITimerFactory`, `IShutdownExecutor`, `IStateStore`)
- [ ] `ScheduleCalculator` + tests
- [ ] `ShutdownScheduler` (tick, warnings, final countdown, due, cancel, postpone, overdue) + tests

### Phase 2 — Shutdown executor
- [ ] `ProcessShutdownExecutor` (normal / `/f` force)
- [ ] Dry-run executor via `SCHEDULE_SHUTDOWN_DRYRUN=1`

### Phase 3 — Main UI
- [ ] MainWindow with Mica + custom title bar
- [ ] At-time mode / Timer mode + presets
- [ ] Force shutdown toggle with warning text
- [ ] Active-schedule panel (countdown, +10 min, cancel)

### Phase 4 — Warnings & tray
- [ ] Toasts at 10 min / 1 min
- [ ] Final 60 s countdown dialog (Cancel / Postpone)
- [ ] Tray icon, tooltip countdown, menu, close-to-tray

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

## Open questions / decisions
- No WinUI CLI template is installed, so the App project files were written by hand. Add new pages/windows by copying the existing XAML + code-behind pattern.
- FluentAssertions pinned to **7.2.0**: v8+ needs a paid commercial license. Don't upgrade.
- `AppWindow.Resize` uses physical pixels, so the window is smaller on high-DPI screens. Fix with DPI scaling in Phase 6.
