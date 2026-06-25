# Bugs Found - HyperBoostX v1.3.0 Audit

## BF-130-001 Version Mismatch

Active source, installer metadata, backend health, README, release guide, QA docs, and gate checklists still referenced `1.2.14` while the target release is `1.3.0`.

Impact: release artifacts and health checks would report the wrong stable version.

Status: fixed.

## BF-130-002 Missing Universal GPU Backend Contract

The tracked backend did not expose the required v1.3.0 hardware/GPU endpoints for NVIDIA, AMD Radeon, Intel, Microsoft Basic Display, and unknown fallback profiles.

Impact: GPU Center and hardware profile workflows could not be validated through API tests.

Status: fixed.

## BF-130-003 Missing Before/After Report Contract

The backend did not expose `/api/reports/latest` or `/api/reports/export`.

Impact: One Click Boost could not produce a stable report schema for UI/export flows.

Status: fixed.

## BF-130-004 Missing Job Queue Contract

Long-running task endpoints were missing.

Impact: cleanup, benchmark, hardware analysis, and repair flows had no progress/cancel contract.

Status: fixed.

## BF-130-005 Mutating Endpoint Session Protection Missing

The backend allowed local POST endpoints without a launcher-provided session header.

Impact: local-only API was weaker than the v1.3.0 security target.

Status: fixed for packaged launcher sessions. Developer mode remains compatible when no token is configured.

## BF-130-006 Unknown GPU Fallback Regression During New Tests

Initial v1.3.0 GPU classifier mapped unknown adapters to `Balanced GPU Mode` instead of `Unknown Safe GPU Mode`.

Impact: unknown hardware fallback was less conservative than required.

Status: fixed and covered by tests.

## BF-130-007 WPF Architecture Still Legacy Large Shell

`MainWindow.xaml.cs` remains a large legacy shell rather than a completed full MVVM split into all requested pages.

Impact: frontend architecture target is only partially satisfied in this run.

Status: known limitation, not claimed complete.

