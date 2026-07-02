$script:HyperBoostXReleaseContract = [ordered]@{
    ExpectedStableMenus = 73
    ExpectedStableButtons = 606
    ExpectedUniqueUiEndpoints = 167
    ExpectedNonRealVisibleInStable = 0
}

function Get-HyperBoostXReleaseContract {
    return [pscustomobject]$script:HyperBoostXReleaseContract
}

function New-HyperBoostXContractCheck {
    param(
        [string]$Name,
        [bool]$Ok,
        [string]$Evidence
    )
    return [pscustomobject]@{
        name = $Name
        ok = $Ok
        evidence = $Evidence
    }
}

function Test-HyperBoostXActionMapContract {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ActionMapPath,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion,

        [string]$NamePrefix = "action map"
    )

    $contract = Get-HyperBoostXReleaseContract
    $checks = New-Object System.Collections.Generic.List[object]
    $resolvedPath = $ActionMapPath
    $payload = $null

    $exists = Test-Path -LiteralPath $ActionMapPath
    $checks.Add((New-HyperBoostXContractCheck "$NamePrefix exists" $exists $ActionMapPath))
    if ($exists) {
        try {
            $resolvedPath = (Resolve-Path -LiteralPath $ActionMapPath).Path
            $payload = Get-Content -LiteralPath $resolvedPath -Raw | ConvertFrom-Json
            $checks.Add((New-HyperBoostXContractCheck "$NamePrefix JSON parses" $true $resolvedPath))
        }
        catch {
            $checks.Add((New-HyperBoostXContractCheck "$NamePrefix JSON parses" $false $_.Exception.Message))
        }
    }

    if ($null -ne $payload) {
        $expectedChannel = if ($ExpectedVersion -match "-") { "Beta" } else { "Stable" }
        $summary = $payload.summary
        $menus = @($payload.menus)
        $actions = @($menus | ForEach-Object { @($_.actions) })
        $uniqueEndpoints = @(@(
            foreach ($action in $actions) {
                $pathString = [string]$action.path
                if ([string]::IsNullOrWhiteSpace($pathString) -or -not $pathString.StartsWith("/api/")) { continue }
                $pathWithoutQuery = $pathString.Split([char]"?")[0]
                "{0} {1}" -f ([string]$action.method).ToUpperInvariant(), $pathWithoutQuery
            }
        ) | Select-Object -Unique)
        $nonRealMenus = @($menus | Where-Object { $_.status -ne "Real" })
        $nonRealActions = @($actions | Where-Object { $_.status -ne "Real" -or $_.partial })
        $badPaths = @($actions | Where-Object { -not $_.path -or -not ([string]$_.path).StartsWith("/api/") })
        $unguardedMutations = @($actions | Where-Object { $_.method -ne "GET" -and $_.safety_guard -ne $true })

        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix app_version matches VERSION" ([string]$payload.app_version -eq $ExpectedVersion) "actual=$($payload.app_version); expected=$ExpectedVersion"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix channel matches VERSION" ([string]$payload.channel -eq $expectedChannel) "actual=$($payload.channel); expected=$expectedChannel"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix summary total_menus" ([int]$summary.total_menus -eq $contract.ExpectedStableMenus) "actual=$($summary.total_menus); expected=$($contract.ExpectedStableMenus)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix summary total_buttons" ([int]$summary.total_buttons -eq $contract.ExpectedStableButtons) "actual=$($summary.total_buttons); expected=$($contract.ExpectedStableButtons)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix summary total_active_buttons" ([int]$summary.total_active_buttons -eq $contract.ExpectedStableButtons) "actual=$($summary.total_active_buttons); expected=$($contract.ExpectedStableButtons)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix summary total_partial_or_roadmap_buttons" ([int]$summary.total_partial_or_roadmap_buttons -eq 0) "actual=$($summary.total_partial_or_roadmap_buttons); expected=0"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix summary total_unique_endpoints_used" ([int]$summary.total_unique_endpoints_used -eq $contract.ExpectedUniqueUiEndpoints) "actual=$($summary.total_unique_endpoints_used); expected=$($contract.ExpectedUniqueUiEndpoints)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix menus length" ($menus.Count -eq $contract.ExpectedStableMenus) "actual=$($menus.Count); expected=$($contract.ExpectedStableMenus)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix computed button count" ($actions.Count -eq $contract.ExpectedStableButtons) "actual=$($actions.Count); expected=$($contract.ExpectedStableButtons)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix computed unique endpoint count" ($uniqueEndpoints.Count -eq $contract.ExpectedUniqueUiEndpoints) "actual=$($uniqueEndpoints.Count); expected=$($contract.ExpectedUniqueUiEndpoints)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix all menus are Real" ($nonRealMenus.Count -eq 0) "non_real_menus=$($nonRealMenus.Count)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix all actions are Real" ($nonRealActions.Count -eq 0) "non_real_actions=$($nonRealActions.Count)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix all action paths start with /api/" ($badPaths.Count -eq 0) "bad_paths=$($badPaths.Count)"))
        $checks.Add((New-HyperBoostXContractCheck "$NamePrefix non-GET actions have safety_guard" ($unguardedMutations.Count -eq 0) "unguarded_mutations=$($unguardedMutations.Count)"))
    }

    $failedChecks = @($checks | Where-Object { -not $_.ok })
    return [pscustomobject]@{
        path = $resolvedPath
        checks = $checks.ToArray()
        ok = ($failedChecks.Count -eq 0)
    }
}
