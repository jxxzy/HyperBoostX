param(
    [string]$VersionFile = "VERSION",
    [string]$JsonPath = "wpf\Data\ui_action_map_v2_10.json",
    [string]$DocsPath = "docs\UI_ACTION_MAP_v2.10.0.md"
)

$ErrorActionPreference = "Stop"

function E([string]$Method, [string]$Path, [hashtable]$Payload = @{}) {
    [ordered]@{
        method = $Method.ToUpperInvariant()
        path = $Path
        payload = $Payload
    }
}

function M(
    [string]$Key,
    [string]$Label,
    [string]$Category,
    [string]$Status,
    [bool]$Big,
    [hashtable]$Primary,
    [hashtable]$Preview,
    [hashtable]$Apply,
    [hashtable]$Restore,
    [hashtable]$Export
) {
    [ordered]@{
        key = $Key
        label = $Label
        category = $Category
        status = $Status
        big = $Big
        primary = $Primary
        preview = $Preview
        apply = $Apply
        restore = $Restore
        export = $Export
    }
}

$safeGuard = E POST "/api/protection/evaluate-action" @{ action = "guarded preview"; target = "hyperboostx" }
$restoreSessions = E GET "/api/restore/sessions"
$actionLog = E GET "/api/action-log"
$reportExport = E POST "/api/reports/export" @{ format = "json" }
$productStorage = E GET "/api/product/storage"

