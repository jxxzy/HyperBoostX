# SBOM v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the Stable Unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; external hardware matrix expansion is recommended.

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
