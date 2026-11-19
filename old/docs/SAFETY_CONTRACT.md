# Safety contract

Registered mutations are limited to four fixed current-user preference IDs, current-user Run entry disable and selecting an existing Windows power plan. Registry paths are not input parameters. Power GUIDs must be enumerated on the PC. Startup names and commands are screened for security, driver, anti-cheat and hardware-control indicators.

Normal mutations are blocked on unsupported Windows builds, Safe Mode, RDP, detected domain management and detected MDM enrollment. These are conservative checks, not a complete enterprise policy detector. Rollback remains available when its adapter/target checks pass.

No process termination, user-file cleanup, service disabling, Defender/firewall disabling, BIOS/firmware access, voltage changes, overclocking, arbitrary scripts, network resets or unsolicited camera/microphone capture are implemented.

The diagnostic tools list is fixed. Handoffs launch a known Windows binary or settings URI; they do not claim verification of work performed externally.

SafePaths rejects UNC/device paths, alternate streams, traversal outside managed roots, and existing reparse points in path ancestry. Atomic recovery files use exclusive creation, write-through, flush and rename. The same-user process/filesystem race threat is not eliminated by these checks; a future hardened broker needs handle-based validation.
