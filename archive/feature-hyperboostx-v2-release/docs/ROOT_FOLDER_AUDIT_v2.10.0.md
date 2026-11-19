# HyperBoostX Root Folder Audit v2.10.0

Date: 2026-06-28
Status: `2.10.0-beta.1` source/test stable candidate, not owner-promoted stable.

## Cleanup Result

| Item | Result | Reason |
| --- | --- | --- |
| `FEATURE_MATRIX_UPDATED.md` | Removed | Stale duplicate still referenced `2.0.1`; canonical files are `FEATURE_MATRIX.md` and `docs/FEATURE_TRUTH_MATRIX_v2.10.0.md`. |
| Installer artifacts | Kept | `HyperBoostXInstaller.exe`, `SHA256SUMS*.txt`, and NSIS script are used by release verification. |
| Root release/docs files | Kept | Existing public docs and release notes are intentionally accessible from repo root. |
| Build output folders | Kept/ignored | `build_tmp/`, `artifacts/`, `release/`, and executable outputs are ignored by `.gitignore`; no source cleanup required. |

## Top-Level Folder Check

| Folder | Status | Action |
| --- | --- | --- |
| `.github/` | Keep | CI/workflow metadata. |
| `app/` | Keep | Flask backend and Python services. |
| `artifacts/` | Generated/ignored | Local packaging evidence; do not commit by default. |
| `build_tmp/` | Generated/ignored | .NET build output; safe to regenerate. |
| `docs/` | Keep | Audit, release, API, QA, and security evidence. |
| `dotnet-tests/` | Keep | WPF/.NET contract and view-model tests. |
| `launcher/` | Keep | Windows launcher project. |
| `release/` | Generated/ignored | Local release/package staging. |
| `runtime_audit/` | Generated evidence | Runtime verification output; keep until owner archive. |
| `scripts/` | Keep | Build, QA, release, and verification scripts. |
| `tests/` | Keep | Python backend and contract tests. |
| `website/` | Keep | Public website/static docs surface. |
| `wpf/` | Keep | WPF desktop client. |

## Root File Check

| File | Status | Action |
| --- | --- | --- |
| `.gitignore` | Keep | Build outputs, venv, artifacts, release folders, and executables are ignored. |
| `API_REFERENCE.md` | Keep | Public API docs mirror. |
| `APP_GATE_CHECKLIST.md` | Keep | Release gate checklist. |
| `AUDIT_REPORT.md` | Keep | Root audit summary mirror. |
| `BENCHMARK_DATABASE_ROADMAP.md` | Keep | Roadmap/reference only. |
| `BUG_REPORT_TEMPLATE.md` | Keep | Issue template. |
| `BUGS_FIXED.md` | Keep | Fix history. |
| `BUGS_FOUND.md` | Keep | Known issue/audit history. |
| `BUILD.md` | Keep | Build instructions. |
| `build_backend.bat` | Keep | Backend build helper. |
| `build_installer.bat` | Keep | NSIS installer helper. |
| `build_launcher.bat` | Keep | Launcher build helper. |
| `build_release.bat` | Keep | Release build helper. |
| `CHANGELOG.md` | Keep | Public changelog. |
| `COMPETITOR_COMPARISON.md` | Keep | Competitor audit summary. |
| `CONTRIBUTING.md` | Keep | Contributor guide. |
| `DIRECTORY_MAP.md` | Keep | Repository layout reference. |
| `DISCLAIMER.md` | Keep | Safety/legal disclaimer. |
| `FAQ.md` | Keep | User-facing FAQ. |
| `FEATURE_MATRIX.md` | Keep | Canonical public feature matrix. |
| `FEATURE_REQUEST_TEMPLATE.md` | Keep | Feature request template. |
| `HyperBoostX.sln` | Keep | Main .NET solution. |
| `HyperBoostXInstaller.exe` | Generated/ignored | Local installer artifact used by verification. |
| `HyperBoostXInstaller.nsi` | Keep | Installer source. |
| `IMPLEMENTATION_STATUS.md` | Keep | Implementation status mirror. |
| `INSTALL.md` | Keep | Install instructions. |
| `package_release.bat` | Keep | Packaging helper. |
| `prepare_stable_release_final.ps1` | Keep | Stable promotion guard; owner approval required. |
| `PRIVACY.md` | Keep | Privacy policy. |
| `pytest.ini` | Keep | Python test config. |
| `QA_CHECKLIST.md` | Keep | Manual QA checklist. |
| `QA_RESULTS.md` | Keep | Root QA result summary. |
| `README.md` | Keep | Public entry point; public stable remains v1.3.0. |
| `RELEASE.md` | Keep | Release process summary. |
| `RELEASE_NOTES_NEXT.md` | Keep | Next release notes. |
| `RELEASE_NOTES_v1.3.0.md` | Keep | Stable release notes. |
| `RELEASE_NOTES_v1.4.0.md` | Keep | Historical/pre-release notes. |
| `RELEASE_NOTES_v2.0.0.md` | Keep | Historical/pre-release notes. |
| `RELEASE_NOTES_v2.10.0-beta.1.md` | Keep | Current beta/stable-candidate notes. |
| `repair_uninstall.ps1` | Keep | Hardened uninstall repair helper. |
| `ROADMAP.md` | Keep | Roadmap. |
| `SBOM_v2.10.0.md` | Keep | Generated dependency/release evidence. |
| `SECURITY.md` | Keep | Security model and vulnerability reporting. |
| `SHA256SUMS.txt` | Keep | Current local checksum manifest. |
| `SHA256SUMS_v2.10.0-beta.1.txt` | Keep | Versioned checksum manifest. |
| `sign_release.ps1` | Keep | Signing helper. |
| `STABLE_RELEASE_CHECKLIST.md` | Keep | Stable release checklist. |
| `start_backend.bat` | Keep | Local backend start helper. |
| `start_wpf_client.bat` | Keep | Local WPF start helper. |
| `SUPPORT.md` | Keep | Support guide. |
| `THIRD_PARTY_NOTICES.md` | Keep | Generated third-party notices. |
| `TROUBLESHOOTING.md` | Keep | User troubleshooting guide. |
| `USER_GUIDE.md` | Keep | User guide. |
| `VERSION` | Keep | Source/package version marker. |
| `WEBSITE_BLUEPRINT.md` | Keep | Website planning document. |

## Follow-Up Rule

Do not delete root release artifacts until checksum/package verification has been archived. Generated folders remain ignored so normal build/test cleanup can happen without changing source files.
