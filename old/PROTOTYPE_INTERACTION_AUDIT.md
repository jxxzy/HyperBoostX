# Prototype Interaction Audit

- Official prototype: https://nexoraid-id.github.io/hyperboostx-v3-prototype/
- Captured: 2026-11-16T15:48:02.663Z
- Browser: Google Chrome via Playwright
- Source SHA-256: `76536fa8ccfc19a6c82ce2ab9fe8f6dd095766241ec3b7bc00adaf6db0d4ea47`
- Navigable routes exercised: 73/73
- Distribution surfaces exercised: 3/3
- Total required surfaces: 76/76
- Onboarding steps exercised: 4/4
- Action review: Review 1 selected categories: Safe Boost
- Command palette query exercised: Network Profiles
- Screenshots: 152 (73 dark, 73 light, 6 state captures)

The 76-surface count is 73 navigation routes plus Splash, Installer Wizard, and Update Wizard. Onboarding, Action Review, and Command Palette are audited interaction states and are not counted again as menu surfaces.

## Live catalog refresh (2026-11-18)

- Re-fetched the deployed `data/catalog.json` and compared every module's capability list with the native catalog. The current source has 73 routes and 376 feature entries; the historical addendum/local snapshot count of 371 is stale by five net entries, and the earlier rendered-page estimate of 372 is not supported by the live source. SHA-256: `d785acfabee0016fb6b714802cdf3737e098375c244334cbecbd47135b73d055`.
- Differences: Audio & DJ Performance replaces `Audio endpoints` with `Advanced Mic Mixer` and `Voicemeeter` (+1 net); Webcam Diagnostics adds `Camera tracking` (+1); Security Health adds `Defender Scan Guard` (+1); Trust Center adds `System Reality Guard` and `Protected Apps` (+2).
- The live catalog declares `demoOnly: true`; its feature cards and demo actions remain reference-only and do not establish Windows behavior. Native UI now labels these capabilities and discloses their unavailable status; no demo operations or sample system state were copied.
