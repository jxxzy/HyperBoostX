# Stable Release Checklist

Target release:
- `HyperBoostX v1.1.0`

Current source line:
- `v1.1.0-beta`

Use this checklist after beta QA is complete and before flipping the project to stable metadata.

## 1. Beta Exit Criteria

- [ ] `QA_CHECKLIST.md` has been executed on at least one clean Windows machine.
- [ ] No high-severity installer, startup, or crash-loop issue remains open.
- [ ] No high-severity automation, AI, restore, or admin-required flow issue remains open.
- [ ] Discord reporting and logs show no repeating failure pattern.
- [ ] No blocking localization or layout issue remains in core navigation and settings.

## 2. Version Flip

- [ ] Update `wpf/HyperBoostX.csproj` from `1.1.0-beta` to `1.1.0`.
- [ ] Update `launcher/HyperBoostLauncher.csproj` from `1.1.0-beta` to `1.1.0`.
- [ ] Update backend version strings from `1.1.0-beta` to `1.1.0` in:
  - `app/__init__.py`
  - `app/core/config.py`
  - `app/api/health.py`
  - `app/dev_client.py`
- [ ] Update installer `DisplayVersion` from `1.1.0-beta` to `1.1.0`.
- [ ] Update About App text from `1.1.0 Beta` to `1.1.0`.
- [ ] Update release-checker metadata in `wpf/MainWindow.xaml.cs` and `wpf/Services/AppUpdateService.cs`.
- [ ] Optional: run `prepare_stable_release_final.ps1 -WhatIfOnly` first, then `prepare_stable_release_final.ps1` after QA sign-off.

## 3. Documentation

- [ ] Move stable highlights into `CHANGELOG.md`.
- [ ] Finalize `release-notes-v1.1.0.txt`.
- [ ] Update `README.md` from beta wording to stable wording.
- [ ] Remove or revise beta-only caution text that is no longer valid.

## 4. Build And Packaging

- [ ] Run clean WPF release build.
- [ ] Run launcher publish.
- [ ] Run backend PyInstaller build.
- [ ] Rebuild `release/package` and `release/app`.
- [ ] Rebuild `HyperBoostXInstaller.exe`.
- [ ] Recreate stable portable and package zip assets.

## 5. Stable Verification

- [ ] Install stable build on a clean machine.
- [ ] Upgrade from `v1.1.0-beta` to stable and verify runtime replacement.
- [ ] Verify launch, exit, uninstall, and reinstall behavior.
- [ ] Verify config migration from beta to stable.
- [ ] Verify no beta label remains in app UI, metadata, or installer info.

## 6. GitHub Publish

- [ ] Commit final stable metadata.
- [ ] Push `main`.
- [ ] Create tag `v1.1.0`.
- [ ] Publish GitHub Release `v1.1.0`.
- [ ] Upload installer, portable zip, and package zip.
- [ ] Verify release body and assets are correct.

## Result

- [ ] Ready to publish stable
- [ ] Hold stable release and continue beta fixes