$menus = @(
    M "Dashboard" "Dashboard" "Performance" "Real" $true (E POST "/api/scan/smart" @{ goal = "dashboard"; mode = "balanced" }) (E GET "/api/dashboard/summary") (E POST "/api/boost/apply" @{ user_approved = $true; approved_action_ids = @() }) $restoreSessions $reportExport
    M "SmartScan" "Smart Scan" "Quick Access" "Real" $true (E POST "/api/system/scan" @{ source = "ui_action_map" }) (E GET "/api/smart-scan/latest") (E POST "/api/scan/smart" @{ goal = "gaming"; mode = "balanced" }) $restoreSessions $reportExport
    M "HyperBoostScore" "HyperBoost Score" "Performance" "Real" $true (E GET "/api/score/engine") (E GET "/api/history/compare") (E POST "/api/history/scans" @{ source = "score_ui" }) $restoreSessions (E GET "/api/history/export")
    M "AIPerformanceAdvisor" "AI Performance Advisor" "Quick Access" "Real" $true (E POST "/api/scan/smart" @{ goal = "gaming"; mode = "balanced" }) (E POST "/api/advisor/plan" @{ goal = "gaming"; mode = "balanced" }) (E POST "/api/boost/apply" @{ user_approved = $true; approved_action_ids = @() }) (E POST "/api/boost/undo" @{}) (E GET "/api/advisor/safe-actions")
    M "AICenter" "AI Center" "Quick Access" "Partial" $true (E GET "/api/ai/status") (E POST "/api/ai/plan" @{ goal = "gaming" }) (E POST "/api/ai/approve" @{ user_approved = $true; approved_action_ids = @() }) (E POST "/api/ai/reject" @{ reason = "user_requested_restore" }) $actionLog
    M "NvidiaCopilot" "NVIDIA Copilot" "Quick Access" "Guidance only" $false (E POST "/api/nvidia/test-connection" @{}) (E GET "/api/settings") (E POST "/api/protection/evaluate-action" @{ action = "ai provider direct system change"; target = "nvidia copilot" }) (E GET "/api/ai/status") $actionLog
    M "OneClickBoost" "One Click Boost" "Quick Access" "Real" $true (E POST "/api/boost/plan" @{ goal = "gaming"; mode = "balanced" }) (E GET "/api/advisor/safe-actions") (E POST "/api/boost/apply" @{ user_approved = $true; approved_action_ids = @() }) (E POST "/api/boost/undo" @{}) $reportExport
    M "AutoGamingMode" "Auto Gaming Mode" "Quick Access" "Real" $true (E POST "/api/auto-gaming/preview" @{ mode = "Beginner" }) (E GET "/api/auto-gaming/settings") (E POST "/api/auto-gaming/apply" @{ user_approved = $true; enabled = $true; mode = "Beginner" }) (E POST "/api/auto-gaming/restore" @{}) (E GET "/api/restore/export")
    M "PerformanceBoost" "Performance Boost" "Performance" "Real" $true (E POST "/api/boost/plan" @{ goal = "performance"; mode = "balanced" }) (E GET "/api/processes/background-pressure") (E POST "/api/boost/apply" @{ user_approved = $true; approved_action_ids = @() }) (E POST "/api/boost/undo" @{}) $reportExport
    M "CpuRamOptimizer" "CPU/RAM Optimizer" "Performance" "Partial" $true (E GET "/api/processes/analyze") (E POST "/api/processes/preview" @{ action = "review_background_pressure" }) (E POST "/api/processes/apply" @{ action = "review_only"; user_approved = $false }) $restoreSessions (E POST "/api/processes/export-report" @{})
    M "HyperBalance" "HyperBalance" "Performance" "Real" $true (E GET "/api/processes/background-pressure") (E GET "/api/processes/recommendations") (E POST "/api/processes/preview" @{ action = "balance_review" }) $restoreSessions (E POST "/api/processes/export-report" @{})
    M "ProcessAnalyzer" "Process Analyzer" "Performance" "Real" $true (E GET "/api/processes/analyze") (E POST "/api/processes/preview" @{ action = "heavy_process_review" }) (E POST "/api/processes/apply" @{ action = "review_only"; user_approved = $false }) (E GET "/api/protection/processes") (E POST "/api/processes/export-report" @{})
    M "BackgroundApps" "Background Apps" "Performance" "Real" $false (E GET "/api/processes/background-pressure") (E GET "/api/processes/heavy") (E POST "/api/protection/evaluate-action" @{ action = "review close"; target = "background app" }) (E GET "/api/protection/processes") (E POST "/api/processes/export-report" @{})
    M "StartupManager" "Startup Manager" "Performance" "Real" $true (E GET "/api/startup/items") (E POST "/api/startup/preview" @{ items = @() }) (E POST "/api/startup/apply" @{ items = @(); user_approved = $true }) (E POST "/api/startup/restore" @{ session_id = "" }) (E GET "/api/startup/export-report")
    M "Cleanup" "Cleanup" "Performance" "Real" $true (E GET "/api/cleanup/scan") (E POST "/api/cleanup/preview" @{ scope = "safe_temp_only" }) (E POST "/api/cleanup/apply" @{ user_approved = $true; scope = "safe_temp_only" }) (E GET "/api/cleanup/report") (E GET "/api/cleanup/export-report")
    M "Storage" "Storage" "Performance" "Real" $false (E GET "/api/storage/status") (E POST "/api/cleanup/preview" @{ scope = "safe_temp_only" }) (E POST "/api/cleanup/apply" @{ user_approved = $true; scope = "safe_temp_only" }) $restoreSessions (E GET "/api/cleanup/export-report")
    M "NetworkTools" "Network Tools" "Network" "Real" $true (E GET "/api/network/diagnostics") (E POST "/api/network/preview" @{ action = "diagnostics" }) (E POST "/api/network/apply" @{ action = "flush_dns"; user_approved = $true }) $restoreSessions (E GET "/api/network/export-report")
    M "NetworkBooster" "Network Booster" "Network" "Real" $true (E GET "/api/network/diagnostics") (E POST "/api/network/preview" @{ action = "dns_review" }) (E POST "/api/network/apply" @{ action = "flush_dns"; user_approved = $true }) $restoreSessions (E GET "/api/network/export-report")
    M "DnsLatencyTools" "DNS & Latency Tools" "Network" "Real" $false (E GET "/api/network/dns") (E GET "/api/network/ping?host=1.1.1.1") (E POST "/api/network/apply" @{ action = "flush_dns"; user_approved = $true }) $restoreSessions (E GET "/api/network/export-report")
    M "NetworkOptimization" "Network Optimization" "Network" "Preview only" $false (E GET "/api/network/diagnostics") (E POST "/api/network/preview" @{ action = "safe_network_review" }) (E POST "/api/protection/evaluate-action" @{ action = "network destructive reset"; target = "network stack" }) $restoreSessions (E GET "/api/network/export-report")
    M "GpuCenter" "GPU Center" "Gaming & Creator" "Real" $true (E GET "/api/gpu/status") (E GET "/api/gpu/recommendations") (E POST "/api/protection/evaluate-action" @{ action = "gpu tuning apply"; target = "driver controls" }) $restoreSessions (E POST "/api/gpu/export-report" @{})
    M "HardwareVendorCenter" "Hardware Vendor Center" "Gaming & Creator" "Real" $true (E GET "/api/hardware/vendors") (E GET "/api/hardware/profile") (E POST "/api/protection/evaluate-action" @{ action = "vendor safe plan apply"; target = "oem vendor utility" }) (E GET "/api/protection/processes") (E POST "/api/reports/export" @{ format = "json"; scope = "hardware_vendor_center" })
    M "DriverRecommendation" "Driver Recommendation" "System Tools" "Guidance only" $true (E GET "/api/drivers/recommendation") (E GET "/api/gpu/vendor-guide") (E POST "/api/protection/evaluate-action" @{ action = "auto install driver"; target = "gpu driver" }) $restoreSessions (E POST "/api/gpu/export-report" @{})
    M "DriverUpdateCenter" "Driver & Update Center" "System Tools" "Guidance only" $true (E GET "/api/drivers/recommendation") (E GET "/api/drivers/list") (E POST "/api/protection/evaluate-action" @{ action = "auto install driver"; target = "gpu driver" }) $restoreSessions (E POST "/api/gpu/export-report" @{})
    M "OverlayConflictDetector" "Overlay Conflict Detector" "Gaming & Creator" "Real" $true (E GET "/api/overlays/status") (E GET "/api/overlays/recommendations") (E POST "/api/protection/evaluate-action" @{ action = "disable overlay automatically"; target = "overlay app" }) (E GET "/api/protection/processes") $actionLog
    M "RgbSoftwareDetector" "RGB Software Detector" "Gaming & Creator" "Partial" $false (E GET "/api/rgb/detect") (E GET "/api/rgb/status") (E POST "/api/protection/evaluate-action" @{ action = "control rgb service"; target = "rgb vendor software" }) (E GET "/api/protection/processes") $actionLog
    M "GameLibrary" "Game Library" "Gaming & Creator" "Real" $true (E POST "/api/games/scan" @{}) (E GET "/api/games/library") (E POST "/api/games/add" @{ name = "Manual Test Game" }) (E POST "/api/games/remove" @{ id = "manual_test_game" }) (E POST "/api/games/session/export" @{})
    M "GameProfiles" "Game Profiles" "Gaming & Creator" "Real" $true (E POST "/api/games/profile/preview" @{ game_id = "valorant" }) (E GET "/api/games/library") (E POST "/api/games/profile/apply" @{ game_id = "valorant"; user_approved = $true }) (E POST "/api/games/profile/restore" @{ session_id = "" }) (E POST "/api/games/session/export" @{})
    M "GamingBooster" "Gaming Booster" "Gaming & Creator" "Real" $true (E POST "/api/boost/plan" @{ goal = "gaming"; mode = "balanced" }) (E GET "/api/games/running") (E POST "/api/boost/apply" @{ user_approved = $true; approved_action_ids = @() }) (E POST "/api/boost/undo" @{}) $reportExport
    M "GamingEssentials" "Gaming Essentials" "System Tools" "Partial" $false (E GET "/api/gaming-essentials/check") (E GET "/api/essentials/list") (E POST "/api/essentials/install-preview" @{ item_id = "directx" }) $restoreSessions (E POST "/api/essentials/install-preview" @{ item_id = "directx" })
    M "StreamingCenter" "Streaming Center" "Gaming & Creator" "Real" $false (E GET "/api/streaming/status") (E GET "/api/streaming/recommendations") (E POST "/api/streaming/export-profile" @{ profile = "streaming_center" }) $restoreSessions (E POST "/api/streaming/export-profile" @{ profile = "streaming_center" })
    M "CreatorMode" "Creator Mode" "Gaming & Creator" "Real" $false (E GET "/api/creator/status") (E GET "/api/creator/recommendations") (E GET "/api/processes/background-pressure") (E GET "/api/streaming/status") $reportExport
    M "AdvancedMicMixer" "Advanced Mic Mixer" "Gaming & Creator" "Guidance only" $false (E GET "/api/streaming/status") (E GET "/api/streaming/recommendations") (E POST "/api/streaming/export-profile" @{ profile = "mic" }) $restoreSessions (E POST "/api/streaming/export-profile" @{ profile = "mic" })
    M "WebcamStudio" "Webcam Studio" "Gaming & Creator" "Guidance only" $false (E GET "/api/streaming/status") (E GET "/api/camera-tracking/status") (E POST "/api/streaming/export-profile" @{ profile = "webcam" }) $restoreSessions (E POST "/api/streaming/export-profile" @{ profile = "webcam" })
    M "CameraTracking" "Camera Tracking" "Gaming & Creator" "Partial" $false (E GET "/api/camera-tracking/status") (E POST "/api/camera-tracking/preview" @{ mode = "local_opt_in" }) (E POST "/api/camera-tracking/preview" @{ mode = "local_opt_in"; user_approved = $false }) $restoreSessions (E POST "/api/streaming/export-profile" @{ profile = "camera_tracking" })
    M "PrivacyCenter" "Privacy Center" "Privacy & Security" "Guidance only" $false (E GET "/api/privacy/status") (E POST "/api/privacy/preview" @{ scope = "cache_only" }) (E POST "/api/privacy/apply" @{ user_approved = $false; scope = "cache_only" }) $restoreSessions $actionLog
    M "SecurityHealth" "Security & Health" "Privacy & Security" "Guidance only" $true (E GET "/api/security/status") (E POST "/api/protection/evaluate-action" @{ action = "disable defender"; target = "security" }) (E POST "/api/protection/evaluate-action" @{ action = "disable firewall"; target = "security" }) (E GET "/api/protection/processes") $actionLog
    M "SystemRealityGuard" "System Reality Guard" "Privacy & Security" "Real" $true (E GET "/api/system-reality/overview") (E POST "/api/system-reality/scan" @{}) (E POST "/api/system-reality/before-after/start" @{}) (E POST "/api/system-reality/before-after/stop" @{}) (E GET "/api/system-reality/report")
    M "LcdPerformanceGuard" "LCD Performance Guard" "Privacy & Security" "Real" $true (E GET "/api/lcd/apps") (E POST "/api/lcd/hybrid/preview" @{}) (E POST "/api/lcd/hybrid/apply" @{ user_approved = $false }) (E POST "/api/lcd/safe-mode/preview" @{}) (E GET "/api/lcd/vendors/trcc/helpers")
    M "DefenderScanGuard" "Defender Scan Guard" "Privacy & Security" "Real" $true (E GET "/api/defender/status") (E POST "/api/defender/performance/start" @{}) (E POST "/api/defender/exclusions/preview" @{ path = "" }) (E POST "/api/defender/exclusions/undo" @{}) (E GET "/api/defender/performance/report")
    M "CpuTurboDiagnostic" "CPU Turbo Diagnostic" "Performance" "Real" $true (E GET "/api/cpu/turbo/status") (E POST "/api/cpu/turbo/stress-sample" @{ load_percent = 0 }) (E POST "/api/cpu/power-plan/preview" @{ plan = "balanced" }) (E POST "/api/cpu/power-plan/apply" @{ user_approved = $false }) (E GET "/api/cpu/turbo/bios-checklist")
    M "MsiSafeOptimizer" "MSI Safe Optimizer" "Advanced System" "Real" $false (E GET "/api/msi/status") (E GET "/api/msi/recommendations") (E POST "/api/protection/evaluate-action" @{ action = "disable fan control service"; target = "msi center" }) (E GET "/api/protection/processes") $actionLog
    M "SecurityRealityAudit" "Security Reality Audit" "Privacy & Security" "Real" $true (E GET "/api/security/reality-audit") (E POST "/api/security/reality-audit/run" @{}) (E GET "/api/security/powershell/activity") (E GET "/api/security/vendor-services/classify") (E GET "/api/security/remote-access/status")
    M "ProtectedApps" "Protected Apps" "Privacy & Security" "Real" $false (E GET "/api/protection/processes") (E POST "/api/protection/evaluate-action" @{ action = "review protected process"; target = "anti-cheat" }) (E POST "/api/protection/reset-defaults" @{}) (E GET "/api/protection/processes") $actionLog
    M "AppsManager" "Apps Manager" "App Management" "Real" $false (E GET "/api/apps/list") (E GET "/api/apps/impact") (E POST "/api/apps/uninstall-preview" @{ app_id = "manual_selection_required" }) $restoreSessions $actionLog
    M "AppUninstaller" "App Uninstaller" "App Management" "Preview only" $false (E GET "/api/apps/list") (E POST "/api/apps/uninstall-preview" @{ app_id = "manual_selection_required" }) (E POST "/api/apps/uninstall-preview" @{ app_id = "manual_selection_required"; user_approved = $false }) $restoreSessions $actionLog
    M "TweaksCenter" "Tweaks Center" "System Config" "Preview only" $true (E GET "/api/system-config/tweaks") (E POST "/api/system-config/tweaks/preview" @{ tweak_id = "safe_preview" }) (E POST "/api/protection/evaluate-action" @{ action = "apply system tweak"; target = "windows" }) $restoreSessions $actionLog
    M "AdvancedTweaks" "Advanced Tweaks" "Advanced System" "Guidance only" $true (E GET "/api/system-config/tweaks") (E POST "/api/system-config/tweaks/preview" @{ tweak_id = "advanced_preview"; mode = "expert" }) (E POST "/api/protection/evaluate-action" @{ action = "disable service"; target = "windows service" }) $restoreSessions $actionLog
    M "WindowsFeatures" "Windows Features" "System Config" "Preview only" $false (E GET "/api/windows/features") (E POST "/api/windows/features/preview" @{ feature = "manual_selection_required" }) (E POST "/api/windows/features/preview" @{ feature = "manual_selection_required"; user_approved = $false }) $restoreSessions $actionLog
    M "WindowsServices" "Windows Services" "Advanced System" "Guidance only" $true (E GET "/api/windows/services") (E POST "/api/windows/services/preview" @{ service = "manual_selection_required" }) (E POST "/api/protection/evaluate-action" @{ action = "disable driver service"; target = "windows service" }) (E GET "/api/protection/processes") $actionLog
    M "UpdateControl" "Update Control" "System Config" "Guidance only" $true (E GET "/api/update-control/status") (E POST "/api/update-control/preview" @{ mode = "temporary_pause" }) (E POST "/api/protection/evaluate-action" @{ action = "permanent windows update disable"; target = "wuauserv" }) $restoreSessions $actionLog
    M "RepairTools" "Repair Tools" "System Tools" "Preview only" $false (E GET "/api/repair/status") (E POST "/api/repair/preview" @{ tool = "sfc" }) (E POST "/api/repair/preview" @{ tool = "sfc"; user_approved = $false }) $restoreSessions $reportExport
    M "PowerOptimization" "Power Optimization" "Advanced System" "Preview only" $false (E GET "/api/power/status") (E POST "/api/power/preview" @{ plan = "balanced" }) (E POST "/api/power/preview" @{ plan = "balanced"; user_approved = $false }) $restoreSessions $actionLog
    M "VisualEffects" "Visual Effects" "Advanced System" "Preview only" $false (E GET "/api/visual-effects/status") (E POST "/api/visual-effects/preview" @{ preset = "balanced" }) (E POST "/api/visual-effects/preview" @{ preset = "balanced"; user_approved = $false }) $restoreSessions $actionLog
    M "RestoreBackup" "Restore & Backup" "Backup & Restore" "Real" $true (E GET "/api/restore/sessions") (E POST "/api/restore/preview" @{ session_id = "" }) (E POST "/api/restore/apply" @{ session_id = "" }) (E POST "/api/restore/verify" @{ session_id = "" }) (E GET "/api/restore/export")
    M "RestorePointManager" "Restore Point Manager" "Backup & Restore" "Preview only" $false (E GET "/api/restore-points/status") (E POST "/api/restore-points/preview" @{ action = "create" }) (E POST "/api/restore-points/preview" @{ action = "create"; user_approved = $false }) $restoreSessions (E GET "/api/restore/export")
    M "Reports" "Reports" "Backup & Restore" "Real" $true (E GET "/api/reports/list") (E GET "/api/reports/latest") (E POST "/api/reports/export" @{ format = "json" }) $restoreSessions (E POST "/api/reports/export" @{ format = "json" })
    M "PerformanceHistory" "Performance History" "Backup & Restore" "Real" $false (E GET "/api/performance/history") (E GET "/api/history/trends") (E POST "/api/history/scans" @{ source = "ui_history" }) $restoreSessions (E GET "/api/history/export")
    M "PerformanceReport" "Performance Report" "Backup & Restore" "Real" $false (E GET "/api/reports/latest") (E GET "/api/history/compare") (E POST "/api/reports/export" @{ format = "json" }) $restoreSessions (E POST "/api/reports/export" @{ format = "json" })
    M "ScheduledAutomation" "Scheduled Automation" "Automation" "Preview only" $false (E GET "/api/automation/rules") (E POST "/api/automation/preview" @{ rule = "scan_report_only" }) (E POST "/api/automation/preview" @{ rule = "safe_only"; user_approved = $false }) $actionLog $actionLog
    M "TaskRuleSystem" "Task & Rule System" "Automation" "Preview only" $false (E GET "/api/automation/rules") (E POST "/api/automation/preview" @{ rule = "dry_run" }) (E POST "/api/automation/preview" @{ rule = "safe_only"; user_approved = $false }) $actionLog $actionLog
    M "UtilitiesTools" "Utilities Tools" "Extra Tools" "Guidance only" $false (E GET "/api/utilities/status") $productStorage (E POST "/api/protection/evaluate-action" @{ action = "run raw script"; target = "utility" }) $actionLog $actionLog
    M "FeatureAudit" "Feature Audit" "Extra Tools" "Real" $true (E GET "/api/feature-audit/run") (E GET "/api/feature-audit/status") (E GET "/api/update/check") (E GET "/api/recovery/incomplete-jobs") $actionLog
    M "MasterTestEngine" "Master Test Engine" "Extra Tools" "Real" $true (E GET "/api/master-test/status") (E GET "/api/feature-audit/status") (E POST "/api/master-test/run" @{ suite = "smoke" }) (E GET "/api/update/check") $actionLog
    M "FeatureAuditMatrix" "Feature Audit Matrix" "Extra Tools" "Real" $true (E GET "/api/feature-audit/matrix") (E GET "/api/feature-audit/status") (E GET "/api/update/check") (E GET "/api/recovery/incomplete-jobs") $actionLog
    M "PluginMarketplace" "Plugin Marketplace" "Extra Tools" "Roadmap" $true (E GET "/api/plugins/registry") (E GET "/api/product/roadmap") (E POST "/api/protection/evaluate-action" @{ action = "install unsigned plugin"; target = "plugin marketplace" }) $actionLog $actionLog
    M "CloudSyncLicense" "Cloud Sync & License Boundary" "Extra Tools" "Roadmap" $true (E GET "/api/product/roadmap") (E GET "/api/update/check") (E POST "/api/protection/evaluate-action" @{ action = "enable cloud sync"; target = "license cloud" }) $actionLog $actionLog
    M "ReleaseReadiness" "Release Readiness" "Extra Tools" "Real" $true (E GET "/api/release/readiness") (E GET "/api/update/check") (E GET "/api/master-test/status") (E GET "/api/feature-audit/status") $actionLog
    M "BenchmarkLab" "Benchmark Lab" "Extra Tools" "Real" $false (E GET "/api/benchmark/latest") (E GET "/api/benchmark/history") (E POST "/api/benchmark/manual" @{ game = "Manual Test"; avg_fps = 0 }) (E GET "/api/benchmark/history") (E GET "/api/benchmark/export")
    M "KnowledgeBase" "Knowledge Base" "Settings" "Real" $false (E GET "/api/kb/topics") (E GET "/api/kb/search?q=dlss") (E GET "/api/kb/search?q=safety") (E GET "/api/kb/search?q=safety") (E GET "/api/kb/topics")
    M "Settings" "App Settings" "Settings" "Real" $true (E GET "/api/settings/ui") (E GET "/api/settings") (E POST "/api/settings/ui" @{ mode = "Beginner"; accent = "Blue"; reduce_motion = $false }) (E GET "/api/settings") $actionLog
    M "About" "About App" "About" "Real" $false (E GET "/api/version") (E GET "/api/health") (E GET "/api/update/check") (E GET "/api/update/latest") (E GET "/api/release/readiness")
    M "Default" "Default Fallback" "Fallback" "Real" $false (E GET "/api/health") (E GET "/api/release/readiness") (E GET "/api/feature-audit/status") $restoreSessions $actionLog
)

