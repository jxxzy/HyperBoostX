# SBOM v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

## Summary

This SBOM is a lightweight repo-level inventory for the beta gate. A formal signed release SBOM must be regenerated from final release artifacts.

| Area | Components |
| --- | --- |
| WPF | .NET 8, Newtonsoft.Json, NAudio, OpenCvSharp WPF/runtime |
| Backend | Python, Flask, psutil, local HyperBoostX services |
| Installer | NSIS script and release package folders |
| Tests | pytest, xUnit, .NET test SDK |
| Docs | Markdown release, security, QA, audit, troubleshooting files |

## Stable Requirement

Stable release requires artifact-level SBOM, checksums, signing status, and third-party notices.

