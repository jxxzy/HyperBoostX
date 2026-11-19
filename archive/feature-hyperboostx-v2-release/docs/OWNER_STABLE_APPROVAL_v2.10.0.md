# Owner Stable Approval v2.10.0

Status: `PENDING_OWNER_ADMIN_GATE`

Stable approval is not recorded yet.

Required before approval:

- Owner admin stable gate passes.
- Installed registry `DisplayVersion` is `2.10.0` after stable promotion.
- Installed backend `/api/health` passes on port `5000`.
- Installed backend `/api/version` returns `2.10.0`.
- Desktop and Start Menu shortcuts target the installed launcher.
- WPF installed smoke, token sync, and no-orphan checks pass.
- Silent install, silent uninstall, and silent reinstall pass.
- Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; unsigned distribution must be explicitly accepted with SHA256 verification.

