# Owner Handoff v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned.

## What Is Ready

- VERSION is `2.10.0`.
- GitHub Release `v2.10.0` is published.
- README points users to v2.10.0 Stable Unsigned.
- UI action map covers 72 menus and 596 active actions.
- Stable action map has 0 partial/roadmap/guidance buttons.
- Runtime verifier now fails if the installed feature registry exposes 0 menus or any count below the release contract.
- Package verifier now requires `wpf\Data\ui_action_map_v2_10.json`.
- Public runtime evidence must pass redaction before commit.
- Code signing remains skipped because no owner certificate/PFX was supplied.

## Owner Responsibilities

1. Keep release artifacts and checksums aligned when rebuilding.
2. Do not call the installer signed until real signing material is supplied and verified.
3. Expand hardware lab coverage beyond this machine over time.
4. Close obsolete draft PR #1 or rebase only the useful patches into small fresh PRs.
5. Keep Safety Guard non-bypassable in Beginner, Advanced, and Expert modes.

## Decision

Stable status: `STABLE_READY_UNSIGNED`.

This is not a signed release. It is a stable unsigned public release with explicit SmartScreen/Unknown Publisher guidance.
