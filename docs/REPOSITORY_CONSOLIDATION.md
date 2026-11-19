# Repository consolidation — 2026-11-19

The active source is the V3 development tree at 26d98366f18a4f3a8aef31e1c695edaa7d55b359, including the legacy reference source under `old/`. The `main` branch is the canonical development branch, not a Stable release. Windows CI now targets main; all existing release gates remain unchanged.

Historical snapshots preserved as commit ancestry:

- Previous main / local intake: 121577d7b85149104014bc602bb2f736b37416b7
- V3 development: 26d98366f18a4f3a8aef31e1c695edaa7d55b359
- V2 release branch: 2140050a006e55927740d65a8fc28c7c1461dec8
- Historical NVIDIA AI audit branch: 0a727e05c17eec0eac0dad5dbd6a51b57894b7a4 (draft PR #1)

The historical NVIDIA AI patch is preserved for review, not claimed as an implemented V3 feature. The previous main tree is preserved in history rather than copied into the active source. No history rewrite or release deletion is intended. Branch deletion is a separate operation after verification of ancestry.

See RELEASE_STATUS.md and DEVELOPMENT_PROGRESS.md for current limitations; this consolidation does not constitute new Windows test evidence.
