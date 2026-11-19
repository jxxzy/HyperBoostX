# Release Blueprint

This document consolidates the current product fixes, architecture upgrades, and adaptive optimization roadmap into one release plan.

## Scope

The current release line needs to solve two classes of work:

- short-term stability and release trust issues that already affect real users
- medium-term product upgrades so optimization results feel fair across SSD, HDD, Windows 10, Windows 11, desktop, and laptop devices

## Release Strategy

Recommended split:

- `v1.1.10`
  Focus on hotfixes, audit stability, updater trust, and release consistency.
- `v1.2.0`
  Focus on adaptive optimization, device-aware results, and deeper verification.

## Current Product Findings

### Release and audit findings

- `Feature Audit` fixes in source do not fully match the currently published binary.
- `Utilities Workflow Actions` can fail because utilities history and workflow output have shared UI state.
- `Dashboard Refresh` performance thresholds were too strict for real machines and created false failures.
- Release packaging and GitHub assets must remain checksum-consistent or update verification will fail.

### Updater findings

- Automatic installer execution currently requires:
  - trusted source
  - valid asset name
  - valid file size
  - checksum match
  - signed installer
  - trusted publisher
- This means unsigned releases can still be valid for manual install but are blocked for automatic install.

### Device compatibility findings

- Users with `SSD` system drives can see much larger perceived gains than `HDD` users.
- This behavior applies across `Windows 10`, `Windows 11`, desktops, and laptops.
- Current optimization and result messaging are not adaptive enough to hardware class and bottleneck type.

## `v1.1.10` Hotfix Blueprint

### Goals

- make the released binary match the latest audit fixes
- remove known false-negative audit failures
- keep updater behavior trustworthy
- reduce confusion between auto-install and manual-install readiness

### Features and fixes

#### 1. Feature Audit stabilization

- ship the latest `Utilities Workflow Actions` fix
- ship the relaxed `Dashboard Refresh` threshold for QA Matrix
- ensure `Full` and `Full QA Matrix` can pass on the current reference machine
- reduce shared-state issues in audit probes

#### 2. Release consistency hardening

- ensure source, packaged release, installer, GitHub release assets, and checksums stay in sync
- add a lightweight release verification pass before publish
- verify the active runtime is the intended build, not a stale install

#### 3. App Update clarification

- separate update states into:
  - `Auto install allowed`
  - `Manual install ready`
  - `Blocked`
- keep checksum validation mandatory for trusted manual install
- stop presenting unsigned-but-valid installers as generic hard failure when manual install is still safe
- keep automatic install disabled by default unless signing is available or policy changes intentionally

#### 4. Audit reporting polish

- reduce misleading summaries after successful probe execution
- keep explicit notes when a feature is UI-only, safe-read-only, or action-backed
- prepare for richer statuses in later releases

### Validation target

- `Feature Audit Full`: `0` failures on reference machine
- `Full QA Matrix`: `0` false failures on reference machine
- updater correctly distinguishes manual-ready vs auto-ready installer states

## `v1.2.0` Adaptive Optimization Blueprint

### Goals

- make optimization results device-aware
- make the app honest about expected gains on SSD vs HDD
- improve trust by showing before/after evidence instead of generic score-only claims

### Core system

#### 1. Device Classification Engine

Detect and classify:

- system drive type: `SSD`, `NVMe`, `SATA SSD`, `HDD`
- OS family: `Windows 10`, `Windows 11`, or other supported Windows runtime
- device type: `desktop` or `laptop`
- RAM class: low, moderate, high
- storage pressure
- battery and charger context
- basic thermal/power context when available

Output example:

- `Desktop / Windows 11 / SSD / 16 GB / storage healthy`
- `Laptop / Windows 10 / HDD / 8 GB / storage-bound`

#### 2. Bottleneck Detector

Determine the main performance constraint:

- `storage-bound`
- `memory-bound`
- `cpu-bound`
- `startup-bound`
- `background-load-bound`
- `thermal/power-bound`

This should drive both profile selection and result messaging.

#### 3. Adaptive Profile Engine

Add device-aware and goal-aware optimization profiles:

- `SSD Responsiveness`
- `HDD Survival`
- `Low RAM`
- `Balanced Laptop`
- `Gaming Stability`
- `Office Productivity`
- `Battery Saver`

Profile selection should depend on detected device class and current bottleneck, not just user intent.

#### 4. Goal-Based Optimization

Support intent-first flows:

- `Boot lebih cepat`
- `PC lebih ringan`
- `Gaming lebih stabil`
- `Laptop lebih adem`
- `Battery lebih hemat`
- `System cleanup aman`

#### 5. Honest Result Messaging

Replace generic result claims with device-aware summaries:

- expected gain by category
- clear bottleneck explanation
- explicit note when hardware remains the main limit

Example messaging:

- `SSD detected: startup and app responsiveness changes should be noticeable.`
- `HDD detected: software optimization helps, but storage hardware remains the main limit for load-heavy tasks.`

#### 6. Before/After Verification

Record baseline and post-action comparisons for:

- startup impact
- RAM pressure
- disk activity
- background process pressure
- system responsiveness indicators

The app should distinguish:

- estimated gain
- measured gain

#### 7. HDD and laptop special handling

Add targeted strategies for the machines that currently benefit least:

- `HDD Survival Mode`
- `Low Free Space Critical Mode`
- `Battery-first mode`
- `Thermal-aware laptop mode`
- charger plugged and unplugged behavior

## Supporting UX and trust improvements

### Result presentation

Move away from a single percentage-only improvement story. Prefer:

- `Responsiveness`
- `Boot`
- `Background load`
- `Storage-heavy tasks`
- `Gaming stability`

### Audit status evolution

Future audit statuses should support:

- `PASS`
- `PARTIAL`
- `BLOCKED`
- `FAIL`

This will better distinguish unsupported or protected flows from genuine failures.

### Feature transparency

The app should gradually classify features as:

- `UI summary`
- `safe inspection`
- `action-backed`
- `high-risk / admin-gated`

## Recommended Build Order

### Phase 1

- release `v1.1.10`
- publish audit and updater stabilization
- verify release assets, checksums, and runtime path consistency

### Phase 2

- implement `Device Classification Engine`
- implement `Bottleneck Detector`
- add base adaptive profiles

### Phase 3

- add honest result messaging
- add before/after verification
- add HDD and laptop specific handling

### Phase 4

- promote adaptive optimization work as `v1.2.0`

## Success Criteria

### `v1.1.10`

- latest audit fixes are included in official binaries
- `Full` audit passes on reference machine
- `Full QA Matrix` passes on reference machine
- updater no longer confuses manual-ready unsigned installers with fully auto-installable signed installers

### `v1.2.0`

- app detects SSD vs HDD and device class
- optimization profiles adapt to bottleneck type
- result messaging explains realistic gains
- users on HDD no longer feel the app is underperforming simply because hardware limits are not explained
- release must satisfy the explicit gate in `release-gates/RELEASE_GATE_v1.2.0.md`
