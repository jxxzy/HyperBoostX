# Directory Map

## Root

- `README.md` - public overview, install notes, and v2.10.0 stable status.
- `VERSION` - current source/package version.
- `CHANGELOG.md` - release history.
- `CONTRIBUTING.md`, `SECURITY.md`, `PRIVACY.md` - contributor, security, and privacy entry points.
- `HyperBoostX.sln` - main .NET solution.
- `HyperBoostXInstaller.nsi` - NSIS installer source.
- `.github/` - GitHub workflows, issue templates, and PR template.

Generated folders such as `release/`, `artifacts/`, `build_tmp/`, and old `runtime_audit/` are intentionally not part of the clean source root.

## Source

- `app/` - Flask backend, API blueprints, services, data, and local backend tests support.
- `wpf/` - WPF desktop client, views, view models, services, themes, styles, and localization.
- `launcher/` - packaged Windows launcher that starts backend and WPF runtime.
- `website/` - static public website/docs surface.

## Tests

- `tests/` - Python backend and API contract tests.
- `dotnet-tests/HyperBoostX.Tests/` - .NET service, navigation, and view-model tests.

## Docs

- `docs/` - canonical user, API, QA, release, troubleshooting, implementation, and audit docs.
- `docs/release-notes/` - current and historical release notes.
- `docs/release/checksums/` - committed checksum manifests.
- `docs/runtime-audit/` - archived QA/runtime gate evidence.
- `docs/templates/` - issue and feature request templates.
- `docs/audit/`, `docs/roadmap/`, `docs/archive/` - supporting audit history, roadmap notes, and legacy mirrors.

## Scripts

- `scripts/` - current build, QA, verification, runtime, and release scripts.
- `scripts/release/` - owner release helpers such as stable preparation, repair uninstall, and signing.
- `scripts/legacy-batch/` - compatibility batch wrappers for manual local build/start workflows.
