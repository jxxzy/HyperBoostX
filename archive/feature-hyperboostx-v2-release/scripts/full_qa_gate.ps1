[CmdletBinding()]
param(
    [switch]$SkipInstall,
    [switch]$IncludeInstaller
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$qaRoot = Join-Path $repoRoot "artifacts\qa"
$logPath = Join-Path $qaRoot "full_qa_gate.log"
$jsonPath = Join-Path $qaRoot "full_qa_summary.json"
$mdPath = Join-Path $qaRoot "full_qa_summary.md"
$expectedVersion = (Get-Content -LiteralPath (Join-Path $repoRoot "VERSION") -Raw).Trim()
$releaseChannel = if ($expectedVersion -match "-") { "Beta" } else { "Stable" }

New-Item -ItemType Directory -Force -Path $qaRoot | Out-Null
if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

$script:results = @()
$script:commands = @()

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date).ToString("s"), $Message
    $line | Tee-Object -FilePath $logPath -Append
}

function Add-Result {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Evidence
    )
    $script:results += [pscustomobject]@{
        name = $Name
        status = $Status
        evidence = $Evidence
    }
}

function Invoke-GateStep {
    param(
        [string]$Name,
        [string]$Command,
        [scriptblock]$Action
    )

    $script:commands += $Command
    Write-Log ""
    Write-Log "=== $Name ==="
    Write-Log "COMMAND: $Command"
    $started = Get-Date
    try {
        & $Action 2>&1 | ForEach-Object { Write-Log ([string]$_) }
        $elapsed = [Math]::Round(((Get-Date) - $started).TotalSeconds, 2)
        Add-Result $Name "PASS" "Completed in ${elapsed}s"
    }
    catch {
        $elapsed = [Math]::Round(((Get-Date) - $started).TotalSeconds, 2)
        Write-Log "ERROR: $($_.Exception.Message)"
        Add-Result $Name "FAIL" "$($_.Exception.Message) after ${elapsed}s"
    }
}

