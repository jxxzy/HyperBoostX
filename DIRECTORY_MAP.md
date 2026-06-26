# Directory Map

## Root

- `README.md` - v1.4 product overview and download guidance.
- `VERSION` - current release version.
- `CHANGELOG.md` - release history.
- `RELEASE.md` - release process.
- `RELEASE_NOTES_v1.4.0.md` - v1.4 release notes.
- `QA_RESULTS.md` - command evidence and known blockers.
- `SECURITY.md`, `PRIVACY.md`, `DISCLAIMER.md`, `CONTRIBUTING.md` - trust docs.
- `AUDIT_REPORT.md`, `BUGS_FOUND.md`, `BUGS_FIXED.md` - audit artifacts.
- `HyperBoostXInstaller.nsi` - NSIS installer script.

## Backend

- `app/backend_server.py` - Flask app and blueprint registration.
- `app/api/` - REST blueprints.
- `app/api/product_v14.py` - v1.4 product API contract.
- `app/services/product_features.py` - v1.4 advisor, knowledge base, storage, profiles, protection, benchmark, settings, and roadmap services.
- `app/services/monitoring/` - hardware, GPU, report, crash report, monitor services.
- `app/services/optimization/` - boost, startup, network, job queue, tweak services.
- `app/core/` - config, constants, logging, restore, app state.

## WPF

- `wpf/App.xaml` - global cyber theme/style dictionary registration.
- `wpf/MainWindow.xaml` - cyber shell only: sidebar, topbar, content host, toast, backend pulse.
- `wpf/MainWindow.xaml.cs` - navigation registration, backend status, page transition, settings bootstrap, test compatibility audit.
- `wpf/Themes/` - cyber colors, accent variants, and animation storyboards.
- `wpf/Styles/` - buttons, cards, sidebar, badges, progress rings, toasts, and modal surfaces.
- `wpf/Views/` - dashboard and all routed page views loaded by the shell.
- `wpf/ViewModels/` - shell, dashboard, settings, audit, and feature page view models.
- `wpf/Services/` - backend client, navigation, config, theme/motion, update, localization, Discord, AI services.
- `wpf/localization/` - English and Indonesian localization files.

## Launcher

- `launcher/Program.cs` - starts packaged backend and WPF app.
- `launcher/LauncherRuntimeLayout.cs` - runtime path resolution.

## Tests

- `tests/` - Python backend tests.
- `tests/test_v14_product_features.py` - v1.4 product feature tests.
- `dotnet-tests/HyperBoostX.Tests/` - .NET service and audit tests.

## CI And Release

- `.github/workflows/` - Windows CI, release gate, and lab workflows.
- `.github/ISSUE_TEMPLATE/` - GitHub issue templates.
- `.github/pull_request_template.md` - PR template.
- `scripts/` - verification, release, runtime, and e2e helper scripts.
- `release/`, `artifacts/`, `build_tmp/` - generated release/build output.
