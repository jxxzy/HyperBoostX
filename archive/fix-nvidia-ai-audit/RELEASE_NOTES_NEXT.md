# HyperBoostX Next Release Notes

## Summary

This release candidate continues the HyperBoostX NVIDIA Copilot migration and safety audit. It focuses on truthful release readiness, restore metadata, and safe optimizer behavior.

## Changed

- Completed user-facing AI wording migration to HyperBoostX NVIDIA Copilot.
- Added owner docs for build, install, security, user guide, and release process.
- Added audit, bug, QA, and next-release reporting files.
- Added strict safety tests for NVIDIA Copilot model registry, secret redaction, Safety Guard blocking, and approval flow.

## Fixed

- Booster profile registry and power-plan actions now record restore metadata before mutation.
- Battery Saver display timeout command is now explicitly allowlisted with a narrow pattern.
- Stale AI provider wording and local machine paths were removed from docs.

## Security

- NVIDIA API key handling remains scoped to Windows Credential Manager.
- App-state serialization excludes NVIDIA API keys and Discord webhook URLs.
- AI plans require approval before non-scan actions.
- Safety Guard blocks unsafe actions such as Defender disablement, permanent Windows Update disablement, driver deletion, and arbitrary command execution.

## Validation

- Targeted Python safety tests: PASS.
- Targeted NVIDIA .NET tests: PASS.
- Full repository verification: PASS.
- Python test suite: PASS.
- .NET restore/build/test: PASS.
- Backend, WPF, launcher, package, and installer builds: PASS.
- Packaged backend health, portable launch, installed launch, and no-orphan smoke: PASS.

## Remaining Risks

- Real NVIDIA API connection requires owner key entered through Settings.
- Installer uninstall/reinstall requires Windows lab QA.
- Signed installer flow remains separate from unsigned manual install flow.
