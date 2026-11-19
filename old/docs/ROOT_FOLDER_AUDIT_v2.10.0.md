# HyperBoostX Root Folder Audit v2.10.0

Date: 2026-07-01
Status: `2.10.0` stable unsigned source layout cleanup.

## Cleanup Result

| Item | Result | Reason |
| --- | --- | --- |
| Root docs overflow | Moved | Detailed docs now live under `docs/`, `docs/release-notes/`, `docs/audit/`, `docs/roadmap/`, and `docs/templates/`. |
| Release notes | Moved | Current and historical notes now live under `docs/release-notes/`. |
| Checksum manifests | Moved | Source-controlled checksum files now live under `docs/release/checksums/`. |
| Runtime evidence | Moved | QA/runtime reports now live under `docs/runtime-audit/`. |
| Release helpers | Moved | Owner release scripts now live under `scripts/release/`; batch helpers live under `scripts/legacy-batch/`. |
| Generated folders | Removed locally | `release/`, `artifacts/`, `build_tmp/`, and root `runtime_audit/` are generated output and are ignored. |

## Clean Root Contract

The root folder should contain only project entry points and source directories:

- `.github/`
- `app/`
- `docs/`
- `dotnet-tests/`
- `launcher/`
- `scripts/`
- `tests/`
- `website/`
- `wpf/`
- `.gitignore`
- `CHANGELOG.md`
- `CONTRIBUTING.md`
- `HyperBoostX.sln`
- `HyperBoostXInstaller.nsi`
- `PRIVACY.md`
- `pytest.ini`
- `README.md`
- `SECURITY.md`
- `VERSION`

## Generated Output Rule

Build and packaging commands may recreate `release/`, `artifacts/`, `build_tmp/`, local installer `.exe`, `.pdb`, logs, and Python/.NET cache folders. These remain ignored and should not be committed unless explicitly converted into documented release evidence under `docs/`.
