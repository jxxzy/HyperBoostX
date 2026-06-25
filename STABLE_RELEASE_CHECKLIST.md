# Stable Release Checklist

Target release:
- `HyperBoostX v1.2.12`

Current release status:
- `Internal beta / RC`
- Public stable claim is blocked until every required gate below has dated evidence.

## Required Evidence

- [x] Python tests: `app\venv\Scripts\python.exe -m pytest`, 2026-06-25, Windows 10 10.0.26200 x64, `40 passed, 1 warning`.
- [x] WPF and launcher Release build: `dotnet build -c Release`, 2026-06-25, Windows 10 10.0.26200 x64, passed, `0 warnings, 0 errors`.
- [x] .NET tests: `dotnet test`, 2026-06-25, Windows 10 10.0.26200 x64, `20 passed`.
- [x] Repo verification: `scripts\verify_repo.ps1`, 2026-06-25, Windows 10 10.0.26200 x64, passed version sync, Python tests, and .NET desktop tests.
- [x] Backend build: `build_backend.bat`, 2026-06-25, passed, artifact `release\backend\hyperboost_backend.exe`.
- [x] Release/package build: `build_release.bat`, `build_launcher.bat`, `package_release.bat`, 2026-06-25, passed, artifact `release\app\HyperBoostX.exe`.
- [x] Installer build: `build_installer.bat`, 2026-06-25, passed, artifact `HyperBoostXInstaller.exe`.
- [ ] Installer E2E Windows lab: not executed in a separate Windows lab yet.
- [x] Installer hash: SHA256 `c7b30d36c49f206ad6181130d7bcc8adee84624e5f09b5253775681a47800525`, size `145236961` bytes, timestamp `2026-06-25 23:13:56`.
- [x] Packaged backend health: `release\backend\hyperboost_backend.exe`, 2026-06-25, `/api/health` returned HTTP 200 with local backend token.
- [x] Portable runtime launch: `release\app\HyperBoostX.exe`, 2026-06-25, launched WPF window and closed without backend/UI/launcher orphan.
- [x] Installed runtime launch: `C:\Program Files\HyperBoost X\HyperBoostX.exe`, 2026-06-25, launched WPF window and closed without backend/UI/launcher orphan.
- [x] No plaintext secret test: app-state serialization tests pass; local repo scan found no real NVIDIA API key.
- [x] Registry/power-plan revert metadata test: automated unit coverage added for booster profile registry and power-plan backups; full real-machine apply/revert matrix for every tweak is still pending.

## Safety Gates

- [x] High-risk tweaks require Expert Mode.
- [x] High-risk tweaks require Administrator privileges.
- [x] High-risk tweaks require double confirmation.
- [x] High-risk tweaks create a real registry restore backup before mutation.
- [x] One Click Boost does not apply `disable_defender` or `disable_updates`.
- [x] Process-kill flows show a preview before closing apps.
- [x] Backend local API rejects requests without `X-HyperBoostX-Token`.
- [x] Shell command execution is allowlisted and timeout-protected.
- [x] NVIDIA and Discord secrets are stored only in Windows Credential Manager.
- [x] Triple AI Engine runs Scan -> Analyzer -> Safety Guard -> Assistant -> Performance Report with local fallback when AI cloud is unavailable.
- [x] Triple AI Safety Guard blocks overclock, undervolt, Windows Security disable, permanent Windows Update disable, BIOS/UEFI, voltage, irreversible registry edits, and guaranteed FPS claims.
- [x] Triple AI Game Optimizer exposes safe manual NVIDIA/game setting recommendations without official NVIDIA partner/certified branding claims.

## Compatibility Matrix

- [ ] Windows 10, admin.
- [ ] Windows 10, non-admin.
- [ ] Windows 11, admin.
- [ ] Windows 11, non-admin.
- [ ] Laptop.
- [ ] Desktop.
- [ ] SSD system drive.
- [ ] HDD system drive.

## Result

- [ ] Ready to publish stable.
- [x] Hold stable release and continue beta / RC fixes.
