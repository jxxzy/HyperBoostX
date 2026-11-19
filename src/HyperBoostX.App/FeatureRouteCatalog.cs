using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HyperBoostX.App;

public sealed record Navigation(
    string Name,
    string Group,
    string Short,
    [property: JsonPropertyName("desc")] string Description,
    string Action,
    string Kind,
    IReadOnlyList<string> Features)
{
    public string FeatureId => $"ui.{FeatureRouteCatalog.Slug(Group)}.{FeatureRouteCatalog.Slug(Name)}";
    public string Aliases => string.Join(' ', new[] { Short, Description, Action }.Concat(Features));
    public string Icon => Name switch
    {
        "Dashboard" => "◈",
        "Smart Scan" => "⌕",
        "One Click Boost" => "✦",
        "HyperBoost Score" => "◉",
        "Activity & Notifications" => "♧",
        "AI Assistant" or "GPU AI Assistant" => "✧",
        "Game Boost Engine" or "Gaming" => "◆",
        "Game Library" or "Per-Game Profiles" => "▣",
        "Benchmark Center" or "FPS & Frame Time" => "↗",
        "Settings" or "Plugin Manager" => "⚙",
        _ => "◇"
    };
    public override string ToString() => Name;
}

public enum RouteSupport
{
    Implemented,
    Limited,
    Unsupported
}

public sealed record NativeRouteBinding(string Surface, RouteSupport Support, string Detail);

public static class FeatureRouteCatalog
{
    private static readonly IReadOnlyList<Navigation> Routes = Load();
    private static readonly IReadOnlyDictionary<string, NativeRouteBinding> Bindings = CreateBindings();

    public static IReadOnlyList<Navigation> All => Routes;

    public static string Slug(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    public static NativeRouteBinding BindingFor(string routeName) =>
        Bindings.TryGetValue(routeName, out var binding)
            ? binding
            : throw new InvalidOperationException($"No native route binding exists for '{routeName}'.");

    private static IReadOnlyList<Navigation> Load()
    {
        var resource = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(".Assets.navigation.json", StringComparison.Ordinal));
        if (resource is null)
            throw new InvalidOperationException("The embedded 73-route navigation catalog is missing.");

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("The embedded navigation catalog could not be opened.");
        var routes = JsonSerializer.Deserialize<Navigation[]>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("The embedded navigation catalog is empty.");
        if (routes.Length != 73 ||
            routes.Any(route => string.IsNullOrWhiteSpace(route.Name) ||
                                string.IsNullOrWhiteSpace(route.Group) ||
                                route.Features is null) ||
            routes.Select(route => route.Name).Distinct(StringComparer.Ordinal).Count() != routes.Length ||
            routes.Select(route => route.FeatureId).Distinct(StringComparer.Ordinal).Count() != routes.Length)
            throw new InvalidOperationException("The reference navigation catalog must contain 73 unique, complete routes.");
        return Array.AsReadOnly(routes);
    }

