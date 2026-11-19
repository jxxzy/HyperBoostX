# Defender Scan Guard Design

Generated: 2026-07-01 03:36 +07:00

## Current Scope

Defender Scan Guard is a diagnostics and safety wrapper. It reports Defender status, records performance samples, advises on exclusions, previews exclusion changes, blocks broad exclusions, and exposes undo metadata.

## Explicit Blocks

- Disable Defender.
- Disable real-time protection by force.
- Add broad exclusions such as `C:\`, `C:\Users`, Desktop, Documents, Downloads, AppData, or Temp.
- Hide errors or claim guaranteed FPS/ping improvements.

## Endpoints

- `GET /api/defender/status`
- `POST /api/defender/performance/start`
- `POST /api/defender/performance/stop`
- `GET /api/defender/performance/report`
- `GET /api/defender/exclusions/advice`
- `POST /api/defender/exclusions/preview`
- `POST /api/defender/exclusions/apply`
- `POST /api/defender/exclusions/undo`

