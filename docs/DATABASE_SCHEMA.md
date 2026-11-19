# Database schema 1

Application version, SQLite `user_version`, and action registry version are separate.

| Table | Purpose |
| --- | --- |
| changes | Monotonic sequence, unique change ID, DPAPI-protected typed record |
| events | Append-only state-transition sequence, change ID, status and UTC timestamp |
| settings | Local nonsecret preferences |
| reports | DPAPI-protected diagnostic snapshots; newest 100 retained |

WAL, synchronous FULL, five-second busy timeout and `quick_check` are configured. A schema newer than supported is rejected. Backup uses SQLite's online backup API.

Every action record also exists under Recovery as an encrypted write-through atomic file. Startup reconciles these records into the database. When SQLite reports corruption/not-a-database, the old database and sidecars are preserved with a corruption suffix; action records are rebuilt from the recovery directory. Settings and reports are not falsely reconstructed. Damaged recovery ciphertext fails closed.

No migration of V1/V3 beta state is performed. This is a first-schema implementation; multi-version migration and recovery under actual power loss are not yet qualified.
