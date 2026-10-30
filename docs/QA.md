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
| 4.9 | Small screen: 1024 × 768 at 125 % (VM) | Whole window on screen | ❌ Window is 575 × 775 px at (160, 160): the Schedule button sits under the taskbar / off screen |

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

## 7. Installer
Build with `dotnet build installer/ScheduleShutdown.Installer -c Release` → `installer/ScheduleShutdown.Installer/bin/Release/ScheduleShutdown-x64.msi`. Installing and uninstalling need a UAC prompt (per-machine install). Launch the installed exe with `SCHEDULE_SHUTDOWN_DRYRUN=1` for any testing.

| # | Check | Expected | Status |
|---|---|---|---|
| 7.1 | Double-click the MSI | Welcome → Destination Folder (`C:\Program Files\Schedule Shutdown\`) → Ready → Finish; no license page; Back works | ✅ Auto |
| 7.2 | After install | 528 files in Program Files; Start-menu "Schedule Shutdown" shortcut; Apps entry "Schedule Shutdown" 1.0.0.0 by Navojith, no Modify button | ✅ Auto |
| 7.3 | "Launch Schedule Shutdown" checkbox on the last page | Ticked by default; starts the app (not elevated) | ✅ checkbox shown / ⬜ launch |
| 7.4 | Start the app from the Start menu | Opens; toasts show "Schedule Shutdown" + icon | ✅ VM (shortcut launch; "still running" toast had name + icon) |
| 7.5 | Uninstall while the app runs (in the tray) | App ended with no "files in use" prompt; files, shortcut and `HKLM\Software\Schedule Shutdown` removed; `%LOCALAPPDATA%\ScheduleShutdown` kept | ✅ Auto (this PC + VM, 7 s) |
| 7.6 | `msiexec /x ScheduleShutdown-x64.msi REMOVEDATA=1` | Also deletes `%LOCALAPPDATA%\ScheduleShutdown` for that user | ✅ Auto |
| 7.7 | Uninstall from Settings → Apps | Same as 7.5 | ✅ VM via the Apps entry's `UninstallString` (`msiexec /x {ProductCode}`) / ⬜ clicking through Settings |
| 7.8 | Install a higher `<Version>` over the old one | Upgrades in place, one Apps entry; running app is ended first | ✅ VM (1.0.0 → 1.0.1 while running: app ended, 1 entry at 1.0.1.0, 528 files, no files-in-use prompt) |
| 7.9 | Install and uninstall on a clean Windows 11 VM / Windows Sandbox | Works with no .NET or Windows App SDK installed | ✅ VM (Win11 Home 26300, no `C:\Program Files\dotnet`) |
| 7.10 | SmartScreen on the unsigned MSI downloaded from the web | "Windows protected your PC" → More info → Run anyway | ⬜ |

## 8. Real shutdown (Phase 8, VirtualBox VM only)
Run in a throwaway VM, never on the dev PC. App started **without** `SCHEDULE_SHUTDOWN_DRYRUN`; title bar has no "Dry run". Blocker = a WinForms window that cancels `FormClosing` when `CloseReason` is `WindowsShutDown` (like an app with unsaved changes).

| # | Check | Expected | Status |
|---|---|---|---|
| 8.1 | 1-min timer, force **off**, Notepad with unsaved text + blocker open | `shutdown.exe /s /t 0` runs on time; Windows shows "Closing 1 app and shutting down … This app is preventing shutdown"; with no answer the shutdown is cancelled and the PC stays on | ✅ VM (run at the target second; event 1074; blocker listed; PC stayed on) |
| 8.2 | Same with force **on** | Countdown says force is on; `shutdown.exe /s /t 0 /f` runs on time; PC powers off despite the blocker | ✅ VM (off within 8 s of the target; orderly shutdown, event 6006) |

## Notes
- **8.1**: Windows 11 Notepad keeps unsaved tabs and closes, so it doesn't block shutdown. Use the blocker window instead. Schedule Shutdown itself exits during a cancelled shutdown (the schedule is already done and its state cleared), so it is no longer in the tray afterwards.
- **3.7**: toast names and icons are cached per notification identity, which comes from the exe path. The dev build path still shows the old "ScheduleShutdown" from earlier runs. A copy at a new path showed the correct header, and so will the installed app.
- Toast buttons (3.2, 3.3, 5.5) can't be clicked through UI Automation.
