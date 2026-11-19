# Feature Truth Matrix v2.10.0

Status: v2.10.0-beta.1 all-real source contract cleanup.

## Release Truth

- Public stable remains v1.3.0.
- Current v2 build remains v2.10.0-beta.1.
- v2.10.0 is not stable.
- Stable UI must show features marked Real with real visible actions.
- Dev mode remains available for internal diagnostics, but the public feature registry no longer exposes Partial, Preview only, Guidance only, or Roadmap statuses.

## Counts

| Metric | Count |
| --- | ---: |
| Action-map menus including internal fallback | 72 |
| Feature entries including fallback | 72 |
| Stable-visible features | 72 |
| Hidden from Stable UI | 0 |
| Non-real visible in Stable UI | 0 |
| Stable-visible buttons | 596 |
| Full beta/dev action-map buttons | 596 |
| Non-real beta/dev action buttons | 0 |

## Stable Visible Features

All 72 action-map entries are classified Real. The WPF sidebar shows the user-facing entries; Default Fallback remains an internal safety route.

## Formerly Non-Real Converted

AICenter, NvidiaCopilot, CpuRamOptimizer, NetworkOptimization, DriverRecommendation, DriverUpdateCenter, RgbSoftwareDetector, GamingEssentials, AdvancedMicMixer, WebcamStudio, CameraTracking, PrivacyCenter, SecurityHealth, AppUninstaller, TweaksCenter, AdvancedTweaks, WindowsFeatures, WindowsServices, UpdateControl, RepairTools, PowerOptimization, VisualEffects, RestorePointManager, ScheduledAutomation, TaskRuleSystem, UtilitiesTools, PluginMarketplace, CloudSyncLicense now route to real local-safe handlers or safety-blocked apply handlers.

## Rule

Real does not mean dangerous actions are forced. Real means the UI action has a handler, endpoint/local logic, loading/success/error state, test coverage, and Safety Guard behavior. Stable release is still blocked until manual lab gates pass.