Push-Location $repoRoot
try {
    Write-Log "HyperBoostX full QA gate"
    Write-Log "Repo root: $repoRoot"

    Invoke-GateStep "Environment info" "whoami; whoami /groups; dotnet --info; python --version; node --version; npm --version; git --version" {
        whoami
        whoami /groups
        dotnet --info
        python --version
        node --version
        npm --version
        git --version
        $PSVersionTable
        Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsBuildNumber, OsArchitecture
    }

    Invoke-GateStep "Git info" "git status --short; git branch --show-current; git log -1 --oneline" {
        git status --short
        git branch --show-current
        git log -1 --oneline
    }

    Invoke-GateStep "Secret scan" "realistic token/webhook regex scan over source/docs/scripts/tests" {
        $files = Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
            $_.FullName -notmatch "\\.git\\|\\bin\\|\\obj\\|\\node_modules\\|\\artifacts\\|\\dist\\|\\build_tmp\\|\\release\\|\\runtime_audit\\|\\app\\venv(\\|\.|$)|\\.pytest_cache\\|__pycache__" -and
            $_.Extension -notin @(".exe", ".dll", ".pdb", ".zip", ".pyc", ".ico", ".png", ".jpg", ".jpeg")
        }
        $pattern = "ghp_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|sk-[A-Za-z0-9]{20,}|nvapi-[A-Za-z0-9_-]{20,}|xox[baprs]-[A-Za-z0-9-]{20,}|https://discord(app)?\.com/api/webhooks/[0-9]+/[A-Za-z0-9_-]+"
        $hits = $files | Select-String -Pattern $pattern -CaseSensitive:$false -ErrorAction SilentlyContinue
        if ($hits) {
            $hits | ForEach-Object { "{0}:{1}: secret-like pattern" -f $_.Path, $_.LineNumber }
            throw "Secret scan found realistic secret-like patterns."
        }
        "No realistic token/webhook/private-key patterns found."
    }

    Invoke-GateStep "Version sync" "powershell -ExecutionPolicy Bypass -File .\scripts\verify_version_sync.ps1" {
        powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify_version_sync.ps1"
        if ($LASTEXITCODE -ne 0) { throw "Version sync failed." }
    }

    Invoke-GateStep "PowerShell syntax" "PSParser tokenize all source scripts" {
        $failures = @()
        Get-ChildItem -Recurse -Filter *.ps1 -File | Where-Object {
            $_.FullName -notmatch "\\.git\\|\\release\\|\\artifacts\\|\\runtime_audit\\|\\app\\venv\\|\\bin\\|\\obj\\"
        } | ForEach-Object {
            $errors = $null
            [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $_.FullName -Raw), [ref]$errors) | Out-Null
            if ($errors) {
                $failures += $_.FullName
                "PS1 ERROR: $($_.FullName)"
                $errors
            }
        }
        if ($failures.Count -gt 0) { throw "PowerShell syntax errors found." }
        "PowerShell syntax pass."
    }

    Invoke-GateStep ".NET restore/build/test Release" "dotnet restore HyperBoostX.sln; dotnet build HyperBoostX.sln -c Release; dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Release" {
        dotnet restore ".\HyperBoostX.sln"
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }
        dotnet build ".\HyperBoostX.sln" --configuration Release -v minimal
        if ($LASTEXITCODE -ne 0) { throw "dotnet build Release failed." }
        dotnet test ".\dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj" -c Release -v minimal
        if ($LASTEXITCODE -ne 0) { throw "dotnet test Release failed." }
    }

    Invoke-GateStep "Python pytest" "app\venv\Scripts\python.exe -m pytest -q tests" {
        & ".\app\venv\Scripts\python.exe" -m pytest -q tests
        if ($LASTEXITCODE -ne 0) { throw "pytest failed." }
    }

    Invoke-GateStep "Backend route contract" "powershell -ExecutionPolicy Bypass -File .\scripts\verify_backend_routes.ps1" {
        powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify_backend_routes.ps1"
        if ($LASTEXITCODE -ne 0) { throw "Backend route verification failed." }
    }

    Invoke-GateStep "WPF UI/UX quality" "powershell -ExecutionPolicy Bypass -File .\scripts\verify_ui_ux_quality.ps1" {
        powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify_ui_ux_quality.ps1"
        if ($LASTEXITCODE -ne 0) { throw "UI/UX quality verification failed." }
    }

    Invoke-GateStep "Real usability" "powershell -ExecutionPolicy Bypass -File .\scripts\verify_real_usability.ps1" {
        powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify_real_usability.ps1"
        if ($LASTEXITCODE -ne 0) { throw "Real usability verification failed." }
    }

    Invoke-GateStep "Release artifact contents" "powershell -ExecutionPolicy Bypass -File .\scripts\verify_release_artifact_contents.ps1" {
        powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify_release_artifact_contents.ps1"
        if ($LASTEXITCODE -ne 0) { throw "Release artifact verification failed." }
    }

    Invoke-GateStep "Docs existence" "check required final QA report docs" {
        $requiredDocs = @(
            "docs\QA_FULL_TEST_REPORT.md",
            "docs\FEATURE_PARITY_MATRIX.md",
            "docs\API_ROUTE_MATRIX.md",
            "docs\UI_SMOKE_TEST_REPORT.md",
            "docs\RELEASE_GATE_RESULT.md"
        )
        $missing = $requiredDocs | Where-Object { -not (Test-Path -LiteralPath $_) }
        if ($missing) {
            $missing | ForEach-Object { "Missing: $_" }
            throw "Required final docs missing."
        }
        "Required final docs exist."
    }

    if ($IncludeInstaller -and -not $SkipInstall) {
        Invoke-GateStep "Installed runtime verification" "powershell -ExecutionPolicy Bypass -File .\scripts\verify_installed_runtime.ps1 -ExpectedVersion $expectedVersion -BackendPort 5000 -LaunchInstalledApp -StopAfterProbe" {
            powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify_installed_runtime.ps1" -ExpectedVersion $expectedVersion -BackendPort 5000 -LaunchInstalledApp -StopAfterProbe
            if ($LASTEXITCODE -ne 0) { throw "Installed runtime verification failed." }
        }
    }
    else {
        Add-Result "Installed runtime verification" "SKIPPED" "Use -IncludeInstaller without -SkipInstall after installing the rebuilt package."
    }

    $failed = @($results | Where-Object { $_.status -eq "FAIL" })
    $skippedRequired = @($results | Where-Object {
        $_.status -eq "SKIPPED" -and $_.name -eq "Installed runtime verification"
    })
    $summaryStatus = if ($failed.Count -gt 0) {
        "PARTIAL"
    }
    elseif ($skippedRequired.Count -gt 0 -and $releaseChannel -eq "Beta") {
        "BETA_READY"
    }
    elseif ($skippedRequired.Count -gt 0) {
        "PARTIAL"
    }
    else {
        "PASS"
    }
    $summary = [ordered]@{
        generated_at = (Get-Date).ToUniversalTime().ToString("o")
        repo_root = $repoRoot
        version = $expectedVersion
        release_channel = $releaseChannel
        status = $summaryStatus
        include_installer = [bool]$IncludeInstaller
        skip_install = [bool]$SkipInstall
        commands = @($commands)
        results = @($results)
    }
    $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

    $lines = @(
        "# HyperBoostX Full QA Summary",
        "",
        "Version: $expectedVersion",
        "Channel: $releaseChannel",
        "Status: $summaryStatus",
        "",
        "| Gate | Status | Evidence |",
        "| --- | --- | --- |"
    )
    foreach ($result in $results) {
        $lines += "| $($result.name) | $($result.status) | $($result.evidence -replace '\|','/') |"
    }
    $lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

    Write-Log "Summary JSON: $jsonPath"
    Write-Log "Summary Markdown: $mdPath"
    if ($failed.Count -gt 0) { exit 1 }
}
finally {
    Pop-Location
}
