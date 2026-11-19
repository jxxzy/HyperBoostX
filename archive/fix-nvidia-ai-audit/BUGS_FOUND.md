# Bugs Found

## BUG-HBX-001

Category: Security / Safety
Severity: Major
Area: Booster profile restore metadata
File: `app/services/optimization/booster_service.py`
Line: Profile registry and power-plan action helpers
Description: Booster profiles could write registry values or change power plans without profile-session restore metadata.
Impact: Undo/restore could be incomplete after profile actions, especially gaming, streaming, productivity, and battery profiles.
Root Cause: Profile service called `RegistryUtil.set_value` and `ShellUtil.execute_command` directly instead of routing through restore backup helpers.
Fix: Added profile restore point context plus registry and power-plan backup helpers.
Test: `app\venv\Scripts\python.exe -m pytest tests/test_booster_service.py tests/test_shell_util.py -q`
Status: Fixed in source
Notes: Full runtime restore matrix still needs manual Windows QA.

## BUG-HBX-002

Category: Function / Safety Policy
Severity: Medium
Area: Battery Saver profile
File: `app/utils/shell.py`
Line: Shell allowlist
Description: Battery display timeout command used by the built-in profile was not allowlisted.
Impact: Battery Saver could report a failed action even though the command is expected and constrained.
Root Cause: Allowlist contained `powercfg /setactive` but not the safe `powercfg /change monitor-timeout-dc` command used by the profile.
Fix: Added a strict allowlist pattern for `powercfg /change monitor-timeout-dc <number>`.
Test: `tests/test_shell_util.py::test_shell_util_allows_battery_display_timeout_command`
Status: Fixed in source
Notes: Command still requires admin when called by an admin-gated profile.

## BUG-HBX-003

Category: Documentation / Owner Experience
Severity: Low
Area: AI branding
File: `README.md`, `CHANGELOG.md`, `QA_CHECKLIST.md`, `STABLE_RELEASE_CHECKLIST.md`, `release-notes-*`
Line: Multiple historical AI references
Description: Documentation still named the previous AI provider in user-facing release and QA text.
Impact: Owner/user instructions conflicted with the NVIDIA Copilot migration.
Root Cause: Runtime migration happened before historical docs and QA checklist text were fully cleaned.
Fix: Reworded docs to NVIDIA Copilot / NVIDIA credentials.
Test: repository keyword scan for the deprecated AI provider names and config variables
Status: Fixed in source
Notes: Legacy runtime provider is not exposed.

## BUG-HBX-004

Category: Documentation / Release Hygiene
Severity: Low
Area: README local path
File: `README.md`
Line: Release blueprint link
Description: README linked to a local Windows drive path.
Impact: Link breaks outside the owner machine and leaks local workspace shape.
Root Cause: Absolute local path was committed into markdown.
Fix: Changed to a relative repository link.
Test: repository scan for local drive-path URL patterns
Status: Fixed in source
Notes: No remaining local drive path found in audited source/docs.
