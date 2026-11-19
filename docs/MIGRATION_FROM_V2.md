# Migration from V1/V2 and V3 beta

Install v3 alongside the older program. It uses a distinct install registration and `%LOCALAPPDATA%\HyperBoostX\v3` data root. No old program files are overwritten, no old Flask service is imported and no legacy backup is assumed valid.

The legacy audit found `disable_startup_item` and `enable_startup_item` returning True without a state change in V1. Such records cannot safely become verified v3 restore records. Review old changes in their original tool and verify Windows state directly.

Automatic import of old preferences, profiles, secrets and restore data is not implemented. Only new changes performed by the v3 engine have v3 ownership and rollback guarantees.

## Reviewed behavior reused in v3

- V1 `NetworkService.test_dns` timed a system resolver lookup. V3 provides a user-initiated resolver lookup with measured time, returned addresses, cancellation, and explicit unavailable/error state.
- V1 DNS apply/restore routes did not change adapter DNS; one route stored request metadata only, while the compatibility contract explicitly blocked the operation until adapter-specific restore metadata existed. They are not evidence of working DNS mutation. V3 therefore exposes diagnostics only; provider choice and DNS changes remain unavailable.
- The later V3 beta's `BootMeasurementReader` reads Windows Diagnostics-Performance Event 100. Its read-only boot, main-path, and post-boot duration concept is now used in the v3 Startup scan, with missing/invalid data represented as unavailable.
- V1 `StartupService` inventories Registry entries, startup folders, scheduled tasks, and services, but its estimated impact fields are heuristics and its enable/disable methods return success without changing Windows. V3 now includes read-only WMI inventories for auto-start services and scheduled tasks on Startup, plus the full bounded service/task inventory on the Windows Services and Scheduled Tasks surfaces when providers respond; task trigger attribution and service/task mutation remain unavailable.
- The older Flask backend, generic shell command runner, and beta privileged service are not embedded into the v3 desktop runtime. Each migrated behavior must use the current typed providers and safety/recovery contracts.

The V1/V2.10 action map and later V3 beta feature manifests are discovery material, not proof that all mapped behavior passed implementation, restore, or release gates.
