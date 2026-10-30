# Manual QA checklist

Run every item with `SCHEDULE_SHUTDOWN_DRYRUN=1` and check that the title bar shows **Dry run** before starting. Real shutdowns are tested only in Phase 8.

```powershell
$env:SCHEDULE_SHUTDOWN_DRYRUN = "1"; dotnet run --project src/ScheduleShutdown.App
```

Data folder: `%LOCALAPPDATA%\ScheduleShutdown` (`app.log`, `settings.json`, `state.dryrun.json`). Dry-run "shutdowns" appear in `app.log` as `[DRY RUN] Would run: ...`.

Status: ✅ passed · ❌ failed · ⬜ not run yet. "Auto" means checked through UI Automation in a dry run; everything else needs a person.

## 1. Scheduling
| # | Check | Expected | Status |
|---|---|---|---|
| 1.1 | At time: pick a time later today | Date stays today; Schedule shows the active panel with the right "Shuts down today at …" | ✅ Auto (Phase 3) |
| 1.2 | At time: pick a time that already passed today | Date moves to tomorrow automatically | ✅ Auto (Phase 3) |
| 1.3 | At time: pick a past date + time | Error InfoBar "That time has already passed…" | ⬜ |
| 1.4 | At time: more than 7 days ahead | Error InfoBar "Pick a time within the next 7 days." | ⬜ |
| 1.5 | Timer: each preset (15 min, 30 min, 1 h, 2 h) | Hours/Minutes update | ✅ Auto |
| 1.6 | Timer: 0 h 0 min | Error InfoBar "Set a timer of at least 1 minute." | ⬜ |
| 1.7 | Timer: clear a NumberBox, or type letters / decimals | Treated as 0 or rounded down; no crash | ⬜ |
| 1.8 | +10 min on the active panel | Countdown jumps up 10 min; ring refills | ✅ Auto (Phase 3) |
| 1.9 | Cancel shutdown on the active panel | Form comes back; `app.log` says "Cancelled shutdown" | ✅ Auto (Phase 3) |

## 2. Force shutdown
| # | Check | Expected | Status |
|---|---|---|---|
| 2.1 | Default state on launch | Force is **Off** | ✅ Auto |
| 2.2 | Turn Force on | "Unsaved work will be lost." warning appears | ✅ Auto |
| 2.3 | 1-min timer, force on, let it run out | Countdown window says force is on; log shows `shutdown.exe /s /t 0 /f` | ✅ Auto |
| 2.4 | Same with force off | Log shows `shutdown.exe /s /t 0` (no `/f`) | ✅ Auto (Phase 3) |

## 3. Warnings and final countdown
| # | Check | Expected | Status |
|---|---|---|---|
| 3.1 | 11-min timer | Toast at 10 min, then at 1 min (replaces the first) | ✅ Auto (Phase 4) |
| 3.2 | Click **Postpone 10 min** on a warning toast | Schedule moves 10 min later | ⬜ |
| 3.3 | Click **Cancel shutdown** on a warning toast | Schedule cancelled | ⬜ |
| 3.4 | Countdown window at 60 s | Always on top, centred, Cancel focused | ✅ Auto (Phase 4) |
| 3.5 | Countdown window: Postpone / Cancel | Window closes; schedule postponed / cancelled | ✅ Auto (Phase 4) |
| 3.6 | Countdown window while the main window is in the tray | Window still appears | ✅ Auto (Phase 4) |
| 3.7 | Toasts show "Schedule Shutdown" and the app icon | Header has the name and icon | ✅ (fresh exe path; see note) |

## 4. Tray and window
| # | Check | Expected | Status |
|---|---|---|---|
| 4.1 | Close the window | Hides to tray; first time only, a "still running" toast | ✅ Auto (Phase 4) |
| 4.2 | Tray tooltip | Shows the remaining time, or "No shutdown scheduled" | ✅ Auto (Phase 4) |
| 4.3 | Tray left-click / Open | Window comes to the front | ✅ Auto (Phase 4) |
| 4.4 | Tray → Cancel shutdown | Schedule cancelled | ⬜ |
| 4.5 | Tray → Exit with a schedule | Confirmation; Keep running keeps it, Exit and cancel exits | ✅ Auto (Phase 4) |
| 4.6 | Tray icon looks sharp at 100 %, 125 %, 150 % scaling | Blue power icon, not blurry | ⬜ (125 % ✅) |
| 4.7 | Resize the window smaller | Can't go below 460 × 620 | ✅ Auto |
| 4.8 | Maximize | Disabled | ⬜ |

## 5. Persistence and single instance
| # | Check | Expected | Status |
|---|---|---|---|
| 5.1 | Schedule, kill the process in Task Manager, relaunch | Schedule restored with an InfoBar | ✅ Auto (Phase 5) |
| 5.2 | Saved target already passed (< 12 h) | Countdown window on launch | ✅ Auto (Phase 5) |
| 5.3 | Saved target more than 12 h old | Discarded with an InfoBar | ✅ Auto (Phase 5) |
| 5.4 | Second launch | Exits; first window comes to the front, even from the tray | ✅ Auto (Phase 5) |
| 5.5 | Kill the app, click Cancel on a leftover toast | App starts, restores, then cancels | ⬜ |
| 5.6 | Sleep the PC through the target, then wake it | Countdown window, no instant shutdown | ⬜ |

## 6. Settings, theme and accessibility
| # | Check | Expected | Status |
|---|---|---|---|
| 6.1 | Gear button in the title bar | Opens Settings; back arrow returns to the form | ✅ Auto (real mouse click) |
| 6.2 | Theme: Light / Dark / Use system setting | Every window, Mica and caption buttons follow; choice survives a restart | ✅ Auto (Light) / ⬜ restart |
| 6.3 | Windows dark ↔ light while the app runs, theme = system | App follows | ⬜ |
| 6.4 | Open (log folder) | Explorer opens `%LOCALAPPDATA%\ScheduleShutdown` | ⬜ |
| 6.5 | Keyboard only: Tab through the form, Space/Enter on each control, schedule and cancel | Everything reachable; focus visible | ⬜ |
| 6.6 | Narrator: controls read sensible names ("Set timer to 15 minutes", "Force shutdown", "Postpone 10 minutes") | Names verified through UIA | ✅ Auto / ⬜ Narrator |
| 6.7 | High contrast theme (Settings → Accessibility → Contrast themes) | Everything readable; no hard-coded colours | ⬜ |
| 6.8 | 150 % scaling, and moving the window between monitors with different scaling | Layout fits; nothing clipped | ⬜ |

## Notes
- **3.7**: toast names and icons are cached per notification identity, which comes from the exe path. The dev build path still shows the old "ScheduleShutdown" from earlier runs. A copy at a new path showed the correct header, and so will the installed app.
- Toast buttons (3.2, 3.3, 5.5) can't be clicked through UI Automation.
