param()

$ErrorActionPreference = "Stop"

function Escape-Xml([string]$value) {
    return [System.Security.SecurityElement]::Escape($value)
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$viewsDir = Join-Path $repoRoot "wpf\Views"
$viewModelsDir = Join-Path $repoRoot "wpf\ViewModels"

$pages = @'
Key|View|Vm|Title|Subtitle|Workspace|Module1|Module2|Module3|Result|Safety|Rec1|Rec2|Metric1|Metric2
PerformanceBoost|PerformanceBoostView|PerformanceBoostViewModel|Performance Boost|Scan CPU, RAM, disk, startup, and background pressure before a reversible fix plan.|Performance Fix Workspace|CPU and RAM pressure|Startup and disk impact|Approved fix plan|Performance Evidence|Unsafe service, driver, anti-cheat, and shell actions are blocked.|Preview the fix plan before apply.|Use report output for before and after comparison.|Pressure|Restore
StartupManager|StartupManagerView|StartupManagerViewModel|Startup Manager|Inspect startup apps, impact, publisher, path, and selected changes before applying anything.|Startup Impact Workspace|Startup app table|Selected app details|Restore history|Startup Change Evidence|Driver, audio, security, anti-cheat, and required vendor startup entries stay protected.|Load startup apps before selecting changes.|Disable only selected low-risk entries after preview.|Startup|Restore
BackgroundApps|BackgroundAppsView|BackgroundAppsViewModel|Background Apps|Review running and background process pressure with protected-process guidance.|Process Pressure Workspace|Running apps|Protected processes|Safe stop guidance|Process Evidence|System, security, anti-cheat, driver, audio, and vendor utility processes remain protected.|Review high-memory apps manually before gaming.|Browsers are treated as work/browser apps unless selected by the user.|Mode|Protection
Cleanup|CleanupView|CleanupViewModel|Cleanup|Scan safe cleanup categories, protect personal folders, then clean only selected approved items.|Cleanup Scope Workspace|Safe cleanup categories|Personal folder protection|Large file review|Cleanup Evidence|Documents, Downloads, Desktop, game saves, project folders, browser sessions, and unreviewed user-file deletion are blocked.|Preview cleanup scope before deleting anything.|Personal files are excluded from default cleanup.|Scope|Documents
Storage|StorageView|StorageViewModel|Storage|Review drive usage, disk pressure, large files, and cleanup guidance without destructive defaults.|Storage Usage Workspace|Drive usage|Large file review|Storage recommendations|Storage Evidence|Destructive personal-file cleanup and unreviewed duplicate deletion stay blocked.|Use storage scan before cleanup guidance.|Duplicate cleanup remains review-only.|Storage|Cleanup
OneClickBoost|OneClickBoostView|OneClickBoostViewModel|One Click Boost|Create a mode-based boost plan, review approved actions, and keep undo evidence visible.|Boost Plan Workspace|Boost mode selector|Custom checklist|Undo and report|Boost Evidence|Arbitrary AI shell commands, unsafe services, security disables, and protected-process kills are blocked.|Preview boost plan before starting.|Apply only approved actions.|Safety Guard|Undo
AutoGamingMode|AutoGamingModeView|AutoGamingModeViewModel|Auto Gaming Mode|Detect supported games, prepare safe profile metadata, and auto-restore after close.|Gaming Session Workspace|Game detection|Profile preview|Auto restore|Gaming Mode Evidence|Browsers and protected processes are not treated as games by default.|Preview safe actions before enabling automation.|Protected processes stay locked while gaming.|Auto Restore|Game Detection
AIPerformanceAdvisor|AIPerformanceAdvisorView|AIPerformanceAdvisorViewModel|AI Performance Advisor|Local diagnosis for bottlenecks, stutter, overlays, startup, and GPU pressure.|Local Advisor Workspace|Bottleneck diagnosis|Explainable recommendation|Advanced detail|Advisor Evidence|Recommendations are guidance and never guaranteed FPS claims.|Review GPU-bound and CPU-bound guidance separately.|Use raw details only in Advanced or Expert mode.|Diagnosis|Risk
GpuCenter|GpuCenterView|GpuCenterViewModel|GPU Center|Detect GPU and driver context, then provide safe vendor guidance without hardware-risk automation.|GPU Guidance Workspace|Detected GPU|VRAM and temperature|Vendor guidance|GPU Evidence|Overclock, undervolt, BIOS edits, forced driver-service changes, and silent driver installs are blocked.|Use official vendor/OEM sources for driver downloads.|Keep GPU services enabled unless the vendor tool says otherwise.|Detected GPU|Driver
GamingBooster|GamingBoosterView|GamingBoosterViewModel|Gaming Booster|Build an instant gaming plan through safe boost endpoints and real game context.|Gaming Boost Workspace|Detected game|Safe boost plan|Undo route|Gaming Boost Evidence|Gaming boost is blocked when no real game context or approved plan exists.|Do not apply a boost unless a real game is selected or detected.|Chrome and browsers are not games by default.|Plan|Undo
CreatorMode|CreatorModeView|CreatorModeViewModel|Creator Mode|Review RAM, disk, GPU, and background app guidance for editing and rendering.|Creator Readiness Workspace|Render pressure|Scratch disk|Export guidance|Creator Evidence|Aggressive cleanup and app stops stay review-only during render/export sessions.|Keep project files and caches on healthy storage.|Avoid cleanup during render or export sessions.|RAM|Disk
NetworkBooster|NetworkBoosterView|NetworkBoosterViewModel|Network Booster|Run diagnostics, DNS checks, and approval-gated cache actions without fake ping claims.|Network Diagnostics Workspace|Network diagnostics|DNS and latency|Guarded network actions|Network Evidence|Network reset and flush actions require confirmation and human-friendly failure states.|Use DNS and latency tests before changing anything.|No ping-lower guarantee is shown.|Network|Risk
DnsLatencyTools|DnsLatencyToolsView|DnsLatencyToolsViewModel|DNS & Latency Tools|Measure DNS and latency diagnostics, then export evidence-based reports.|DNS Latency Workspace|DNS test|Latency sample|Report export|DNS Evidence|DNS changes require approval; measured latency is never hardcoded.|Run diagnostics to get real local results.|DNS changes stay approval gated.|DNS|Latency
PrivacyCenter|PrivacyCenterView|PrivacyCenterViewModel|Privacy Center|Review privacy controls, browser-session warnings, and personal-folder protection.|Privacy Review Workspace|Windows privacy controls|Sensitive data boundaries|Official settings shortcuts|Privacy Evidence|Personal-folder cleanup and browser session deletion are blocked by default.|Privacy cleanup is not a default performance action.|Reports redact sensitive values.|Privacy|Sessions
SecurityHealth|SecurityHealthView|SecurityHealthViewModel|Security & Health|Collect read-only security evidence for Defender, Firewall, updates, protected apps, and blocked risky tweaks.|Security Evidence Workspace|Security overview|Protection guards|Health report|Security Evidence|Disabling Defender, Firewall, anti-cheat, driver services, or Windows Update is blocked.|HyperBoostX will not disable Defender, Firewall, or anti-cheat.|Admin-required security states are clearly labeled.|Security|Safety
AppsManager|AppsManagerView|AppsManagerViewModel|Apps Manager|Inventory installed and running apps, explain impact, then hand off uninstall safely.|App Inventory Workspace|Installed apps list|Running apps|Selected app detail|Apps Evidence|Protected system apps stay blocked and no app is removed silently.|Scan apps before uninstall guidance.|Uninstall remains confirmation-first.|Apps|Uninstall
TweaksCenter|TweaksCenterView|TweaksCenterViewModel|Tweaks Center|Review allowlisted tweak categories, risk level, and selected tweak explanation before approval.|Safe Tweaks Workspace|Safe tweak filter|Selected tweak explanation|Approval and restore|Tweaks Evidence|Unsafe tweak categories are blocked; Expert detail cannot bypass Safety Guard.|No arbitrary shell command is exposed.|Expert tweaks stay disabled by default.|Tweaks|Apply
WindowsFeatures|WindowsFeaturesView|WindowsFeaturesViewModel|Windows Features|Review optional Windows features, categories, status, admin needs, and restart warnings.|Optional Features Workspace|Feature categories|Feature status|Windows UI handoff|Windows Feature Evidence|Silent enable/disable and hidden component changes are blocked.|Feature changes can require restart.|Final changes stay in Windows UI/admin path.|Features|Admin
UpdateControl|UpdateControlView|UpdateControlViewModel|Update Control|Show Windows Update status, pending updates, active hours, and temporary guidance without permanent disable.|Windows Update Workspace|Update status|Temporary guidance|Windows Update handoff|Update Evidence|Permanently disabling Windows Update, driver service hacks, and security service disables are blocked.|Do not permanently disable Windows Update.|Use Windows Update for final OS-managed decisions.|Updates|Pause
RepairTools|RepairToolsView|RepairToolsViewModel|Repair Tools|Review repair readiness, SFC/DISM modules, admin requirements, and reports before running approved repair.|Repair Modules Workspace|System file repair|Windows repair areas|Admin and time warning|Repair Evidence|Arbitrary repair commands, hidden shell execution, and unreviewed system changes are blocked.|Run repair actions only after reading impact.|Long-running repair actions are never silent.|Repair|Report
DriverUpdateCenter|DriverUpdateCenterView|DriverUpdateCenterViewModel|Driver & Update Center|Inventory device driver status and provide manual OEM/vendor update handoffs without silent installs.|Driver Inventory Workspace|Driver overview|Peripherals|Manual update handoff|Driver Evidence|Overclock, undervolt, BIOS edits, forced driver-service changes, and silent driver installs are blocked.|Use vendor/OEM sources for downloads.|HyperBoostX never disables driver services.|Driver|Install
AppUninstaller|AppUninstallerView|AppUninstallerViewModel|App Uninstaller|Review installed apps, selected app detail, uninstall preview, residual cleanup, and confirmation.|Uninstall Review Workspace|Installed apps list|Selected app detail|Residual cleanup preview|Uninstall Evidence|No silent uninstall. Protected system apps and required utilities stay blocked.|Use Windows Apps Settings for final uninstall when needed.|System apps are protected.|Mode|Safety
RestoreBackup|RestoreBackupView|RestoreBackupViewModel|Restore & Backup|Review restore readiness, applied changes, sessions, evidence, restore points, and rollback timeline.|Restore Sessions Workspace|Restore readiness|Evidence groups|Rollback timeline|Restore Evidence|Restore actions require matching metadata and never fabricate rollback support.|Review restore preview before rollback.|Missing metadata blocks rollback.|Sessions|Integrity
'@ | ConvertFrom-Csv -Delimiter '|'

$template = @'
<views:PlacementActionPageBase x:Class="HyperBoostX.Views.__VIEW__"
                               xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                               xmlns:views="clr-namespace:HyperBoostX.Views"
                               xmlns:vm="clr-namespace:HyperBoostX.ViewModels"
                               AutomationProperties.AutomationId="CoreFeaturePage___KEY__"
                               Tag="CORE_UI:__KEY__">
    <views:PlacementActionPageBase.DataContext>
        <vm:__VM__/>
    </views:PlacementActionPageBase.DataContext>

    <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
        <StackPanel Margin="0,0,18,28">
            <Border Style="{StaticResource CyberHeroCardStyle}" Margin="0,0,0,16">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="260"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel>
                        <TextBlock Text="__TITLE__" FontSize="30" FontWeight="Black" TextWrapping="Wrap"/>
                        <TextBlock Text="__SUBTITLE__" FontSize="13" Foreground="{StaticResource Brush.Accent.Primary}" Margin="0,5,0,0" TextWrapping="Wrap" MaxWidth="930"/>
                        <TextBlock Text="{Binding Purpose}" FontSize="13" Foreground="{StaticResource Brush.Text.Secondary}" Margin="0,12,0,0" TextWrapping="Wrap" MaxWidth="940"/>
                        <Border Style="{StaticResource CyberBadgeStyle}" Margin="0,16,0,0" HorizontalAlignment="Left">
                            <TextBlock Text="Core page marker: __KEY__" Foreground="{StaticResource Brush.Text.Muted}"/>
                        </Border>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="20,0,0,0">
                        <Border Style="{StaticResource SuccessBadgeStyle}" Margin="0,0,0,10">
                            <TextBlock Text="Safety Guard Active" Foreground="{StaticResource Brush.Status.Success}" FontWeight="SemiBold"/>
                        </Border>
                        <Border Style="{StaticResource CyberBadgeStyle}" Margin="0,0,0,10">
                            <TextBlock Text="{Binding LastUpdated, StringFormat=Updated: {0}}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap"/>
                        </Border>
                        <Border Style="{StaticResource CyberBadgeStyle}">
                            <TextBlock Text="{Binding Status, StringFormat=Status: {0}}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap"/>
                        </Border>
                    </StackPanel>
                </Grid>
            </Border>

            <Border Style="{StaticResource CyberCardStyle}" Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="__WORKSPACE__" FontSize="18" FontWeight="Bold"/>
                    <TextBlock Text="This surface is page-specific, evidence-first, and wired to the local backend action map." Foreground="{StaticResource Brush.Text.Muted}" TextWrapping="Wrap" Margin="0,4,0,14"/>
                    <UniformGrid Columns="3">
                        <Border Background="#0F1B2B" BorderBrush="{StaticResource Brush.Border.Subtle}" BorderThickness="1" CornerRadius="{StaticResource CornerRadius.Small}" Padding="13" Margin="0,0,10,0">
                            <StackPanel>
                                <TextBlock Text="__MODULE1__" Foreground="{StaticResource Brush.Accent.Primary}" FontWeight="Bold" TextWrapping="Wrap"/>
                                <TextBlock Text="Uses local evidence before any recommendation is shown." Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,7,0,0"/>
                            </StackPanel>
                        </Border>
                        <Border Background="#0F1B2B" BorderBrush="{StaticResource Brush.Border.Subtle}" BorderThickness="1" CornerRadius="{StaticResource CornerRadius.Small}" Padding="13" Margin="0,0,10,0">
                            <StackPanel>
                                <TextBlock Text="__MODULE2__" Foreground="{StaticResource Brush.Accent.Primary}" FontWeight="Bold" TextWrapping="Wrap"/>
                                <TextBlock Text="Separates readable beginner copy from advanced raw details." Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,7,0,0"/>
                            </StackPanel>
                        </Border>
                        <Border Background="#0F1B2B" BorderBrush="{StaticResource Brush.Border.Subtle}" BorderThickness="1" CornerRadius="{StaticResource CornerRadius.Small}" Padding="13">
                            <StackPanel>
                                <TextBlock Text="__MODULE3__" Foreground="{StaticResource Brush.Accent.Primary}" FontWeight="Bold" TextWrapping="Wrap"/>
                                <TextBlock Text="Keeps preview, approval, report, and restore evidence visible." Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,7,0,0"/>
                            </StackPanel>
                        </Border>
                    </UniformGrid>
                </StackPanel>
            </Border>

            <Grid Margin="0,0,0,16">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="0.95*"/>
                    <ColumnDefinition Width="1.35*"/>
                </Grid.ColumnDefinitions>
                <Border Style="{StaticResource CyberCardStyle}" Margin="0,0,14,0">
                    <StackPanel>
                        <TextBlock Text="Current State" FontSize="17" FontWeight="Bold"/>
                        <TextBlock Text="{Binding EmptyState}" Foreground="{StaticResource Brush.Text.Muted}" TextWrapping="Wrap" Margin="0,5,0,14"/>
                        <ItemsControl ItemsSource="{Binding Metrics}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate><UniformGrid Columns="2"/></ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Background="#0F1B2B" BorderBrush="{StaticResource Brush.Border.Subtle}" BorderThickness="1" CornerRadius="{StaticResource CornerRadius.Small}" Padding="12" Margin="0,0,10,10">
                                        <StackPanel>
                                            <TextBlock Text="{Binding Title}" Foreground="{StaticResource Brush.Text.Muted}" FontSize="11" FontWeight="SemiBold"/>
                                            <TextBlock Text="{Binding Value}" FontSize="20" FontWeight="Black" Margin="0,5,0,2"/>
                                            <TextBlock Text="{Binding Detail}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap"/>
                                        </StackPanel>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
                <Border Grid.Column="1" Style="{StaticResource CyberCardStyle}">
                    <StackPanel>
                        <TextBlock Text="Feature Modules" FontSize="17" FontWeight="Bold"/>
                        <ItemsControl ItemsSource="{Binding PlacementSections}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate><UniformGrid Columns="2"/></ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Background="#0F1B2B" BorderBrush="{StaticResource Brush.Border.Subtle}" BorderThickness="1" CornerRadius="{StaticResource CornerRadius.Small}" Padding="12" Margin="0,10,10,0">
                                        <StackPanel>
                                            <TextBlock Text="{Binding Title}" FontWeight="SemiBold" Foreground="{StaticResource Brush.Accent.Primary}" TextWrapping="Wrap"/>
                                            <TextBlock Text="{Binding Description}" Foreground="{StaticResource Brush.Text.Muted}" TextWrapping="Wrap" Margin="0,4,0,8"/>
                                            <ItemsControl ItemsSource="{Binding Items}">
                                                <ItemsControl.ItemTemplate>
                                                    <DataTemplate>
                                                        <TextBlock Text="{Binding StringFormat=- {0}}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,0,0,5"/>
                                                    </DataTemplate>
                                                </ItemsControl.ItemTemplate>
                                            </ItemsControl>
                                        </StackPanel>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
            </Grid>

            <Border Style="{StaticResource CyberCardStyle}" Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Guided Workflow" FontSize="17" FontWeight="Bold"/>
                    <TextBlock Text="Start with scan or preview. Apply-style actions stay confirmation-gated by Safety Guard and local session token." Foreground="{StaticResource Brush.Text.Muted}" TextWrapping="Wrap" Margin="0,4,0,14"/>
                    <ItemsControl ItemsSource="{Binding PrimaryPlacementActions}">
                        <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Button Content="{Binding Label}" ToolTip="{Binding Tooltip}" Tag="{Binding}" Click="RunPlacementAction_Click" Style="{StaticResource CyberButtonStyle}" Margin="0,0,10,10" MinWidth="168"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    <TextBlock Text="Reports, History, and Help" FontSize="14" FontWeight="Bold" Margin="0,10,0,8"/>
                    <ItemsControl ItemsSource="{Binding SecondaryPlacementActions}">
                        <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Button Content="{Binding Label}" ToolTip="{Binding Tooltip}" Tag="{Binding}" Click="RunPlacementAction_Click" Style="{StaticResource CyberGhostButtonStyle}" Margin="0,0,10,10" MinWidth="150"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </Border>

            <Grid Margin="0,0,0,16">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="1.18*"/>
                    <ColumnDefinition Width="1*"/>
                </Grid.ColumnDefinitions>
                <Border Style="{StaticResource CyberCardStyle}" Margin="0,0,14,0">
                    <StackPanel>
                        <TextBlock Text="__RESULT__" FontSize="17" FontWeight="Bold"/>
                        <TextBlock Text="{Binding ResultSummary}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,10,0,12"/>
                        <Border Style="{StaticResource CyberBadgeStyle}">
                            <TextBlock Text="{Binding Status}" Foreground="{StaticResource Brush.Text.Primary}" TextWrapping="Wrap"/>
                        </Border>
                        <Expander Header="Technical Details" Foreground="{StaticResource Brush.Text.Primary}" Margin="0,16,0,0" IsExpanded="False">
                            <StackPanel Margin="0,10,0,0">
                                <ItemsControl ItemsSource="{Binding AdvancedRouteLines}" Margin="0,0,0,12">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <TextBlock Text="{Binding}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" FontFamily="Consolas" FontSize="11" Margin="0,0,0,3"/>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                                <TextBox Text="{Binding LiveResult}" IsReadOnly="True" TextWrapping="Wrap" AcceptsReturn="True" MinHeight="165" MaxHeight="320" VerticalScrollBarVisibility="Auto" FontFamily="Consolas" FontSize="12" Background="#0B1220" Foreground="{StaticResource Brush.Text.Secondary}" BorderBrush="{StaticResource Brush.Border.Subtle}"/>
                            </StackPanel>
                        </Expander>
                    </StackPanel>
                </Border>
                <Border Grid.Column="1" Style="{StaticResource CyberCardStyle}">
                    <StackPanel>
                        <TextBlock Text="Safety and Restore" FontSize="17" FontWeight="Bold"/>
                        <Border Style="{StaticResource WarningBadgeStyle}" Margin="0,10,0,10">
                            <TextBlock Text="__SAFETY__" Foreground="{StaticResource Brush.Status.Warning}" TextWrapping="Wrap"/>
                        </Border>
                        <TextBlock Text="{Binding RestoreNote}" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,0,0,14"/>
                        <ItemsControl ItemsSource="{Binding RestorePlacementActions}">
                            <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Button Content="{Binding Label}" ToolTip="{Binding Tooltip}" Tag="{Binding}" Click="RunPlacementAction_Click" Style="{StaticResource CyberGhostButtonStyle}" Margin="0,0,10,10" MinWidth="150"/>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
            </Grid>

            <Border Style="{StaticResource CyberCardStyle}">
                <StackPanel>
                    <TextBlock Text="Page Recommendations" FontSize="17" FontWeight="Bold"/>
                    <TextBlock Text="- __REC1__" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap" Margin="0,10,0,7"/>
                    <TextBlock Text="- __REC2__" Foreground="{StaticResource Brush.Text.Secondary}" TextWrapping="Wrap"/>
                    <ItemsControl ItemsSource="{Binding Recommendations}" Margin="0,10,0,0">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding StringFormat=- {0}}" Foreground="{StaticResource Brush.Text.Muted}" TextWrapping="Wrap" Margin="0,0,0,6"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</views:PlacementActionPageBase>
