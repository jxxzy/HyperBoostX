# HyperBoostX V3 development contract

## Canonical requirements and current source

- `docs/MASTER_DEVELOPMENT_TASK.md` and `docs/MASTER_DEVELOPMENT_TASK_ADDENDUM.md` are the development contract.
- Continue from the existing .NET/WPF native application. Do not reset the repository, discard owner changes, or replace working functionality with a prototype.
- `src/` is the implementation source. `docs/registry/runtime.json` and `docs/registry/legacy-parity.json` are the canonical action and legacy inventories; update them only with evidence from actual handlers and tests.
- All features remain free. Do not introduce Free/Pro/VIP tiers, paywalls, fabricated activation, or subscription locks.

## Implementation requirements

- Implement real behavior, not mock telemetry, fake success, empty handlers, or capability-label changes without providers.
- Give each mutation a stable typed ActionId and route it through the shared safety, preview, approval, backup, apply, verify, evidence, history, and recovery lifecycle.
- Use least privilege and explicit consent. Never add arbitrary shell/registry execution, including through AI, plugins, or IPC.
- Preserve Windows security, boot, login, networking, accessibility, driver, cooling, and recovery controls. Prefer read-only or reversible work when no safe mutation workflow exists.
- Report unsupported hardware, OS, providers, permissions, and external dependencies explicitly. Do not substitute fabricated values.
- Keep credentials in the existing protected store; redact logs and require consent before external data sharing.

## Verification and release policy

- Read this file, both master-task documents, `DEVELOPMENT_PROGRESS.md`, and `RELEASE_STATUS.md` before continuing work. Check `git status` first and preserve unrelated changes.
- Add focused tests for new behavior. Run the smallest relevant test/build, then the documented broader gate when needed. Do not weaken assertions or mark unrun tests as passed.
- Test mutations only against isolated fixtures or a disposable Windows VM. Never run destructive or privileged experiments on the owner's host without explicit authorization.
- Update `DEVELOPMENT_PROGRESS.md`, canonical registries, and user-facing limitation documentation when implementation status changes. Record exact commands and observed results.
- Save a checkpoint before context handoff: current commit/worktree, verified work, open blockers, and the next concrete implementation step.
- A build, navigation smoke, or unsigned package does not establish Stable readiness. Keep `RELEASE_STATUS.md` honest; do not claim Stable until every mandatory gate has evidence.
- Do not publish a release or deploy publicly without the owner's explicit authorization. Local validation builds are permitted. Do not force-push or rewrite history.

## Priority

Continue the highest-priority unfinished requirements in the master task, especially secure least-privilege worker/IPC and verified action/recovery workflows. Do not repeat completed audits instead of implementing work. When an external certificate, hardware, provider, or consent is required, record the precise blocker and continue with independent work.
