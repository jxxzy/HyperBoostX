# Competitor Reference Audit - Kentang Optimizer Demo

Status: `Owner Lab / Release Candidate`

Reference path: `kompetitor/cek/clean_rebuild`

## Useful Patterns Adopted

- Clear sidebar license state: Free/VIP style status inspired the HyperBoostX License Center device/plan/offline-grace surface.
- Focused feature cards: each menu needs feature-specific evidence, action, result, and restore areas rather than generic cards.
- Check Status / Apply All / Reset style footer: HyperBoostX maps this into action registry, report, restore, and safety controls.
- Preset export/import integrity: HyperBoostX should keep profile exports signed or integrity-checked before importing.
- License server authority: license key + device fingerprint + device limit + signed response belongs server-side, not in client-only logic.

## Patterns Rejected For Safety

- Direct `winget upgrade --all --silent` driver/app updates.
- Direct power plan switch without preview and restore metadata.
- Clear temp / recycle operations without selected-item preview and report.
- Client-side VIP decisions as authority.
- "All features unlocked" copy that could imply safety/restore is paywalled.

## HyperBoostX Direction

HyperBoostX keeps the UX clarity but routes every button through Action Registry metadata, Safety Guard, preview/approval, report output, and restore metadata where supported. License gating cannot block Safety Guard, Restore & Backup, rollback, critical repair, uninstall, or local report export.
