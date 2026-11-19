# CPU Turbo Diagnostic Design

Generated: 2026-07-01 03:36 +07:00

## Current Scope

The CPU Turbo Diagnostic checks base frequency, current frequency, CPU load, power-plan hints, MSI mode hints, thermal throttling indicators, and power-limit indicators. It reports one of:

- `invalid_test`
- `turbo_working`
- `turbo_not_boosting`

## Safety Boundaries

- No BIOS write.
- No overclock.
- No undervolt.
- No voltage or fan curve mutation.
- Expert mode cannot bypass these guards.

## Endpoints

- `GET /api/cpu/turbo/status`
- `POST /api/cpu/turbo/stress-sample`
- `GET /api/cpu/power-plan`
- `POST /api/cpu/power-plan/preview`
- `POST /api/cpu/power-plan/apply`
- `GET /api/cpu/turbo/bios-checklist`

