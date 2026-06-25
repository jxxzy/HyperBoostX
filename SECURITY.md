# HyperBoostX Security Notes

## Local API

The backend is intended for local runtime use on `127.0.0.1`. Release validation checks packaged `/api/health` and confirms version `1.2.13`.

## NVIDIA API Key

Active AI provider: NVIDIA NIM API.

Required runtime defaults:

```env
AI_PROVIDER=nvidia
NVIDIA_API_KEY=
NVIDIA_BASE_URL=https://integrate.api.nvidia.com/v1
NVIDIA_DEFAULT_MODEL=nvidia/nemotron-3-nano-30b-a3b
NVIDIA_FALLBACK_MODEL=nvidia/nvidia-nemotron-nano-9b-v2
AI_MODEL_AUTO_FALLBACK=true
AI_REQUIRE_ACTION_APPROVAL=true
AI_ENABLE_SAFETY_GUARD=true
```

`NVIDIA_API_KEY` must be saved through Windows Credential Manager. It must not be written to plaintext config, logs, app state, crash reports, or release artifacts.

## Safety Guard

NVIDIA Copilot cannot execute system actions directly. The required flow is plan, risk review, user approval, backend execution, and after-action report.

Safety Guard blocks or downgrades destructive actions including forced Defender disablement, permanent Windows Update disablement, system-file deletion, silent Microsoft Store removal, arbitrary PowerShell from AI, driver deletion, boot config changes, personal file deletion, registry edits without backup, and service edits without restore metadata.

## Restore Policy

Registry tweaks, service startup changes, DNS/network changes, startup changes, power-plan changes, visual effects changes, and aggressive profile actions require backup or restore metadata where applicable.
