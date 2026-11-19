# Local V3 upload intake audit — 2026-11-19

Upload snapshot: 075794fe86583dccad11b8f4e8a47b6c9f3d6ebd (main, old/).
Development baseline: 12416266d60ba5eacd1fd837486c89335a3852a6 (v3-development).

- Root legacy source unchanged by upload outside old/.
- V3 files excluding nested legacy: 175 blobs, 88 match baseline, 55 changed and 32 upload-only. Seven baseline-only files need preservation during integration.
- Removed the redundant nested third-party extracted/rebuilt competitor distribution at old/old/kompetitor/ from the current tree: 978 blobs, 72,703,176 bytes, including extracted binaries, generated build output and a license database. The canonical legacy action map and application source remain intact. This is not a malware verdict or an attribution of wrongdoing.
- Retained the first-party V3 source, tests, source registries, prototype audit reports and legacy reference code. The uploaded settings sample contains appearance/language fields; the local MCP configuration was inspected for common secret-key fields, with no match. This limited check is not a full secret audit.
- Normal commit deletion does not remove earlier public Git objects or copies. Any credential actually exposed must be revoked/rotated; complete history removal requires a separately reviewed process. No history was rewritten.
- Local documentation reports 253 passing tests and 73 route smoke results; referenced artifacts were not present in this upload, so those are historical reported results, not independently reproduced in CI. Reconcile local 376-capability catalog with the canonical acceptance baseline; the historical 371 features remain mandatory. Do not claim Stable.
