# GitHub Release Manual Steps v2.10.0-beta.1

Do not create a public stable release until owner lab gates pass.

## Current Allowed Release

Allowed label after current source/package gates: `SOURCE_BETA_READY`.

## Owner Lab First

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\clean_install_verify.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\runtime_verifier.ps1 -LaunchInstalledApp -StopAfterProbe
```

Review:

- `docs/runtime-audit/clean_install_verify_report.md`
- `docs/runtime-audit/runtime_audit_report.md`
- `docs/runtime-audit/full_qa_summary.md`
- `SHA256SUMS_v2.10.0-beta.1.txt`

## If Owner Lab Passes

1. Commit only reviewed v2.10 changes.
2. Tag beta if desired:

```powershell
git tag v2.10.0-beta.1
```

3. Create a GitHub draft release with:
   - `HyperBoostXInstaller.exe`
   - `SHA256SUMS_v2.10.0-beta.1.txt`
   - `docs/release-notes/RELEASE_NOTES_v2.10.0-beta.1.md`
   - Explicit note: unsigned build, code signing `SKIPPED_BY_OWNER_NO_CERT`

## Public Stable Rule

Do not use `v2.10.0 stable` or `production ready` unless install lab, runtime, hardware matrix, UI scaling, secret scan, checksums, and owner release approval all pass.

