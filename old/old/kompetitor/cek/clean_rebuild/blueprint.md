# Kentang Optimizer Clean Rebuild Blueprint

Source EXE inspected:

- `H:\MOD\cek\KentangOptimizerV9.2 (1).exe`
- SHA256: `D2E226F4C82BC2E38200B59FD17D7E5431434D59D61D85CC04C090DC05BBDEC7`
- Packaging: Nuitka onefile, Python 3.11 runtime, Tk/Tcl UI, Win32 extensions
- Payload extracted statically to `extracted_blueprint/payload_133200_plain`
- Primary payload: `KentangOptimizerV9.dll`

## Product Identity

- App name: Kentang Optimizer
- Publisher text: Prosnatics
- Window title: Kentang Optimizer by Prosnatics
- Old app size hint: `1020x680`
- Languages: ID and EN
- Sidebar state: Free or VIP Aktif

## UI Navigation

Observed sidebar groups:

- Advanced
- Tools
- Troubleshoot

Observed navigation items:

- Performance Boost
- RAM Flush
- Process Flush
- Remove Bloat
- Process Mgr
- Presets
- VIP Profiles
- VIP Presets
- VIP Game Detect
- Troubleshoot

Observed key controls/text:

- Auto-start
- Auto RAM
- Ping Test
- Full Fix
- Game Mode
- Apply
- Apply All
- Check Status
- Reset All Tweaks
- Restart PC
- Unlock VIP
- Enter VIP
- Order VIP
- Later
- Join Discord
- Logout Key
- Run as Administrator first.
- No tweaks selected.
- done, restart PC
- preview mode
- view VIP tweaks, apply needs key

## VIP Feature Prompts

Observed VIP preview cards:

- Game Mode: enable gaming mode in 1 click
- FPS Boost Pro: GPU+CPU+RAM tweaks for gaming
- Hardware Monitor: monitor GPU/CPU/RAM live
- Remove Bloat: clean bloat apps unlimited
- Driver Update: auto update drivers via winget
- Tweak Advanced: 300+ CPU/GPU/RAM tweaks
- Auto RAM: auto trim RAM every 2 hours

Observed upgrade/free-limit prompts:

- All VIP features are unlocked. Check and Apply as needed.
- Auto RAM without time limit
- Runs every 2 hours automatically
- Auto Re-kill Process FREE udah jalan 30 menit.
- Upgrade VIP untuk Auto Re-kill tanpa batas.
- Semua fitur VIP terbuka

## Old License Flow Indicators

Old flow had client-visible indicators for:

- `kentang_keys.json`
- `key_hash`
- `user_hwid`
- `max_devices`
- `tier`
- `free`
- saved key
- Gist/raw URL endpoint
- label JSON endpoint
- Apps Script/HWID registration URL
- Discord webhook notifications
- client-side VIP/FREE UI branching

All old URL/token/webhook material must be treated as compromised and rotated.

## New License Architecture

The clean rebuild uses a new backend authority:

- Client sends entered key and local HWID to backend.
- Backend hashes keys server-side and stores device slots in SQLite.
- Backend returns tier, label, max devices, devices used, and feature gates.
- Client caches only the last key for convenience, not as proof of entitlement.
- Client asks backend again for status on startup.
- Logout deregisters the device slot server-side.

Security posture:

- No Gist/JSON as license authority.
- No Discord webhook as source of truth.
- No client-side "invalid becomes valid" decision.
- Legacy endpoints are not reused.
- Old EXE should be considered permanently compromised.