'@

foreach ($page in $pages) {
    $xaml = $template
    foreach ($property in $page.PSObject.Properties) {
        $token = $property.Name.ToUpperInvariant()
        $xaml = $xaml.Replace("__$($token)__", (Escape-Xml ([string]$property.Value)))
    }

    $xamlPath = Join-Path $viewsDir "$($page.View).xaml"
    Set-Content -LiteralPath $xamlPath -Value $xaml -Encoding UTF8

    $codeBehind = @"
namespace HyperBoostX.Views
{
    public partial class $($page.View) : PlacementActionPageBase
    {
        public $($page.View)() => InitializeComponent();
    }
}
"@
    Set-Content -LiteralPath (Join-Path $viewsDir "$($page.View).xaml.cs") -Value $codeBehind -Encoding UTF8
}

$existingVm = @(
    "StartupManagerViewModel",
    "CleanupViewModel",
    "OneClickBoostViewModel",
    "AutoGamingModeViewModel",
    "AIPerformanceAdvisorViewModel",
    "GpuCenterViewModel",
    "CreatorModeViewModel",
    "RestoreBackupViewModel"
)

$vmLines = New-Object System.Collections.Generic.List[string]
$vmLines.Add("namespace HyperBoostX.ViewModels")
$vmLines.Add("{")
foreach ($page in $pages | Where-Object { $existingVm -notcontains $_.Vm }) {
    $glyph = $page.Key.Substring(0, [Math]::Min(2, $page.Key.Length)).ToUpperInvariant()
    $vmLines.Add("    public sealed class $($page.Vm) : PlacementPageViewModel")
    $vmLines.Add("    {")
    $vmLines.Add("        public $($page.Vm)() : base(`"$($page.Key)`", `"$($page.Title)`", `"$($page.Subtitle)`", `"$($page.Module1)`", `"$($page.Rec1)`", `"$($page.Rec2)`")")
    $vmLines.Add("        {")
    $vmLines.Add("            Metrics.Add(new CyberMetricViewModel { Title = `"$($page.Metric1)`", Value = `"Ready`", Detail = `"Load local evidence first`", Score = 80, Glyph = `"$glyph`" });")
    $vmLines.Add("            Metrics.Add(new CyberMetricViewModel { Title = `"$($page.Metric2)`", Value = `"Guarded`", Detail = `"Safety Guard active`", Score = 100, Glyph = `"SG`" });")
    $vmLines.Add("        }")
    $vmLines.Add("    }")
    $vmLines.Add("")
}
$vmLines.Add("}")
Set-Content -LiteralPath (Join-Path $viewModelsDir "CoreFeatureViewModels.cs") -Value ($vmLines -join [Environment]::NewLine) -Encoding UTF8

Write-Host "Generated $($pages.Count) core page views and CoreFeatureViewModels.cs"
