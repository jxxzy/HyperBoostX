# Stable Release Checklist - HyperBoostX v1.3.0

Stable release target: `HyperBoostX v1.3.0 Stable`

## Must Pass Before Publishing

- [x] Source version synchronized to `1.3.0`.
- [x] Python tests PASS: `43 passed`.
- [x] .NET tests PASS.
- [x] WPF Debug build PASS.
- [x] Support docs, FAQ, roadmap, and crash report redaction tests PASS.
- [x] `scripts\verify_repo.ps1` PASS after final docs/code updates.
- [x] `dotnet restore` PASS.
- [x] `dotnet build` PASS.
- [x] `dotnet build -c Release` PASS.
- [x] WPF Release build PASS.
- [x] Launcher Release build PASS.
- [x] Backend build PASS.
- [x] Release package PASS.
- [x] Installer build PASS.
- [x] Packaged backend health returns `1.3.0`.
- [x] Portable launch smoke PASS.
- [x] Installer install/uninstall/reinstall PASS.
- [x] Secret scan PASS.
- [x] SHA256 checksum generated and verified.
- [ ] Git status clean after commit.
- [ ] Main pushed.
- [ ] Tag `v1.3.0` pushed.
- [ ] GitHub Release created with `HyperBoostXInstaller.exe` and optional `SHA256SUMS.txt`.

## Not Claimed

- Full multi-machine Windows lab matrix is not claimed from this workspace alone.
- Universal hardware compatibility is not claimed beyond tested code paths and recorded lab results.
