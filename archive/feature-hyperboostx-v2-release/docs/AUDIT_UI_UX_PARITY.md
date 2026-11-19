# UI/UX Parity Audit

Audit date: 2026-06-27

## Current Shell

| Metric | Value |
| --- | ---: |
| Sidebar items | 52 |
| Sidebar groups | 14 |
| WPF route registrations | 55 |
| XAML files audited | 136 |
| Button controls | 41 |
| Checkbox controls | 2 |
| List/table controls | 19 |
| Legacy mappings | 250 |

## Required Groups

Quick Access, Performance, Gaming & Creator, Network, Privacy & Security, App Management, System Config, System Tools, Backup & Restore, Automation, Extra Tools, Settings, About are present. `Advanced System` is also present for expert-oriented controls.

## Gate

`scripts/verify_ui_ux_quality.ps1`: PASS.

## UX Status

- v2 premium shell remains.
- v1.3/v1.4 density is exposed via primary navigation and legacy-safe functional pages.
- Generic Preview/Apply/Undo/Export remains as workflow bar, not the only content.
- Risky categories stay visible with preview/blocked/manual status instead of silent mutation.

