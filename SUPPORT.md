# HyperBoostX Support

HyperBoostX v1.3.0 is a local Windows optimizer. Support should focus on safety, restore/undo access, installer issues, backend health, GPU detection, and crash diagnostics.

## Recommended First Steps

1. Open HyperBoostX and check the backend status badge.
2. Open Restore & Backup before applying more changes.
3. Export logs or a local crash report if the app shows an error.
4. Include your Windows version, CPU, RAM, and GPU when reporting a problem.

## Bug Report Format

```text
HyperBoostX Bug Report

Version:
Windows:
CPU:
RAM:
GPU:
Issue:
Steps before error:
Screenshot:
Logs if available:
```

## Privacy

Diagnostics stay local unless you manually share them. Local crash reports redact API keys, AI keys, tokens, GitHub tokens, usernames, sensitive local paths, and future license keys.

## Safety Notes

HyperBoostX does not force-disable Defender, does not permanently disable Windows Update, does not disable GPU driver services blindly, and does not overclock, undervolt, or change voltage.

## Release Download

Recommended download: `HyperBoostXInstaller.exe` from the GitHub Release page.

Optional: `SHA256SUMS.txt` for checksum verification.
