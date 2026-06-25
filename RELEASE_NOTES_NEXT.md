# HyperBoostX v1.2.13 Stable

## Status
Stable release validation target for HyperBoostX v1.2.13.

## Highlights
- Migrated active AI Copilot flow to NVIDIA Copilot.
- Added NVIDIA provider configuration.
- Added 10 selectable NVIDIA model options.
- Added NVIDIA model fallback handling.
- Added NVIDIA connection test flow.
- Improved secret handling with Windows Credential Manager.
- Added/validated AI approval flow before optimization actions.
- Added/validated Safety Guard for risky actions.
- Hardened optimizer safety and restore/undo behavior.
- Cleaned stale legacy AI provider wording from active runtime/docs/UI where applicable.
- Synced release metadata to v1.2.13.
- Added APP_GATE_CHECKLIST.md for final stable gate tracking.
- Updated audit, bug, QA, and release documentation.

## NVIDIA AI Models
1. nvidia/nemotron-3-nano-30b-a3b
2. nvidia/llama-3.3-nemotron-super-49b-v1.5
3. nvidia/nemotron-3-super-120b-a12b
4. nvidia/nemotron-3-ultra-550b-a55b
5. nvidia/llama-3.1-nemotron-ultra-253b-v1
6. nvidia/nvidia-nemotron-nano-9b-v2
7. nvidia/nemotron-mini-4b-instruct
8. nvidia/nemotron-content-safety-reasoning-4b
9. nvidia/llama-3.1-nemoguard-8b-content-safety
10. nvidia/llama-3.1-nemoguard-8b-topic-control

## Safety
- AI cannot execute system actions without user approval.
- High-risk actions are checked by Safety Guard when enabled.
- NVIDIA_API_KEY is not written to plaintext config, logs, state, or crash reports.
- System tweaks require backup/restore metadata where applicable.
- One Click Boost remains safe by default.

## Validation
- Automated validation passed.
- `scripts\verify_repo.ps1`: PASS.
- Python tests: PASS - 24 passed, 0 warnings.
- Python warning-as-error: PASS.
- .NET tests: PASS - 27 passed.
- `dotnet restore`: PASS.
- `dotnet build`: PASS, 0 warnings, 0 errors.
- `dotnet build -c Release`: PASS, 0 warnings, 0 errors.
- WPF build: PASS.
- Launcher build: PASS.
- Backend package: PASS.
- Portable package: PASS.
- Installer build: PASS.
- Packaged backend health: PASS - version 1.2.13.
- Portable launch smoke: PASS.
- Installed app launch: PASS.
- Installer uninstall/reinstall: PASS.
- Real NVIDIA API connection: PASS from Windows Credential Manager.
- NVIDIA default model: PASS - `nvidia/nemotron-3-nano-30b-a3b`.
- NVIDIA fallback model: PASS - `nvidia/nvidia-nemotron-nano-9b-v2`.
- AI approval flow: PASS.
- Safety Guard: PASS.
- Current-machine automated matrix: PASS.
- Full multi-machine Windows lab matrix: NOT CLAIMED.
- Restore/Undo metadata: PASS.
- Secret redaction: PASS.
- SHA256SUMS: PASS.

## Final Quality Statement

Zero known Critical/Major bugs after automated validation.

## Known Limitations
Stable does not mean bug-free forever.
Stable means zero known Critical/Major bugs after current validation.
