# Anti-Regression v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the Stable Unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; external hardware matrix expansion is recommended.

## Protected Baselines

- v1.3.0 is the public stable baseline.
- v1.4.x and v2.0.x are evidence/history for feature parity and preview work.
- No v1.3 feature may disappear from v2 without a documented replacement, limitation, or deliberate safety block.

## Automated Gates

- UI action map density and route coverage: tests/test_ui_action_map_v210.py.
- Runtime route contract: tests/test_runtime_route_contract.py.
- Version/channel beta contract: tests/test_v13_api_contract.py.
- WPF build/test: dotnet build, dotnet test.

## Release Blockers

- Empty menu.
- Decorative button without command/handler/route.
- Dead backend route.
- Unauthorized local session not handled.
- Version mismatch.
- Installer/admin/hardware/signing gates missing for stable.
