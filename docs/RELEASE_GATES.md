# Release gates

Stable promotion is blocked. An installer is a delivery format, not proof of feature completeness.

Machine-verifiable local evidence: build result, unit/integration TRX, diagnostics summary, UI navigation/screenshots, dependency audit, installer smoke report and artifact checksums.

Mandatory outstanding gates: full legacy capability parity, elevated broker security, real reboot/crash recovery, minimum hardware matrix, 24–72 hour soak, upgrade from old schemas, signed updater rollback, production signature and complete privacy/accessibility qualification.

Feature statuses must reflect behavior, not counts of menus/endpoints. The generated registry distinguishes READ_ONLY_REAL, MUTATING implementation, EXTERNAL_APP_HANDOFF and unimplemented historical entries. No release script changes these statuses to Stable automatically.

Package lifecycle verification now runs in development CI against the actual generated NSIS/portable files and publishes commit-bound evidence, including explicit NOT_RUN for interactive UI. Same-version repair is not cross-version upgrade; Windows Server host evidence is not Win10/11 workstation evidence. Authenticity and signing remain independent gates.