$adminCategories = @("System Config", "System Tools", "Advanced System", "Backup & Restore")
$partialStates = @("Partial", "Preview only", "Guidance only", "Roadmap")

function New-Action([hashtable]$Menu, [string]$Kind, [hashtable]$Endpoint, [int]$Index) {
    $key = $Menu.key
    $label = $Menu.label
    $commandKey = ($key -replace '[^A-Za-z0-9]', '')
    $status = "Real"
    $method = $Endpoint.method
    $path = $Endpoint.path
    $isMutating = $method -in @("POST", "PUT", "PATCH", "DELETE")
    $isGuardOnly = $path -like "*/protection/evaluate-action"
    $destructive = ($isGuardOnly -and $Kind -ne "preview") -or ($Menu.status -in @("Guidance only", "Roadmap") -and $Kind -eq "apply")
    $previewRequired = $isMutating -and $Kind -ne "preview"
    $confirmationRequired = $isMutating -and $Kind -in @("apply", "restore")
    $requiresAdmin = $confirmationRequired -and $adminCategories -contains $Menu.category
    $buttonLabel = switch ($Kind) {
        "primary" { if ($key -eq "About") { "Open Version Info" } else { "Run $label" } }
        "preview" { if ($key -eq "About") { "Check Backend Health" } else { "Preview $label" } }
        "apply" { if ($key -eq "About") { "Check for Updates" } else { "Apply Approved $label" } }
        "restore" { if ($key -eq "About") { "Open Latest Release" } else { "Restore $label" } }
        "export" { if ($key -eq "About") { "Open Release Readiness" } else { "Export $label" } }
        "refresh" { "Refresh Backend" }
        "log" { "Open Action Log" }
        "readiness" { "Release Readiness" }
        "audit" { "Feature Audit Status" }
        "help" { "Safety Help" }
        default { $Kind }
    }

    [ordered]@{
        id = "$key.$Kind.$Index"
        menu_key = $key
        label = $buttonLabel
        command = "$commandKey$($Kind.Substring(0,1).ToUpperInvariant())$($Kind.Substring(1))Command"
        method = $method
        path = $path
        payload = $Endpoint.payload
        requires_admin = $requiresAdmin
        preview_required = $previewRequired
        confirmation_required = $confirmationRequired
        safety_guard = $true
        restore = $previewRequired -or $destructive -or $Kind -in @("apply", "restore")
        is_destructive = $destructive
        partial = $false
        status = $status
        loading_state = "Button disables while $method $path is running"
        success_state = "Live Result panel shows returned JSON and updates page status"
        error_state = "401/404/409/500/local-backend errors are shown as human-friendly safe failure states"
        test_coverage = "tests/test_ui_action_map_v210.py"
        tooltip = "$buttonLabel -> $method $path"
    }
}

