# Kentang Optimizer Clean Rebuild

This is a clean source rebuild based on a static blueprint from the old EXE. It does not reuse the old Gist/webhook license flow.

## Layout

- `app/` - Tkinter desktop app source.
- `backend/` - stdlib HTTP + SQLite license backend.
- `dist/` - generated EXE output after building.
- `blueprint.md` - static inventory from the old binary.

## Quick Start

Start the license backend:

```powershell
python .\backend\license_server.py --init-demo
```

Run the app:

```powershell
python .\app\kentang_clean.py
```

Demo keys created by `--init-demo`:

- `FREE-DEMO-0000`
- `VIP-DEMO-0000`

Build EXE:

```powershell
powershell -ExecutionPolicy Bypass -File .\build_exe.ps1
```

The EXE will be written to `dist\KentangOptimizerClean.exe`.

## Production Notes

- Set `KENTANG_LICENSE_SECRET` on the backend before creating real keys.
- Serve the backend over HTTPS.
- Rotate all old endpoints, webhooks, Gist tokens, Apps Script URLs, and secrets found in the old binary.
- Keep the client as a UI only; the server must remain the license authority.

