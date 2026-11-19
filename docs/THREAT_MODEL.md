# Threat model

Assets: original Windows configuration, encrypted rollback data, user files, application integrity and truthful results.

Boundaries: normal-user desktop/CLI; native Windows APIs; local database/recovery directory; external Windows applications; exported reports. There is no remote API, plugin loader or privileged service in this build.

Controls: registered actions, target allowlists, explicit consent, one-use preview tokens, monotonic expiry, mandatory environment guard, cross-process operation lock, parameterized SQL, DPAPI encryption/integrity, owner binding, state drift checks, bounded inventory rows, CSV formula escaping and restricted support exports.

Negative tests cover unknown actions, stale/replayed previews, missing consent, backup failure, failed verification, exception after mutation, external drift, owner mismatch, path escape and DPAPI tamper.

Limitations: arbitrary code already running as the same user can decrypt that user's DPAPI data or directly modify HKCU. Local administrator compromise is outside this application's protection. Native WMI providers may hang beyond an enumeration timeout. Code-signing, broker attack-surface tests, signed updates and third-party hardware-driver qualification remain pending.
