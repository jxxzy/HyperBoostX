# HyperBoostX WPF Client

HyperBoostX WPF is the .NET 8 desktop shell for the local-first HyperBoostX backend. The running app now uses the cyber UI directly: `MainWindow.xaml` is only a shell, `App.xaml` merges the global cyber dictionaries, and page content is loaded from `wpf/Views/*`.

## Current UI

- Cyber shell: `MainWindow.xaml` + `MainWindow.xaml.cs`
- Global theme: `Themes/CyberTheme.xaml`, `Themes/AccentColors.xaml`, `Themes/Animations.xaml`
- Global styles: `Styles/Buttons.xaml`, `Styles/Cards.xaml`, `Styles/Sidebar.xaml`, `Styles/Badges.xaml`, `Styles/ProgressRings.xaml`, `Styles/Toasts.xaml`, `Styles/Modals.xaml`
- Dashboard: `Views/DashboardView.xaml` with PC Health, Gaming, Streaming, Startup, Network, Safety, CPU/RAM/GPU/VRAM/Storage/Network/Power/Restore/backend cards
- Page navigation: sidebar routes load real `UserControl` views from `Views/`
- Settings persistence: `%LocalAppData%\HyperBoost X\config\ui_settings.json`

## Screenshots

Release screenshots captured from the real WPF app:

- `docs/screenshots/wpf-cyber-dashboard.png` - dashboard hero, score cards, backend pulse
- `docs/screenshots/wpf-cyber-settings.png` - motion, accent, Beginner/Advanced/Expert Preview settings
- `docs/screenshots/wpf-cyber-feature-audit.png` - read-only audit page with Safety Guard indicators

## Architecture

```text
Launcher
  -> generates local session token when configured
  -> starts local backend on 127.0.0.1
  -> starts WPF runtime

WPF cyber shell
  -> App.xaml global theme/style dictionaries
  -> MainWindow shell sidebar/topbar/content host/toast/status
  -> Views/*.xaml page controls
  -> ViewModels/*.cs page state
  -> Services/HyperBoostBackendClient.cs localhost REST client

Python Flask backend
  -> /api/health, /api/version
  -> safe scan/plan/report/restore/product APIs
  -> mutating routes require X-HyperBoostX-Session when token is present
```

## Implemented Views

- Dashboard
- AI Performance Advisor
- Auto Gaming Mode
- Game Library
- Game Profiles
- GPU Center
- HyperBalance
- One Click Boost
- Process Analyzer
- Startup Manager
- Cleanup
- Network Tools
- Benchmark Lab
- Performance History
- Performance Report
- Streaming Center
- Creator Mode
- Gaming Essentials
- Restore & Backup
- Protected Apps
- Knowledge Base
- Settings
- Feature Audit
- About

## Safety UI Rules

- The UI must not expose Defender-disable, permanent Windows Update disable, anti-cheat changes, driver service disabling, overclocking, undervolting, voltage tuning, BIOS edits, destructive cleanup, or arbitrary AI shell execution.
- One Click Boost is plan-first. It may generate a safe plan, but apply still requires review, Safety Guard, approval, and restore metadata where supported.
- Backend offline is not fatal. The shell shows Offline and keeps navigation usable.
- Unknown GPU telemetry falls back to safe vendor guidance instead of crashing.

## Settings

The Settings page exposes:

- Enable Animations
- Reduce Motion
- Accent color: Cyan, Purple, Green, Blue, Matrix Green, Red Alert, OLED Dark
- Beginner, Advanced, Expert Preview

Settings are saved to `ui_settings.json` under LocalAppData. Corrupt settings files are copied aside and regenerated with safe defaults.

## Build

```powershell
dotnet restore ..\HyperBoostX.sln
dotnet build ..\HyperBoostX.sln -v minimal
dotnet build ..\HyperBoostX.sln -c Release -v minimal
dotnet test ..\dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
```

## Run Locally

Recommended path is through the launcher so backend lifecycle and session-token behavior match the packaged app:

```powershell
..\launcher\bin\Debug\net8.0-windows\win-x64\HyperBoostLauncher.exe
```

For UI-only development, the WPF client can run directly. Backend status will show Offline unless `python -m app.backend_server` is already running on `http://127.0.0.1:5000`.

```powershell
dotnet run --project .\HyperBoostX.csproj
```

## Development Notes

- Add new pages as `Views/NewPageView.xaml` plus `Views/NewPageView.xaml.cs`.
- Add page state in `ViewModels/NewPageViewModel.cs`.
- Register navigation in `MainWindow.xaml.cs` through `NavigationService`.
- Keep `MainWindow.xaml` as shell only; do not add full feature screens back into the window.
- Use existing styles and theme resources before adding new local styling.

## Version

- Client version: `2.0.0`
- Target framework: `net8.0-windows`
- Backend: local Flask API on `127.0.0.1`
