# Stable Release Checklist - HyperBoostX

Current public stable baseline: `v1.3.0`
Current v2.10 candidate: `2.10.0-beta.1`
Automated status: `BETA_READY`
Stable status: `NO-GO` until owner/manual blockers close.

## Completed Automated Gates

- [x] Version synchronized to `2.10.0-beta.1`.
- [x] Windows file version synchronized to `2.10.0.0`.
- [x] Backend feature registry audit endpoints added.
- [x] WPF action map generated with 72 menus and 596 active actions.
- [x] Partial/roadmap/guidance action-map count reduced to 0.
- [x] Backend route contract passed.
- [x] WPF button handler verifier passed.
- [x] Placeholder/fake UI guard passed.
- [x] WPF UI/UX quality verifier passed.
- [x] Real usability verifier passed.
- [x] Python tests passed: 72.
- [x] .NET tests passed: 38.
- [x] Debug build passed.
- [x] Release build/test passed.
- [x] PowerShell syntax scan passed.
- [x] Secret scan passed.
- [x] Release package contents verified.
- [x] Installer rebuilt.
- [x] SHA256 manifests regenerated.
- [x] Root folder audit completed.
- [x] Code signing status documented as `SKIPPED_BY_OWNER_NO_CERT`.

## Required Before Public Stable

- [ ] Fresh install with rebuilt `HyperBoostXInstaller.exe`.
- [ ] Silent reinstall with rebuilt installer.
- [ ] Silent uninstall with rebuilt installer.
- [ ] Installed runtime verifier pass.
- [ ] Backend health from installed runtime pass.
- [ ] WPF connected-to-installed-backend smoke pass.
- [ ] Token sync installed-runtime smoke pass.
- [ ] No orphan process installed-runtime smoke pass.
- [ ] Admin apply/rollback lab pass.
- [ ] Hardware matrix pass for NVIDIA, AMD, Intel, no GPU, and low-end profiles.
- [ ] Windows scaling/manual UI smoke at 100%, 125%, and 150%.
- [ ] Owner approves unsigned distribution or provides certificate/PFX.
- [ ] Signed artifact verification pass, or unsigned release notes/checksum approval if no cert is available.
- [ ] Owner approves stable promotion.
- [ ] Stable tag created.
- [ ] GitHub Release created and artifacts attached.

## Stable Rule

Do not rename `2.10.0-beta.1` to public stable and do not create a stable tag until every required item above is complete or explicitly waived by the owner in release notes.
