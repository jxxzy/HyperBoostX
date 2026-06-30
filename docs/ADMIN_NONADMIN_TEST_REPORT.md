# Admin / Non-Admin Test Report

Audit date: 2026-06-27

Current shell token: Medium Mandatory Level. User belongs to Administrators but is not elevated in this session.

| Action | Non-admin behavior | Admin behavior | Status | Notes |
| --- | --- | --- | --- | --- |
| Launch/source backend | Health works | Not required | PASS | Live backend smoke passed. |
| Preview safe boost | Works with valid token | Works | PASS | `/api/boost/plan`. |
| Apply without approval/token | Blocked | Blocked until token/approval | PASS | Missing token 401. |
| Repair SFC/DISM | Preview only/admin warning | Needs lab | PARTIAL | No elevated run in this pass. |
| Service mutation | Blocked/guarded | Needs lab | PARTIAL | Protected services not changed. |
| DNS/network reset | Preview/admin warning | Needs lab | PARTIAL | Adapter rollback pending. |
| Installer install/uninstall | Not executed | Needs admin | BLOCKED | Owner must run installer. |
| System restore point | Not executed | Needs admin | BLOCKED | Hardware/admin lab required. |

Result: non-admin preview/safety behavior is PASS; admin apply/rollback lab is BLOCKED by non-elevated session.

