# Release gates

Stable promotion is blocked. An installer is a delivery format, not proof of feature completeness.

Machine-verifiable local evidence: build result, unit/integration TRX, diagnostics summary, UI navigation/screenshots, dependency audit, installer smoke report and artifact checksums.

Mandatory outstanding gates: full legacy capability parity, elevated broker security, real reboot/crash recovery, minimum hardware matrix, 24–72 hour soak, upgrade from old schemas, signed updater rollback, production signature and complete privacy/accessibility qualification.

Feature statuses must reflect behavior, not counts of menus/endpoints. The generated registry distinguishes READ_ONLY_REAL, MUTATING implementation, EXTERNAL_APP_HANDOFF and unimplemented historical entries. No release script changes these statuses to Stable automatically.
