# HyperBoostX Security Guide

## Local Backend

The backend binds to `127.0.0.1` by default. Requests must include `X-HyperBoostX-Token`, and CORS is restricted to local hosts.

## Command Execution

System commands are allowlisted in `app\utils\shell.py`. Free-form PowerShell is blocked by default. Admin actions return a clear admin-required result when HyperBoostX is not elevated.

## Secret Storage

NVIDIA API keys and Discord webhook URLs are stored in Windows Credential Manager by `SecureSecretStoreService`. App config marks secret fields with `JsonIgnore`, and tests verify that plaintext API keys are not serialized into app-state.

## Redaction

NVIDIA tokens, bearer tokens, and Discord webhook-like values are redacted before user-facing error output or alert payloads.

## AI Safety

HyperBoostX NVIDIA Copilot creates action plans only. It must not execute system actions directly. Non-scan actions require user approval, and Safety Guard blocks or downgrades unsafe actions such as Defender disablement, permanent Windows Update disablement, driver deletion, arbitrary command execution, and registry/service edits without backup metadata.

## Restore Requirements

Registry edits, power plan changes, service startup changes, startup changes, and network changes must create restore metadata before mutation. Aggressive actions require explicit warning and admin context.

## Known Limitations

Real installer E2E, installed-app launch, and cross-device compatibility require a Windows lab matrix. A real NVIDIA API connection test requires an owner-provided key entered through the UI so the key remains in Windows Credential Manager and does not appear in terminal history.
