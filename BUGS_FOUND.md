# Bugs Found - HyperBoostX v1.4.0 Audit

## Active Version Drift

Active source and docs still reported `1.3.0` while the requested target was `1.4.0`.

## Missing v1.4 API Surface

The backend did not expose the requested v1.4 endpoints for AI Performance Advisor, Knowledge Base, Performance History, Game Profiles, Overlay Detector, Protected Process List, Benchmark Reports, GPU recommendations, Cleanup, Network diagnostics, Gaming Essentials, Restore sessions, Settings, Feature Audit, and roadmap foundations.

## Product Storage Gaps

Runtime config created only the older config/data/logs/backups folders. v1.4 needed reports, profiles, sessions, and diagnostics folders plus corrupted JSON recovery.

## Roadmap Risk

Some requested additions, such as global similar-hardware benchmark comparison, RGB control, cloud sync, plugin marketplace, and paid licensing, could become fake features if presented as complete without backend/data support.

## Trust Docs Missing

Root privacy, disclaimer, contributing, GitHub issue templates, PR template, and v1.4 API reference were missing or incomplete.

## Legacy WPF UI Still Active

The running WPF client still used the large legacy `MainWindow.xaml`/`MainWindow.xaml.cs` layout, old sidebar structure, and old local color resources. New cyber UI work was not fully integrated into the app shell or installer output.

## Release Blockers

Code signing, GitHub release publishing, installed app smoke, uninstall/reinstall smoke, and multi-machine validation require owner credentials or an interactive release environment.

## Signing Script Parser Error

`sign_release.ps1` placed `$ErrorActionPreference` before `param(...)`, which causes a PowerShell parser error and prevents proper signing readiness checks.
