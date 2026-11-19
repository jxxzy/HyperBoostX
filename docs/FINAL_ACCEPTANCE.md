# HyperBoostX V3.0.0 — Final acceptance contract

Owner-confirmed targets (2026-11-19). All are mandatory and additive to AGENTS.md, both master-task documents, and the canonical requirement registry. Do not weaken requirements, replace real features with simulation, or infer stable readiness from a passing build. No paid tiers.

| # | Acceptance target | Required evidence |
|---|---|---|
| 1 | V3 source migration | 143 source blobs checked at 82e973f, legacy main preserved |
| 2 | Automated build and tests | Windows CI, locked restore, tests and artifact hashes; 920d6b5 development CI passed |
| 3 | All 73 native menus | Route-specific functional tests and real provider evidence, not navigation only |
| 4 | All 371 prototype subfeatures | Individually mapped commands, handlers, tests, verification and evidence |
| 5 | Reconcile all 606 legacy actions | Canonical legacy IDs, actual safe equivalent or explicitly approved replacement with evidence |
| 6 | Prototype UI/UX parity | Visual review, accessibility, dialogs, control bindings and functional smoke |
| 7 | Real Windows backend and optimization | Direct OS evidence, approved bounded mutations and read-back |
| 8 | Secure privileged worker and IPC | Authenticated allowlist, UAC, least privilege, isolated cross-elevation negative tests and recovery |
| 9 | AI and hardware monitoring | Real providers, consent, hardware sample evidence, explicit unavailable status |
| 10 | Six languages and all features free | Locale parity and translated functional review, no payment or entitlement gates |
| 11 | Security, backup and rollback | Threat model, failure injection, crash/reboot, conflict-aware restoration, protected paths |
| 12 | Windows 10 and Windows 11 | Disposable real Windows client VM or hardware test matrix, not Windows Server alone |
| 13 | Verified final installer and portable | Clean install, upgrade, repair, uninstall, checksums and trusted publisher signatures |
| 14 | All Stable requirements verified | Structural gate plus authentic commit-bound evidence; no fabricated status promotion |
| 15 | Publish V3.0.0 Final Stable | All preceding targets verified, owner release authorization and reviewed signing/release provenance |

Last independently observed successful CI: 920d6b5, run 44454458264, 188/188 passing tests, no compiler warnings/errors, unsigned development packages. This is not full Stable readiness. Later commits require their own test evidence. The registry previously reported 78/4,051 implemented-verified requirements and 3,973 outstanding; no status promotion without actual evidence. Keep development on v3-development; do not overwrite main or existing legacy release assets while gates remain closed.