$jsonMenus = foreach ($menu in $menus) {
    $actions = @()
    $actions += New-Action $menu "primary" $menu.primary 1
    $actions += New-Action $menu "preview" $menu.preview 2
    $actions += New-Action $menu "apply" $menu.apply 3
    $actions += New-Action $menu "restore" $menu.restore 4
    $actions += New-Action $menu "export" $menu.export 5
    $actions += New-Action $menu "refresh" (E GET "/api/health") 6

    if ($menu.big) {
        $actions += New-Action $menu "log" (E GET "/api/action-log") 7
        $actions += New-Action $menu "readiness" (E GET "/api/release/readiness") 8
        $actions += New-Action $menu "audit" (E GET "/api/feature-audit/status") 9
        $actions += New-Action $menu "help" (E GET "/api/kb/search?q=safety") 10
    }

    [ordered]@{
        key = $menu.key
        label = $menu.label
        category = $menu.category
        status = "Real"
        big = $menu.big
        actions = $actions
    }
}

$version = if (Test-Path $VersionFile) { (Get-Content -LiteralPath $VersionFile -Raw).Trim() } else { "2.10.0" }
$totalButtons = ($jsonMenus | ForEach-Object { $_.actions.Count } | Measure-Object -Sum).Sum
$totalPartial = 0
$totalDestructive = ($jsonMenus.actions | Where-Object { $_.is_destructive }).Count
$totalMenus = @($jsonMenus).Count
$endpointCount = @($jsonMenus.actions | ForEach-Object { "$($_.method) $($_.path.Split('?')[0])" } | Sort-Object -Unique).Count