    private static IReadOnlyDictionary<string, NativeRouteBinding> CreateBindings()
    {
        var bindings = new Dictionary<string, NativeRouteBinding>(StringComparer.Ordinal);

        void Add(RouteSupport support, string surface, string detail, params string[] names)
        {
            foreach (var name in names)
                if (!bindings.TryAdd(name, new(surface, support, detail)))
                    throw new InvalidOperationException($"Duplicate native route binding: {name}.");
        }

        Add(RouteSupport.Implemented, "Dashboard",
            "Live CPU, physical memory, system-volume usage, network and bounded WDDM busiest-observed-engine readings are available on supported providers. Missing or partial GPU data is explicit. The on-demand readiness score displays measured component coverage and timestamp; the OS low-memory signal is distinct from RAM usage. Vendor thermal sensors remain unavailable; storage health is not inferred from capacity.",
            "Dashboard");
        Add(RouteSupport.Implemented, "Smart Scan",
            "Runs six selectable local diagnostic scopes, including bounded driver inventory and documented machine privacy policies. Missing providers stay explicit; no Windows setting is changed by a scan.",
            "Smart Scan");
        Add(RouteSupport.Implemented, "Dashboard",
            "Calculates a timestamped, transparent six-component score from measured CPU and memory headroom, network sampling readiness, Windows GPU/driver readiness, system-volume free-space readiness, and bounded 24-hour System reliability events. Missing providers are excluded and coverage is reported.",
            "HyperBoost Score");
        Add(RouteSupport.Implemented, "Notifications",
            "Shows local notification history. Optional webhook delivery requires an explicit preview and consent.",
            "Activity & Notifications");
        Add(RouteSupport.Implemented, "Game Library",
            "Executable registration, profile CRUD, validated import/export, approved session changes and restore are available.",
            "Game Library");
        Add(RouteSupport.Implemented, "Restore",
            "Managed action history, state verification and conflict-aware rollback are available.",
            "Restore & Rollback");
        Add(RouteSupport.Implemented, "Reports",
            "Saved local reports, export and a redacted support bundle are available.",
            "Reports Center");
        Add(RouteSupport.Implemented, "Knowledge Base",
            "Offline guidance is available; articles do not perform Windows changes.",
            "Knowledge Base");
        Add(RouteSupport.Implemented, "Settings",
            "Local appearance, goal, experience level, sidebar and window preferences are available.",
            "Settings");
        Add(RouteSupport.Implemented, "License Center",
            "Version, local entitlement and release limitations are shown. This build has no paid tiers or activation requirement.",
            "License Center", "About HyperBoostX");

        Add(RouteSupport.Limited, "Optimize",
            "Registered reversible actions include the current-user visual-effects preset and transparency preferences. The broader reference profile/tweak catalog is not treated as implemented.",
            "Safe Tweak Library", "Advanced Tweaks", "Windows Optimization");
        Add(RouteSupport.Limited,"Optimize",
            "Safe Boost composes explicitly selected transparency and game-capture preferences with per-action review, durable recovery, read-back and compensating rollback. Broad presets and measured gains remain unavailable.",
            "One Click Boost");
        Add(RouteSupport.Limited, "Dashboard",
            "Memory Guard provides persisted 70-95% notification thresholds for physical RAM and measured commit usage, independent Windows low-memory signal alerts, and 1-60 minute cooldown controls. It provides local recommendations only; it does not purge memory or terminate processes. Process exclusions and emergency workload handling are not implemented.",
            "Smart Memory Guard");
        Add(RouteSupport.Limited, "Power",
            "Existing Windows power plans can be inspected and selected with backup and verification. Processor, sleep and AC/DC policies are not changed.",
            "Power Optimization", "Laptop & Battery");
        Add(RouteSupport.Limited, "Processes",
            "Process inventory and sampled interval CPU/memory data are available. Priority, affinity and process termination controls are not implemented.",
            "CPU Optimization", "Process & Task Manager");
        Add(RouteSupport.Limited, "Hardware",
            "Available Windows hardware properties are read; temperature, clocks and vendor sensor data remain unavailable.",
            "CPU Turbo Diagnostics", "Hardware Tuner", "Hardware Inventory");
        Add(RouteSupport.Limited, "GPU",
            "GPU identity, driver details and bounded WDDM per-engine/adapter-memory snapshots can be scanned. VRAM capacity, mapped adapter totals and thermal telemetry remain unavailable.",
            "GPU AI Assistant", "GPU Center");
        Add(RouteSupport.Limited, "Gaming",
            "Game Library can launch a registered executable with an approved, reversible profile. Automatic detection, overlays and FPS capture are not implemented.",
            "Game Boost Engine", "Auto Gaming Mode");
        Add(RouteSupport.Limited, "Game Library",
            "Per-game profile CRUD and import/export are available. Automatic rules and lifecycle detection are not implemented.",
            "Performance Profiles", "Per-Game Profiles");
        Add(RouteSupport.Limited, "Benchmark Lab",
            "CPU and storage microbenchmarks are measured. FPS/frametime entries are user-reported; frame capture and stutter correlation are unavailable.",
            "FPS & Frame Time", "Benchmark Center");
        Add(RouteSupport.Limited, "Drivers",
            "Reads Windows-reported driver class, version, provider, signature flag, INF and PnP problem codes. Vendor update comparison, independent signature verification and mutations remain unavailable.",
            "Driver Health");
        Add(RouteSupport.Limited, "Devices",
            "Reads PnP driver and device problem evidence and offers the Windows Device Manager handoff. Device mutations are not implemented.",
            "Device Manager");
        Add(RouteSupport.Limited, "Startup",
            "Registry/folder startup entries, auto-start services, scheduled tasks when the Windows provider is available, selected current-user Run-entry recovery and Diagnostics-Performance boot durations when logged are available. Startup-trigger attribution and impact estimates are not implemented.",
            "Startup Manager");
        Add(RouteSupport.Limited, "Apps",
            "Installed application observations are available. Bulk debloating and package removal are not implemented.",
            "Debloat Center");
        Add(RouteSupport.Limited, "Windows",
            "Windows and update indicators can be inspected. Service/task mutations and update repair are not implemented.",
            "Windows Update");
        Add(RouteSupport.Limited, "Repair",
            "The page provides safe guidance and Windows recovery handoffs; elevated SFC/DISM execution is not implemented.",
            "Repair & Troubleshooting");
        Add(RouteSupport.Limited, "Storage",
            "Volumes and available capacity are inspected. Duplicate and large-file analysis is available on its dedicated route; drive optimization controls are not implemented.",
            "Storage Center");
        Add(RouteSupport.Limited, "Cleanup",
            "Bounded old temporary files can be quarantined and restored. Quarantine does not free disk space and does not cover the preview's broader cleanup scope.",
            "Cleanup Center");
        Add(RouteSupport.Limited, "Network",
            "Interface/IP/DNS configuration can be inspected, with ICMP and system-resolver timing tests. Resolver selection and network profile mutations are not implemented.",
            "Network Center", "DNS & Latency");
        Add(RouteSupport.Limited, "Creator",
            "Windows settings handoffs are available. Encoder analysis, capture and device controls are not implemented.",
            "Streaming Center");
        Add(RouteSupport.Limited, "Creator",
            "Windows audio settings can be opened. Advanced Mic Mixer and Voicemeeter integration are not implemented.",
            "Audio & DJ Performance");
        Add(RouteSupport.Limited, "Creator",
            "Windows camera and microphone settings can be opened. Camera tracking, driver-state analysis and permission auditing are not implemented.",
            "Webcam Diagnostics");
        Add(RouteSupport.Limited, "Security",
            "Local privacy preferences and consent are available. Complete Windows privacy-policy coverage and enterprise policy management are not implemented.",
            "Privacy Center");
        Add(RouteSupport.Implemented, "Security",
            "Reads real Windows Defender, Domain/Private/Public firewall, UAC, Secure Boot, and pending-update/restart indicators through WMI, Windows Firewall policy, and registry providers. Defender Scan Guard is not implemented. The route is intentionally diagnostic and never weakens a protection.",
            "Security Health");
        Add(RouteSupport.Limited, "License Center",
            "Local release information, signed update-package inspection, and fail-closed plugin manifest/trust review are available. Plugins are deliberately never loaded; System Reality Guard, Protected Apps, online update discovery, installation, rollback, revocation feeds, and process isolation remain unavailable.",
            "Trust Center");
        Add(RouteSupport.Limited, "Restore",
            "The local action journal and database backup are available; crash/reboot recovery is limited to persisted managed-action state.",
            "Backup Snapshot Center", "Emergency Recovery");
        Add(RouteSupport.Limited, "Reports",
            "Stored diagnostic and action evidence is available. Research-mode A/B/C/D experiment management is not implemented.",
            "Before / After Report");
        Add(RouteSupport.Limited, "Test Center",
            "Runs safe, read-only application checks and records timestamped results. Elevated mutation tests and unconsented network checks are not run.",
            "Test Center");
        Add(RouteSupport.Limited, "Application Health",
            "Reports actual runtime, route, action registry, database, configuration, recovery and telemetry checks. Privileged worker and signed file-integrity checks are not supported.",
            "Application Health");
        Add(RouteSupport.Limited, "AI Center",
            "An opt-in NVIDIA-compatible provider and offline guidance are implemented. The provider is not granted action execution and requires explicit data-sharing consent.",
            "AI Assistant", "AI Provider Center");
        Add(RouteSupport.Limited, "Devices",
            "Windows device observations are available. Vendor-specific lighting, cooling and optimizer controls are not exposed.",
            "Hardware Vendor Center");
        Add(RouteSupport.Limited, "Notifications",
            "An optional redacted Discord webhook can be stored locally and enabled with consent. It is not a general chat or event-stream integration.",
            "Discord Webhook");

        Add(RouteSupport.Implemented, "Benchmark Lab",
            "Imports bounded frametime CSV captures, calculates average FPS, 1% and 0.1% lows, percentiles and spike counts, and correlates optional CPU, GPU, disk and thermal columns. Imported data is never represented as live capture.",
            "Stutter Analyzer");
        Add(RouteSupport.Implemented, "Gaming",
            "Inventories supported overlay-capable software and measures interval CPU plus working-set memory for running processes. Presence does not prove an overlay is enabled; no application is stopped or reconfigured.",
            "Overlay Conflict Detector");
        Add(RouteSupport.Implemented, "Devices",
            "Enumerates supported RGB controller signatures from running processes, Windows services and installed-application records. It reports possible controller conflicts and never stops or reconfigures vendor software.",
            "RGB Software Detector");
        Add(RouteSupport.Implemented, "Devices",
            "Inventories MSI Center, Mystic Light and Dragon Center signatures across processes, services and installed applications, measures running-process overhead, reports service dependencies, and enforces cooling/fan/pump exclusions. It never stops services or changes hardware.",
            "MSI Safe Optimizer");
        Add(RouteSupport.Implemented, "Startup",
            "Reads up to ten Windows Diagnostics-Performance Event 100 boot records, Event 101–110 startup degradation evidence, and automatic-service dependencies when available. This analysis route does not change boot, service, or task configuration.",
            "Boot Optimizer");
        Add(RouteSupport.Implemented, "Windows",
            "Inventories installed applications from bounded Windows registry providers. It launches a user-selected local executable after explicit confirmation and records process-creation time, GUI input-idle time when exposed, an initial resource sample, and local report history. It never closes or reconfigures the launched application.",
            "App Load Optimizer");
        Add(RouteSupport.Limited, "Windows",
            "Read-only service inventory is available; service status changes and dependency-aware mutations are not implemented.",
            "Windows Services");
        Add(RouteSupport.Limited, "Windows",
            "Read-only scheduled task inventory is available when the Windows provider responds; startup-trigger attribution and task modification are not implemented.",
            "Scheduled Tasks");
        Add(RouteSupport.Implemented, "Storage",
            "Scans a user-selected local folder, groups duplicates by size and SHA-256 content, lists files above a configurable threshold, reports incomplete coverage explicitly, skips reparse points, and never deletes files.",
            "Duplicate & Large Files");
        Add(RouteSupport.Implemented, "Storage",
            "Inventories physical media, volumes, health, TRIM delete-notification policy, and the Windows ScheduledDefrag task using read-only providers. Windows Optimize Drives is available as an explicit external handoff; HyperBoostX does not start maintenance automatically.",
            "Drive Optimization");
        Add(RouteSupport.Implemented, "Network",
            "Previews Balanced or Gaming diagnostics profiles using real adapter, Windows profile, DHCP/DNS policy and optional gateway latency evidence. An optional per-adapter DNS profile uses compiled resolver presets, explicit review and Administrator consent, durable backup, read-back verification and rollback; no QoS, MTU or offload tweak is applied.",
            "Network Profiles");
        Add(RouteSupport.Implemented, "Security",
            "Measures supported LCD/sensor utility process CPU and memory against user-configured thresholds. Gaming-only alert gating is available, and processes that may control cooling, fans, or pumps are explicitly protected; no process is stopped.",
            "LCD Performance Guard");
        Add(RouteSupport.Implemented, "Settings",
            "Provides opt-in in-app schedules for allowlisted read-only Smart Scan, cleanup discovery, and reminders. Rules support bounded intervals, conditions, cooldowns, pause/remove, emergency stop, and encrypted local history; no command, script, or unattended mutation is accepted.",
            "Scheduled Automation", "Task & Rule System");
        Add(RouteSupport.Implemented, "Benchmark Lab",
            "Records repeat A/B/C/D trials with explicit workload, metric, unit and notes in encrypted local storage, then calculates per-variant means and deltas against A without claiming causation or simulating unmeasured hardware.",
            "Research Mode");
        Add(RouteSupport.Implemented, "License Center",
            "Reviews bounded local plugin packages using strict manifests, app-version compatibility, SHA-256, an explicit permission allowlist and Windows Authenticode trust. Plugin execution is fail-closed and disabled; untrusted code is never loaded.",
            "Plugin Manager");
        Add(RouteSupport.Implemented, "License Center",
            "Inspects the current executable or a user-selected local release package using SHA-256, Windows Authenticode trust, signer and version metadata, and retains report history. It never downloads, executes or installs a package automatically.",
            "Update Center");

        var catalogNames = Routes.Select(route => route.Name).ToHashSet(StringComparer.Ordinal);
        if (bindings.Count != Routes.Count || !catalogNames.SetEquals(bindings.Keys))
        {
            var missing = catalogNames.Except(bindings.Keys, StringComparer.Ordinal);
            var extra = bindings.Keys.Except(catalogNames, StringComparer.Ordinal);
            throw new InvalidOperationException(
                $"Native route bindings do not match the catalog. Missing: {string.Join(", ", missing)}. Extra: {string.Join(", ", extra)}.");
        }
        return bindings;
    }
}
