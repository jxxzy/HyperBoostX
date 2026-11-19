# P0 Module Implementation Plan v2.10.0

Release status: HyperBoostX v2.10.0 Owner Lab / Release Candidate

## Current P0 Plan

1. Keep Beginner at exactly 27 pages.
2. Keep Hardware Vendor Center in Advanced/Expert.
3. Keep Expert at 72+ action-map surface with search and maturity labels.
4. Keep dense generated pages locked by source verifiers.
5. Keep AI universal layer locked by 10-model registry, redaction, action mapper, and no-auto-apply gates.
6. Keep GPU/vendor pages vendor-neutral and safety guarded.
7. Keep reports and restore schemas present on mutating flows.
8. Keep installer unsigned and documented until a certificate/PFX exists.

## Gate

Run `scripts/verify_final_stable_release.ps1`. The script now includes UI, AI, Expert, dense content, P0 module, button state, and report/restore gates.