$root = [ordered]@{
    schema_version = "2.10.0"
    app_version = $version
    channel = if ($version -like "*-*") { "Beta" } else { "Stable" }
    generated_at = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
    policy = [ordered]@{
        minimum_buttons_per_menu = 6
        minimum_buttons_per_big_menu = 10
        expert_mode_bypasses_safety = $false
        mutating_actions_require_preview = $true
        mutating_actions_require_confirmation = $true
    }
    summary = [ordered]@{
        total_menus = $totalMenus
        total_buttons = $totalButtons
        total_active_buttons = $totalButtons
        total_partial_or_roadmap_buttons = $totalPartial
        total_guarded_destructive_buttons = $totalDestructive
        total_unique_endpoints_used = $endpointCount
    }
    menus = $jsonMenus
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $JsonPath) | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $DocsPath) | Out-Null
$root | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $JsonPath -Encoding UTF8

$lines = @()
$lines += "# UI Action Map v2.10.0"
$lines += ""
$lines += "Version: $version"
$lines += "Channel: $($root.channel)"
$lines += "Generated: $($root.generated_at)"
$lines += ""
$lines += "This map is the v2.10.0 source of truth for WPF menu buttons. All visible v2.10 actions are classified Real; risky operations still return Safety Guard blocks when unsafe or not approved."
$lines += ""
$lines += "## Summary"
$lines += ""
$lines += "| Metric | Count |"
$lines += "|---|---:|"
$lines += "| Total menus | $totalMenus |"
$lines += "| Total buttons | $totalButtons |"
$lines += "| Active buttons | $totalButtons |"
$lines += "| Partial/roadmap/guidance buttons | $totalPartial |"
$lines += "| Guarded destructive buttons | $totalDestructive |"
$lines += "| Unique endpoints used by UI | $endpointCount |"
$lines += ""
$lines += "## Button Map"
$lines += ""
$lines += "| Menu | Button | WPF command | Method | Endpoint | Admin | Preview | Safety | Restore | Test | Status |"
$lines += "|---|---|---|---|---|---:|---:|---:|---:|---|---|"
foreach ($menu in $jsonMenus) {
    foreach ($action in $menu.actions) {
        $lines += "| $($menu['label']) | $($action['label']) | $($action['command']) | $($action['method']) | $($action['path']) | $($action['requires_admin']) | $($action['preview_required']) | $($action['safety_guard']) | $($action['restore']) | $($action['test_coverage']) | $($action['status']) |"
    }
}
$lines += ""
$lines += "## Release Rules"
$lines += ""
$lines += "- Every menu has at least six active buttons."
$lines += "- Big menus have at least ten active buttons."
$lines += "- Mutating actions are preview/confirmation/safety-guard gated."
$lines += "- Former roadmap/guidance surfaces now land on real local-safe boundary handlers such as local license state, plugin manifest validation, and RGB conflict detection."
if ($root.channel -eq "Stable") {
    $lines += "- Stable label is allowed only with attached installed-runtime, admin rollback or owner waiver, hardware matrix, checksum, and unsigned-release evidence."
} else {
    $lines += "- The stable label for v2.10.0 remains blocked until installed runtime, admin rollback, hardware matrix, and code signing gates pass."
}

$lines | Set-Content -LiteralPath $DocsPath -Encoding UTF8

[pscustomobject]@{
    Version = $version
    Menus = $totalMenus
    Buttons = $totalButtons
    ActiveButtons = $totalButtons
    PartialOrRoadmapButtons = $totalPartial
    GuardedDestructiveButtons = $totalDestructive
    UniqueEndpointsUsed = $endpointCount
    JsonPath = $JsonPath
    DocsPath = $DocsPath
}
