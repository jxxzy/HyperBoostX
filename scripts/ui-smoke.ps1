param([string]$Executable,[string]$DataDirectory,[string]$OutputDirectory,[string]$NavigationCatalog=(Join-Path $PSScriptRoot '..\src\HyperBoostX.App\Assets\navigation.json'),[string]$OnlyRoute)
$ErrorActionPreference='Stop'
trap {
 $failure=$_
 if($other -and !$other.HasExited){Stop-Process -Id $other.Id -Force}
 if($second -and !$second.HasExited){Stop-Process -Id $second.Id -Force}
 if($p -and !$p.HasExited){Stop-Process -Id $p.Id -Force}
 Write-Error -ErrorRecord $failure -ErrorAction Continue
 throw $failure
}
if([string]::IsNullOrWhiteSpace($Executable)){throw 'An application executable path is required.'}
$Executable=[System.IO.Path]::GetFullPath($Executable)
if(!(Test-Path -LiteralPath $Executable -PathType Leaf)){throw "Application executable does not exist: $Executable"}
if([string]::IsNullOrWhiteSpace($DataDirectory)){throw 'An isolated application data directory is required.'}
if([string]::IsNullOrWhiteSpace($OutputDirectory)){throw 'A UI smoke output directory is required.'}
$DataDirectory=[System.IO.Path]::GetFullPath($DataDirectory)
$OutputDirectory=[System.IO.Path]::GetFullPath($OutputDirectory)
$NavigationCatalog=[System.IO.Path]::GetFullPath($NavigationCatalog)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class HbxDialogNative { [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l); [DllImport("user32.dll", EntryPoint="SendMessageW", CharSet=CharSet.Unicode)] public static extern IntPtr SendMessageText(IntPtr h, uint m, IntPtr w, string l); [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr h, int id); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint command); }'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
function Save-WindowScreenshot([string]$Path){
 [HbxDialogNative]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle) | Out-Null
 Start-Sleep -Milliseconds 150
 $bounds=$window.Current.BoundingRectangle
 $bitmap=New-Object System.Drawing.Bitmap([int]$bounds.Width,[int]$bounds.Height)
 $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
 try{$graphics.CopyFromScreen([int]$bounds.X,[int]$bounds.Y,0,0,$bitmap.Size);$bitmap.Save($Path)}
 finally{$graphics.Dispose();$bitmap.Dispose()}
}
function Invoke-ControlById([string]$AutomationId){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $control=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $pattern=$null
 if(!$control -or !$control.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$pattern)){throw "Control is not invokable: $AutomationId"}
 $pattern.Invoke()
}
function Select-ControlById([string]$AutomationId){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $control=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $pattern=$null
 if(!$control -or !$control.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$pattern)){throw "Control is not selectable: $AutomationId"}
 $pattern.Select()
 Start-Sleep -Milliseconds 120
}
function Get-GridRowCount([string]$AutomationId){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $grid=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $pattern=$null
 if(!$grid -or !$grid.TryGetCurrentPattern([System.Windows.Automation.GridPattern]::Pattern,[ref]$pattern)){return 0}
 return $pattern.Current.RowCount
}
function Assert-VisibleText([string]$Text){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,$Text)
 $element=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 if(!$element -or $element.Current.IsOffscreen){throw "Expected visible text is missing: $Text"}
}
function Set-ValueById([string]$AutomationId,[string]$Value){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $control=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition);$pattern=$null
 if(!$control -or !$control.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pattern)){throw "Control value is unavailable: $AutomationId"}
 $pattern.SetValue($Value)
}
function Select-FirstGridRow([System.Windows.Automation.AutomationElement]$Grid){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::DataItem)
 $rows=$Grid.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)
 foreach($row in $rows){
  $selection=$null
  if($row.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$selection)){$selection.Select();return $true}
 }
 return $false
}
function Select-ComboOptionById([string]$AutomationId,[string]$OptionName){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $control=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $expand=$null
 if(!$control -or !$control.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern,[ref]$expand)){throw "Combo box is not expandable: $AutomationId"}
 $expand.Expand()
 Start-Sleep -Milliseconds 100
 $optionCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,$OptionName)
 $options=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$optionCondition)
 $selection=$null
 foreach($option in $options){if($option.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$selection)){break}}
 if(!$selection){throw "Combo option is not selectable: $OptionName"}
 $selection.Select()
 $expand.Collapse()
 Start-Sleep -Milliseconds 250
}
function Get-SelectedComboOptionById([string]$AutomationId){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $control=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $selection=$null
 if(!$control -or !$control.TryGetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern,[ref]$selection)){throw "Combo selection is unavailable: $AutomationId"}
 $selected=$selection.Current.GetSelection()
 if($selected.Count -ne 1){throw "Combo has no unique selected option: $AutomationId"}
 return $selected[0].Current.Name
}
function Get-SettingsFileDialog([string]$AutomationId){
 $deadline=[DateTime]::UtcNow.AddSeconds(10)
 $dialog=$null
 $windowCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Window)
 while([DateTime]::UtcNow -lt $deadline){
  $dialogs=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$windowCondition)
  foreach($candidate in $dialogs){
   if($candidate.Current.Name -match '(?i)(export|import|ekspor|impor).*(settings|pengaturan|frametime)'){
    $dialog=$candidate;break
   }
  }
  if($dialog){break};Start-Sleep -Milliseconds 100
 }
 if(!$dialog -or $dialog.Current.NativeWindowHandle -eq 0){throw "Settings file dialog did not open: $AutomationId"}
 return $dialog
}
function Wait-SettingsFileDialogClosed([string]$AutomationId,[IntPtr]$DialogHandle){
 $deadline=[DateTime]::UtcNow.AddSeconds(10)
 $mainHandle=[IntPtr]$window.Current.NativeWindowHandle
 while([DateTime]::UtcNow -lt $deadline){
  $popup=[HbxDialogNative]::GetWindow($mainHandle,6)
  if($popup -ne $DialogHandle){return}
  Start-Sleep -Milliseconds 100
 }
 throw "Settings file dialog did not close after selecting a file: $AutomationId"
}
function Set-SettingsFileDialogPath([string]$AutomationId,[string]$Path){
 Invoke-ControlById $AutomationId
 $dialog=Get-SettingsFileDialog $AutomationId
 $dialogHandle=[IntPtr]$dialog.Current.NativeWindowHandle
 $fileNameHandle=[HbxDialogNative]::GetDlgItem($dialogHandle,1148)
 $acceptHandle=[HbxDialogNative]::GetDlgItem($dialogHandle,1)
 if($fileNameHandle -ne [IntPtr]::Zero -and $acceptHandle -ne [IntPtr]::Zero){
  [HbxDialogNative]::SetForegroundWindow($dialogHandle) | Out-Null
  [HbxDialogNative]::SendMessageText($fileNameHandle,0x000C,[IntPtr]::Zero,$Path) | Out-Null
  [HbxDialogNative]::SendMessage($acceptHandle,0x00F5,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null
  Wait-SettingsFileDialogClosed $AutomationId $dialogHandle
  return
 }
 [HbxDialogNative]::SetForegroundWindow($dialogHandle) | Out-Null
 Start-Sleep -Milliseconds 150
 $fileNameCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'1148')
 $fileName=$dialog.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$fileNameCondition)
 $value=$null
 if($fileName -and $fileName.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$value)){
  $value.SetValue($Path)
  $buttonCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button)
  $buttons=$dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants,$buttonCondition)
  foreach($button in $buttons){
   if($button.Current.AutomationId -ne '1' -and $button.Current.Name -notmatch '^(?i:open|buka|save|simpan)$'){continue}
   $invoke=$null
   if($button.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$invoke)){
    $invoke.Invoke()
    Wait-SettingsFileDialogClosed $AutomationId
    return
   }
  }
 }
 # Fallback for a shell dialog variant that does not expose the standard
 # filename edit (AutomationId 1148) and accept button (AutomationId 1).
 [System.Windows.Forms.SendKeys]::SendWait('%n')
 [System.Windows.Forms.SendKeys]::SendWait('^a')
 [System.Windows.Forms.SendKeys]::SendWait($Path)
 [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
 Wait-SettingsFileDialogClosed $AutomationId $dialogHandle
}
function Close-SettingsFileDialog([string]$AutomationId){
 Invoke-ControlById $AutomationId
 $dialog=Get-SettingsFileDialog $AutomationId
 [HbxDialogNative]::SetForegroundWindow([IntPtr]$dialog.Current.NativeWindowHandle) | Out-Null
 [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
 Start-Sleep -Milliseconds 300
}
function Invoke-SettingsConfirmation([bool]$Accept){
 $automationId=if($Accept){6}else{7}
 $mainHandle=[IntPtr]$window.Current.NativeWindowHandle
 $deadline=[DateTime]::UtcNow.AddSeconds(10)
 while([DateTime]::UtcNow -lt $deadline){
  # A WPF MessageBox is a separate top-level window. Prefer its actual UIA
  # button so the smoke test proves the intended decision, not just that a
  # WM_COMMAND happened to reach an owned popup.
  $dialogs=$root.FindAll([System.Windows.Automation.TreeScope]::Children,$pidCondition)
  foreach($dialog in $dialogs){
   if($dialog.Current.NativeWindowHandle -eq 0 -or [IntPtr]$dialog.Current.NativeWindowHandle -eq $mainHandle){continue}
   $buttonCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button)
   $buttons=$dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants,$buttonCondition)
   foreach($button in $buttons){
    $isMatchingChoice=if($Accept){$button.Current.Name -match '^(?i:yes|ya)$'}else{$button.Current.Name -match '^(?i:no|tidak)$'}
    if(!$isMatchingChoice){continue}
    $invoke=$null
    if($button.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$invoke)){
     $invoke.Invoke()
     Start-Sleep -Milliseconds 250
     return
    }
   }
  }
  # Native fallback for MessageBox implementations that do not expose their
  # buttons through UIA. GW_ENABLEDPOPUP is valid only after it differs from
  # the application window.
  $handle=[HbxDialogNative]::GetWindow($mainHandle,6)
  if($handle -ne [IntPtr]::Zero -and $handle -ne $mainHandle){
   [HbxDialogNative]::SendMessage($handle,0x0111,[IntPtr]$automationId,[IntPtr]::Zero) | Out-Null
   Start-Sleep -Milliseconds 250
   $remaining=[HbxDialogNative]::GetWindow($mainHandle,6)
   if($remaining -eq [IntPtr]::Zero -or $remaining -eq $mainHandle){return}
   [HbxDialogNative]::SetForegroundWindow($handle) | Out-Null
   [System.Windows.Forms.SendKeys]::SendWait($(if($Accept){'%y'}else{'{ESC}'}))
   Start-Sleep -Milliseconds 250
   $remaining=[HbxDialogNative]::GetWindow($mainHandle,6)
   if($remaining -eq [IntPtr]::Zero -or $remaining -eq $mainHandle){return}
  }
  Start-Sleep -Milliseconds 100
 }
 $statusElement=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'OperationStatus')))
 $statusText=if($statusElement){$statusElement.Current.Name}else{'Unavailable'}
 throw "Settings confirmation dialog did not accept the requested decision: $Accept. OperationStatus=$statusText."
}
$p=Start-Process -FilePath $Executable -ArgumentList @('--data-dir',('"'+$DataDirectory+'"')) -PassThru
$root=[System.Windows.Automation.AutomationElement]::RootElement
$pidCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id)
$deadline=[DateTime]::UtcNow.AddSeconds(30)
$window=$null
while([DateTime]::UtcNow -lt $deadline){
 $window=$root.FindFirst([System.Windows.Automation.TreeScope]::Children,$pidCondition)
 if($window){break};Start-Sleep -Milliseconds 300
}
if(!$window){throw 'App window did not appear.'}
# Accept local-only first-run notice, when present.
Start-Sleep -Milliseconds 700
$nameCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'OK')
$okItems=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$nameCondition)
foreach($ok in $okItems){$invoke=$null;if($ok.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$invoke)){$invoke.Invoke();break};if($ok.Current.NativeWindowHandle -ne 0){[HbxDialogNative]::SendMessage([IntPtr]$ok.Current.NativeWindowHandle,0xF5,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null;break}}
Start-Sleep -Seconds 3
$windows=$root.FindAll([System.Windows.Automation.TreeScope]::Children,$pidCondition)
foreach($candidate in $windows){if($candidate.Current.AutomationId -eq 'HyperBoostXMain'){$window=$candidate}}
$statusCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'OperationStatus')
$status=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
if(!$status){throw 'Main window status not found.'}
$paletteEvidence=[ordered]@{Opened=$false;SearchTerm='Network Profiles';Selected=$false;Navigated=$false;Closed=$false;OpenedWithShortcut=$false;DismissedWithEscape=$false;ExecutedWithEnter=$false}
Invoke-ControlById 'OpenCommandPaletteButton'
$paletteSearchCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CommandPaletteSearch')
$paletteSearch=$null;$paletteOpenDeadline=[DateTime]::UtcNow.AddSeconds(5)
while(!$paletteSearch -and [DateTime]::UtcNow -lt $paletteOpenDeadline){
 $paletteSearch=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteSearchCondition)
 if(!$paletteSearch){Start-Sleep -Milliseconds 100}
}
if(!$paletteSearch -or $paletteSearch.Current.IsOffscreen){throw 'Command palette search input did not become visible.'}
$paletteEvidence.Opened=$true
Set-ValueById 'CommandPaletteSearch' $paletteEvidence.SearchTerm
Save-WindowScreenshot (Join-Path $OutputDirectory 'command-palette.png')
$paletteResultsCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CommandPaletteResults')
$paletteResults=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteResultsCondition)
$paletteResultCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,$paletteEvidence.SearchTerm)
$paletteResult=if($paletteResults){$paletteResults.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteResultCondition)}else{$null}
$paletteSelection=$null
if(!$paletteResult -or !$paletteResult.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$paletteSelection)){
 throw 'Command palette did not expose a selectable Network Profiles result.'
}
$paletteSelection.Select();$paletteEvidence.Selected=$true
Invoke-ControlById 'ExecuteCommandPalette'
$pageTitleCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CurrentPageTitle')
$palettePageTitle=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$pageTitleCondition)
if(!$palettePageTitle -or $palettePageTitle.Current.Name -ne $paletteEvidence.SearchTerm){throw 'Command palette did not navigate to the selected route.'}
$paletteEvidence.Navigated=$true
$paletteSearch=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteSearchCondition)
if($paletteSearch -and !$paletteSearch.Current.IsOffscreen){throw 'Command palette remained visible after navigation.'}
$paletteEvidence.Closed=$true
[HbxDialogNative]::SetForegroundWindow([IntPtr]$window.Current.NativeWindowHandle) | Out-Null
[System.Windows.Forms.SendKeys]::SendWait('^{k}')
$paletteOpenDeadline=[DateTime]::UtcNow.AddSeconds(5);$paletteSearch=$null
while(!$paletteSearch -and [DateTime]::UtcNow -lt $paletteOpenDeadline){
 $paletteSearch=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteSearchCondition)
 if(!$paletteSearch){Start-Sleep -Milliseconds 100}
}
if(!$paletteSearch -or $paletteSearch.Current.IsOffscreen){throw 'Ctrl+K did not open the command palette.'}
$paletteEvidence.OpenedWithShortcut=$true
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
$paletteDismissDeadline=[DateTime]::UtcNow.AddSeconds(5)
do{
 Start-Sleep -Milliseconds 100
 $paletteSearch=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteSearchCondition)
}while($paletteSearch -and !$paletteSearch.Current.IsOffscreen -and [DateTime]::UtcNow -lt $paletteDismissDeadline)
if($paletteSearch -and !$paletteSearch.Current.IsOffscreen){throw 'Escape did not dismiss the command palette.'}
$paletteEvidence.DismissedWithEscape=$true
[System.Windows.Forms.SendKeys]::SendWait('^{k}')
$paletteOpenDeadline=[DateTime]::UtcNow.AddSeconds(5);$paletteSearch=$null
while(!$paletteSearch -and [DateTime]::UtcNow -lt $paletteOpenDeadline){
 $paletteSearch=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteSearchCondition)
 if(!$paletteSearch){Start-Sleep -Milliseconds 100}
}
if(!$paletteSearch -or $paletteSearch.Current.IsOffscreen){throw 'Ctrl+K did not reopen the command palette for keyboard execution.'}
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
$keyboardNavDeadline=[DateTime]::UtcNow.AddSeconds(5);$palettePageTitle=$null
while([DateTime]::UtcNow -lt $keyboardNavDeadline){
 $palettePageTitle=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$pageTitleCondition)
 if($palettePageTitle -and $palettePageTitle.Current.Name -eq 'Dashboard'){break}
 Start-Sleep -Milliseconds 100
}
if(!$palettePageTitle -or $palettePageTitle.Current.Name -ne 'Dashboard'){throw 'Enter did not execute the selected command-palette route.'}
$paletteSearch=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$paletteSearchCondition)
if($paletteSearch -and !$paletteSearch.Current.IsOffscreen){throw 'Command palette remained visible after Enter navigation.'}
$paletteEvidence.ExecutedWithEnter=$true
$paletteEvidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'command-palette-evidence.json') -Encoding UTF8
$routes=Get-Content -LiteralPath $NavigationCatalog -Raw | ConvertFrom-Json
if($routes.Count -ne 73){throw "Expected 73 reference routes, found $($routes.Count)."}
if(![string]::IsNullOrWhiteSpace($OnlyRoute)){
 $selectedRoute=@($routes | Where-Object {$_.name -ceq $OnlyRoute})
 if($selectedRoute.Count -ne 1){throw "Requested UI smoke route was not found uniquely: $OnlyRoute"}
 $routes=$selectedRoute
}
$results=@()
$domainPageEvidence=[ordered]@{}
$settingsFlowEvidence=$null
$searchCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'SearchFeatures')
$fileAnalysisFixture=Join-Path $OutputDirectory 'file-analysis-fixture'
$frametimeFixture=Join-Path $OutputDirectory 'frametime-fixture.csv'
@('frametime_ms,cpu,gpu,disk_io,temperature')+@(1..100|ForEach-Object{if($_ -gt 95){'45,80,95,8,78'}else{'12,35,55,1,65'}})|Set-Content -LiteralPath $frametimeFixture -Encoding ASCII
New-Item -ItemType Directory -Force -Path $fileAnalysisFixture | Out-Null
$fileAnalysisBytes=New-Object byte[] (1024*1024)
$fileAnalysisRng=[System.Security.Cryptography.RandomNumberGenerator]::Create()
try{$fileAnalysisRng.GetBytes($fileAnalysisBytes)}finally{$fileAnalysisRng.Dispose()}
[IO.File]::WriteAllBytes((Join-Path $fileAnalysisFixture 'duplicate-a.bin'),$fileAnalysisBytes)
[IO.File]::WriteAllBytes((Join-Path $fileAnalysisFixture 'duplicate-b.bin'),$fileAnalysisBytes)
foreach($route in $routes){
 $page=$route.name
 $searchReady=$false;$searchDeadline=[DateTime]::UtcNow.AddSeconds(60)
 while(!$searchReady -and [DateTime]::UtcNow -lt $searchDeadline){
  $search=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$searchCondition)
  $searchValue=$null
  if(!$search -or !$search.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$searchValue)){Start-Sleep -Milliseconds 100;continue}
  try{$searchValue.SetValue($page);$searchReady=$true}
  catch [System.Windows.Automation.ElementNotEnabledException]{Start-Sleep -Milliseconds 100}
  catch [System.Windows.Automation.ElementNotAvailableException]{Start-Sleep -Milliseconds 100}
 }
 if(!$searchReady){
  $titleCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CurrentPageTitle')
  $currentTitle=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$titleCondition)
  $currentStatus=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
  $titleText=if($currentTitle){$currentTitle.Current.Name}else{'Unavailable'}
  $statusText=if($currentStatus){$currentStatus.Current.Name}else{'Unavailable'}
  throw "Navigation search remained disabled before route: $page. CurrentPage=$titleText; Status=$statusText."
 }
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,$page)
 $selected=$false;$selectionDeadline=[DateTime]::UtcNow.AddSeconds(30)
 while(!$selected -and [DateTime]::UtcNow -lt $selectionDeadline){
  $all=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)
  foreach($item in $all){
   $pattern=$null
   if($item.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$pattern)){
    try{$pattern.Select();$selected=$true;break}catch [System.Windows.Automation.ElementNotEnabledException]{}
   }
  }
  if(!$selected){Start-Sleep -Milliseconds 100}
 }
 $results += [pscustomobject]@{Page=$page;Selected=$selected}
 $results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'ui-navigation-progress.json') -Encoding UTF8
 if(!$selected){throw "Navigation failed: $page"}
 Start-Sleep -Milliseconds 80
 $titleCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CurrentPageTitle')
 $pageTitle=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$titleCondition)
 if(!$pageTitle -or $pageTitle.Current.Name -ne $page){throw "Route selection did not load the expected page: $page"}
 if($page -eq 'Smart Scan'){
  $workspaceCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'SmartScanTabs')
  $workspace=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$workspaceCondition)
  if(!$workspace -or $workspace.Current.IsOffscreen){throw 'Smart Scan workspace panel is missing or offscreen.'}
  $quickCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Dashboard')
  $quick=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$quickCondition);$quickToggle=$null
  if(!$quick -or !$quick.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern,[ref]$quickToggle)){throw 'Quick scan scope is not toggleable.'}
  if($quickToggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On){throw 'Quick scan scope is not selected by default.'}
  $quickToggle.Toggle();Start-Sleep -Milliseconds 100
  if($quickToggle.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::Off){throw 'Quick scan scope selection did not update.'}
  $quickToggle.Toggle();Start-Sleep -Milliseconds 100
  $privacyCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Privacy')
  $privacy=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$privacyCondition)
  if(!$privacy -or $privacy.Current.IsEnabled){throw 'Unsupported privacy scope was not visibly disabled.'}
  $driverCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'Drivers')
  $drivers=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$driverCondition)
  if(!$drivers -or $drivers.Current.IsEnabled){throw 'Unsupported driver scope was not visibly disabled.'}
  Invoke-ControlById 'ScanButton'
  $scanDeadline=[DateTime]::UtcNow.AddSeconds(150);$scanCompleted=$false
  while([DateTime]::UtcNow -lt $scanDeadline){
   Start-Sleep -Milliseconds 250
   $status=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   if($status -and $status.Current.Name -like 'Smart Scan completed:*'){$scanCompleted=$true;break}
  }
  if(!$scanCompleted){throw "Read-only Smart Scan did not complete. Last status: $($status.Current.Name)"}
  Select-ControlById 'SmartScanEvidenceTab'
  if((Get-GridRowCount 'SmartScanEvidenceGrid') -lt 1){throw 'Smart Scan evidence grid is empty after a completed scan.'}
  Select-ControlById 'SmartScanHistoryTab'
  if((Get-GridRowCount 'SmartScanHistoryGrid') -lt 1){throw 'Smart Scan history did not record the completed scan.'}
  Select-ControlById 'SmartScanWorkspaceTab'
  Save-WindowScreenshot (Join-Path $OutputDirectory 'smart-scan.png')
  $domainPageEvidence.smartScanScopesToggled=$true
  $domainPageEvidence.unsupportedPrivacyScopeDisabled=$true
  $domainPageEvidence.unsupportedDriverScopeDisabled=$true
  $domainPageEvidence.readOnlyScanCompleted=$true
  $domainPageEvidence.evidenceAndHistoryRendered=$true
 }
 if($page -eq 'CPU Optimization'){
  $centerCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CpuOptimizationTabs')
  $center=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$centerCondition)
  if(!$center -or $center.Current.IsOffscreen){throw 'CPU Optimization workspace is missing or offscreen.'}
  $logicalCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CpuLogicalProcessors')
  $logical=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$logicalCondition)
  if(!$logical -or $logical.Current.Name -notmatch '^\d+$'){throw 'CPU Optimization did not display the Windows logical processor count.'}
  $policyCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CpuPolicyStatus')
  $policy=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$policyCondition)
  if(!$policy -or $policy.Current.Name -notmatch 'NOT_IMPLEMENTED'){throw 'Unimplemented CPU policy controls were not disclosed.'}
  $gameLibraryCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CpuGameLibraryButton')
  $gameLibraryButton=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$gameLibraryCondition)
  if(!$gameLibraryButton -or !$gameLibraryButton.Current.IsEnabled){throw 'Supported per-game priority handoff is unavailable from CPU Optimization.'}
  $guardCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CpuMemoryGuardEnabled')
  $guard=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$guardCondition);$guardToggle=$null
  if(!$guard -or !$guard.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern,[ref]$guardToggle)){throw 'CPU Center Memory Guard control is unavailable.'}
  $initialGuardState=$guardToggle.Current.ToggleState
  $guardToggle.Toggle();Start-Sleep -Milliseconds 150
  if($guardToggle.Current.ToggleState -eq $initialGuardState){throw 'Memory Guard did not update its persisted toggle state.'}
  $thresholdCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CpuMemoryGuardThreshold')
  $threshold=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$thresholdCondition)
  if(!$threshold -or !$threshold.Current.IsEnabled){throw 'Memory Guard threshold did not enable with the alert toggle.'}
  $guardToggle.Toggle();Start-Sleep -Milliseconds 150
  if($guardToggle.Current.ToggleState -ne $initialGuardState){throw 'Memory Guard did not return to its original state.'}
  Invoke-ControlById 'ScanButton'
  $scanDeadline=[DateTime]::UtcNow.AddSeconds(150);$scanCompleted=$false
  while([DateTime]::UtcNow -lt $scanDeadline){
   Start-Sleep -Milliseconds 250
   $status=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   if($status -and $status.Current.Name -like 'CPU Optimization:*observasi*'){$scanCompleted=$true;break}
  }
  if(!$scanCompleted){$lastStatus=if($status){$status.Current.Name}else{'no status'};throw "CPU process scan did not complete. Last status: $lastStatus"}
  Select-ControlById 'CpuOptimizationEvidenceTab'
  if((Get-GridRowCount 'CpuScanEvidenceGrid') -lt 1){throw 'CPU process scan produced no visible evidence.'}
  Select-ControlById 'CpuOptimizationHistoryTab'
  if((Get-GridRowCount 'CpuScanHistoryGrid') -lt 1){throw 'CPU process scan was not recorded in local history.'}
  Select-ControlById 'CpuOptimizationWorkspaceTab'
  Save-WindowScreenshot (Join-Path $OutputDirectory 'cpu-optimization.png')
  $domainPageEvidence.cpuOptimizationWorkspaceRendered=$true
  $domainPageEvidence.logicalProcessorCountRendered=$true
  $domainPageEvidence.cpuPolicyLimitsDisclosed=$true
  $domainPageEvidence.perGamePriorityHandoffAvailable=$true
  $domainPageEvidence.memoryGuardToggleValidated=$true
  $domainPageEvidence.readOnlyProcessScanCompleted=$true
  $domainPageEvidence.processEvidenceAndHistoryRendered=$true
 }
 if($page -eq 'Per-Game Profiles'){
  $centerCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GameProfileTabs')
  $center=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$centerCondition)
  if(!$center -or $center.Current.IsOffscreen){throw 'Per-Game Profiles workspace is missing or offscreen.'}
  foreach($automationId in @('GameProfileCapabilityWorkspaceTitle','GameProfileCapabilityEditorTitle','GameProfileCapabilityImportTitle','GameProfileCapabilityExportTitle','GameProfileCapabilityHistoryTitle','GameProfileActionSafetyTitle','GameProfileModuleContractTitle')){
   $capabilityCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$automationId)
   $capability=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$capabilityCondition)
   if(!$capability -or $capability.Current.IsOffscreen){throw "Per-Game Profiles prototype section is missing or offscreen: $automationId"}
  }
  $autoApplyCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GameProfileAutoApplyStatus')
  $autoApply=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$autoApplyCondition)
  if(!$autoApply -or $autoApply.Current.Name -notmatch 'NOT_IMPLEMENTED'){throw 'Unsupported automatic game-profile activation was not disclosed.'}
  $profileListCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GameProfileLibraryList')
  $profileList=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$profileListCondition)
  $emptyMessageCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GameProfileEmptyStateMessage')
  $emptyMessage=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$emptyMessageCondition)
  $emptyButtonCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GameProfileEmptyStateRegisterButton')
  $emptyButton=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$emptyButtonCondition)
  $libraryVisible=$profileList -and !$profileList.Current.IsOffscreen
  $emptyStateVisible=$emptyMessage -and !$emptyMessage.Current.IsOffscreen
  if([bool]$libraryVisible -eq [bool]$emptyStateVisible){throw 'Per-Game Profiles must show exactly one of the registered library or its empty state.'}
  if($emptyStateVisible -and (!$emptyButton -or !$emptyButton.Current.IsEnabled)){throw 'The game-profile empty state does not expose an enabled registration action.'}
  Select-ControlById 'GameProfilesEvidenceTab'
  Invoke-ControlById 'ScanSavedGamesButton'
  $scanDeadline=[DateTime]::UtcNow.AddSeconds(30);$scanCompleted=$false
  while([DateTime]::UtcNow -lt $scanDeadline){
   Start-Sleep -Milliseconds 150
   $status=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   if($status -and $status.Current.Name -like 'Pencocokan executable game selesai*'){$scanCompleted=$true;break}
  }
  if(!$scanCompleted){$lastStatus=if($status){$status.Current.Name}else{'no status'};throw "Saved-game process scan did not complete. Last status: $lastStatus"}
  if((Get-GridRowCount 'GameProfileEvidenceGrid') -lt 1){throw 'Saved-game scan produced no visible process evidence.'}
  Select-ControlById 'GameProfilesHistoryTab'
  Select-ControlById 'GameProfilesWorkspaceTab'
  Save-WindowScreenshot (Join-Path $OutputDirectory 'per-game-profiles.png')
  $domainPageEvidence.perGameProfilesWorkspaceRendered=$true
  $domainPageEvidence.perGameProfilesEmptyStateRendered=[bool]$emptyStateVisible
  $domainPageEvidence.automaticActivationNotImplementedDisclosed=$true
  $domainPageEvidence.readOnlySavedGameScanCompleted=$true
  $domainPageEvidence.evidenceAndHistoryTabsAvailable=$true
 }
 if($page -eq 'Audio & DJ Performance'){
  Assert-VisibleText 'Advanced Mic Mixer'
  Assert-VisibleText 'Voicemeeter'
  $detailCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'RouteImplementationDetail')
  $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
  if(!$detail -or $detail.Current.Name -notmatch 'Advanced Mic Mixer and Voicemeeter integration are not implemented'){throw 'Audio integration limitations for the current prototype capabilities were not disclosed.'}
  $domainPageEvidence.audioMixerAndVoicemeeterLimitationsDisclosed=$true
 }
 if($page -eq 'Webcam Diagnostics'){
  Assert-VisibleText 'Camera tracking'
  $detailCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'RouteImplementationDetail')
  $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
  if(!$detail -or $detail.Current.Name -notmatch 'Camera tracking, driver-state analysis and permission auditing are not implemented'){throw 'Webcam capability limitations were not disclosed.'}
  $domainPageEvidence.webcamCapabilityLimitationsDisclosed=$true
 }
 if($page -eq 'Security Health'){
  $detailCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'RouteImplementationDetail')
  $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
  if(!$detail -or $detail.Current.Name -notmatch 'Defender Scan Guard is not implemented'){throw 'Defender Scan Guard limitation was not disclosed.'}
  $domainPageEvidence.defenderScanGuardLimitationDisclosed=$true
 }
 if($page -eq 'Trust Center'){
  Assert-VisibleText 'System Reality Guard'
  Assert-VisibleText 'Protected Apps'
  $detailCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'RouteImplementationDetail')
  $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
  if(!$detail -or $detail.Current.Name -notmatch 'System Reality Guard, Protected Apps'){throw 'Trust Center capability limitations were not disclosed.'}
  $domainPageEvidence.trustCenterCapabilityLimitationsDisclosed=$true
 }
 if($page -eq 'GPU Center'){
  $centerCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GPUCenterTabs')
  $center=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$centerCondition)
  if(!$center -or $center.Current.IsOffscreen){throw 'GPU Center workspace is missing or offscreen.'}
  $filterCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'GPUVendorNameFilter')
  $filter=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$filterCondition)
  if(!$filter -or !$filter.Current.IsEnabled){throw 'GPU adapter-name filter is unavailable.'}
  Invoke-ControlById 'ScanButton'
  $scanDeadline=[DateTime]::UtcNow.AddSeconds(150);$scanCompleted=$false
  while([DateTime]::UtcNow -lt $scanDeadline){
   Start-Sleep -Milliseconds 250
   $status=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   if($status -and $status.Current.Name -like 'GPU Center:*observasi*'){$scanCompleted=$true;break}
  }
  if(!$scanCompleted){$lastStatus=if($status){$status.Current.Name}else{'no status'};throw "GPU diagnostics scan did not complete. Last status: $lastStatus"}
  Select-ControlById 'GPUCenterEvidenceTab'
  if((Get-GridRowCount 'GPUCenterEvidenceGrid') -lt 1){throw 'GPU scan produced no visible Windows-reported evidence.'}
  Select-ControlById 'GPUCenterHistoryTab'
  if((Get-GridRowCount 'GPUCenterHistoryGrid') -lt 1){throw 'GPU scan was not recorded in local history.'}
  Select-ControlById 'GPUCenterWorkspaceTab'
  Save-WindowScreenshot (Join-Path $OutputDirectory 'gpu-center.png')
  $domainPageEvidence.gpuCenterWorkspaceRendered=$true
  $domainPageEvidence.gpuAdapterNameFilterAvailable=$true
  $domainPageEvidence.readOnlyGpuScanCompleted=$true
  $domainPageEvidence.gpuEvidenceAndHistoryRendered=$true
 }
 if($page -eq 'One Click Boost'){
  $boostCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'OneClickBoostTitle')
  $boost=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$boostCondition)
  if(!$boost -or $boost.Current.IsOffscreen){throw 'One Click Boost capability overview is missing or offscreen.'}
  $previewCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'PreviewAction')
  if(!$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$previewCondition)){throw 'One Click Boost has no registered action review control.'}
  Save-WindowScreenshot (Join-Path $OutputDirectory 'one-click-boost.png')
  $domainPageEvidence.oneClickCapabilityOverviewRendered=$true
  $domainPageEvidence.registeredActionReviewAvailable=$true
 }
 if($page -eq 'DNS & Latency'){
  $hostCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'NetworkTestHost')
  $hostControl=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$hostCondition)
  $hostValue=$null
  if(!$hostControl -or !$hostControl.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$hostValue)){throw 'DNS test host input is unavailable.'}
  $hostValue.SetValue('localhost')
  Invoke-ControlById 'ResolveDnsButton'
  $resolvedCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Resolved')
  $deadline=[DateTime]::UtcNow.AddSeconds(10)
  $resolved=$null
  while([DateTime]::UtcNow -lt $deadline){
   $resolved=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$resolvedCondition)
   if($resolved){break};Start-Sleep -Milliseconds 100
  }
  if(!$resolved){throw 'DNS command did not report a successful localhost resolution.'}
 }
 if($page -eq 'HyperBoost Score'){
  Invoke-ControlById 'ScanButton'
  $deadline=[DateTime]::UtcNow.AddSeconds(20);$scoreRows=0
  while([DateTime]::UtcNow -lt $deadline){
   $scoreRows=Get-GridRowCount 'ObservationFacts'
   if($scoreRows -ge 5){break};Start-Sleep -Milliseconds 100
  }
  if($scoreRows -lt 5){throw "HyperBoost Score did not render its measured score and methodology rows: rows=$scoreRows"}
 }
 if($page -eq 'Duplicate & Large Files'){
  $rootCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'FileAnalysisRoot')
  $rootControl=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$rootCondition);$rootPattern=$null
  if(!$rootControl -or !$rootControl.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$rootPattern)){throw 'File analysis root input is unavailable.'}
  $thresholdCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'LargeFileThreshold')
  $thresholdControl=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$thresholdCondition);$thresholdPattern=$null
  if(!$thresholdControl -or !$thresholdControl.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$thresholdPattern)){throw 'Large file threshold input is unavailable.'}
  $rootPattern.SetValue($fileAnalysisFixture);$thresholdPattern.SetValue('1');Invoke-ControlById 'RunFileAnalysisButton'
  $deadline=[DateTime]::UtcNow.AddSeconds(30);$duplicateRows=0;$largeRows=0
  while([DateTime]::UtcNow -lt $deadline){$duplicateRows=Get-GridRowCount 'DuplicateGroupsGrid';$largeRows=Get-GridRowCount 'LargeFilesGrid';if($duplicateRows -gt 0 -and $largeRows -eq 2){break};Start-Sleep -Milliseconds 150}
  if($duplicateRows -le 0 -or $largeRows -ne 2){
   $analysisStatus=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'FileAnalysisStatus')))
   $operationStatus=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   $runButton=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,(New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'RunFileAnalysisButton')))
   $analysisText=if($analysisStatus){$analysisStatus.Current.Name}else{'Unavailable'}
   $operationText=if($operationStatus){$operationStatus.Current.Name}else{'Unavailable'}
   $runEnabled=if($runButton){$runButton.Current.IsEnabled}else{$false}
   throw "File analysis did not render expected duplicate/large-file rows: duplicate=$duplicateRows large=$largeRows; FileStatus=$analysisText; OperationStatus=$operationText; RunButtonEnabled=$runEnabled."
  }
  if((Get-FileHash (Join-Path $fileAnalysisFixture 'duplicate-a.bin')).Hash -ne (Get-FileHash (Join-Path $fileAnalysisFixture 'duplicate-b.bin')).Hash){throw 'File analysis fixture changed during the read-only scan.'}
 }
 if($page -eq 'App Load Optimizer'){
  Invoke-ControlById 'ScanButton'
  $deadline=[DateTime]::UtcNow.AddSeconds(30);$inventoryRows=0
  while([DateTime]::UtcNow -lt $deadline){$inventoryRows=Get-GridRowCount 'ObservationFacts';if($inventoryRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($inventoryRows -le 0){throw 'App Load installed-application inventory did not render results.'}
  $pathCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'AppLoadExecutable')
  $pathControl=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$pathCondition);$pathPattern=$null
  if(!$pathControl -or !$pathControl.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pathPattern)){throw 'App Load executable input is unavailable.'}
  $measurementTarget=Join-Path (Split-Path -Parent $Executable) 'HyperBoostX.Worker.exe'
  if(!(Test-Path -LiteralPath $measurementTarget)){throw 'Packaged worker fixture for App Load measurement is missing.'}
  $pathPattern.SetValue($measurementTarget)
  Invoke-ControlById 'MeasureAppLoadButton'
  Invoke-SettingsConfirmation $true
  $deadline=[DateTime]::UtcNow.AddSeconds(20);$measured=$null
  $measuredCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Process created')
  while([DateTime]::UtcNow -lt $deadline){$measured=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$measuredCondition);if($measured){break};Start-Sleep -Milliseconds 150}
  if(!$measured){
   $measurementStatus=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   $measurementStatusText=if($measurementStatus){$measurementStatus.Current.Name}else{'Unavailable'}
   $observationRows=Get-GridRowCount 'ObservationFacts'
   throw "App Load measurement did not render a measured process-creation result. Status=$measurementStatusText; rows=$observationRows."
  }
 }
 if($page -eq 'RGB Software Detector'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$rgbRows=0
  while([DateTime]::UtcNow -lt $deadline){$rgbRows=Get-GridRowCount 'ObservationFacts';if($rgbRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($rgbRows -le 0){throw 'RGB Software Detector returned no visible observation or explicit no-match result.'}
 }
 if($page -eq 'MSI Safe Optimizer'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$msiRows=0
  while([DateTime]::UtcNow -lt $deadline){$msiRows=Get-GridRowCount 'ObservationFacts';if($msiRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($msiRows -le 0){throw 'MSI Safe Optimizer returned no safety policy or MSI observation.'}
 }
 if($page -eq 'Overlay Conflict Detector'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$overlayRows=0
  while([DateTime]::UtcNow -lt $deadline){$overlayRows=Get-GridRowCount 'ObservationFacts';if($overlayRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($overlayRows -le 0){throw 'Overlay Conflict Detector returned no visible observation or explicit no-match result.'}
 }
 if($page -eq 'Boot Optimizer'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$bootRows=0
  while([DateTime]::UtcNow -lt $deadline){$bootRows=Get-GridRowCount 'ObservationFacts';if($bootRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($bootRows -le 0){throw 'Boot Optimizer returned no boot-history or explicit provider result.'}
 }
 if($page -eq 'Drive Optimization'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$driveRows=0
  while([DateTime]::UtcNow -lt $deadline){$driveRows=Get-GridRowCount 'ObservationFacts';if($driveRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($driveRows -le 0){throw 'Drive Optimization returned no storage-policy or explicit provider result.'}
 }
 if($page -eq 'LCD Performance Guard'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$lcdRows=0
  while([DateTime]::UtcNow -lt $deadline){$lcdRows=Get-GridRowCount 'ObservationFacts';if($lcdRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($lcdRows -le 0){throw 'LCD Performance Guard returned no policy or process observation.'}
 }
 if($page -eq 'Stutter Analyzer'){
  Set-SettingsFileDialogPath 'ImportFrametimeButton' $frametimeFixture
  $deadline=[DateTime]::UtcNow.AddSeconds(20);$fpsResult=$null;$frametimeRows=0
  $fpsCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Average FPS')
  while([DateTime]::UtcNow -lt $deadline){
   $fpsResult=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$fpsCondition)
   $frametimeRows=Get-GridRowCount 'ObservationFacts'
   if($fpsResult -and $frametimeRows -ge 6){break};Start-Sleep -Milliseconds 150
  }
  if(!$fpsResult -or $frametimeRows -lt 6){throw "Stutter Analyzer did not render imported frametime evidence: rows=$frametimeRows."}
 }
 if($page -eq 'Research Mode'){
  Set-ValueById 'ResearchExperiment' 'UI smoke experiment';Set-ValueById 'ResearchWorkload' 'Fixed workload';Set-ValueById 'ResearchMetric' 'Average FPS';Set-ValueById 'ResearchValue' '100';Set-ValueById 'ResearchUnit' 'FPS'
  Invoke-ControlById 'SaveResearchTrialButton'
  $analyzeCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'AnalyzeResearchButton')
  $deadline=[DateTime]::UtcNow.AddSeconds(10);$analyzeControl=$null
  while([DateTime]::UtcNow -lt $deadline){$analyzeControl=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$analyzeCondition);if($analyzeControl -and $analyzeControl.Current.IsEnabled){break};Start-Sleep -Milliseconds 100}
  if(!$analyzeControl -or !$analyzeControl.Current.IsEnabled){throw 'Research Mode did not enable analysis after saving a trial.'}
  Invoke-ControlById 'AnalyzeResearchButton';$deadline=[DateTime]::UtcNow.AddSeconds(20);$researchRows=0
  while([DateTime]::UtcNow -lt $deadline){$researchRows=Get-GridRowCount 'ObservationFacts';if($researchRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($researchRows -le 0){throw 'Research Mode did not render the saved variant summary.'}
 }
 if($page -eq 'Scheduled Automation'){
  Set-ValueById 'AutomationName' 'UI smoke scheduled scan';Set-ValueById 'AutomationInterval' '60';Set-ValueById 'AutomationCooldown' '60';Invoke-ControlById 'AddAutomationRuleButton'
  $deadline=[DateTime]::UtcNow.AddSeconds(10);$ruleRows=0
  while([DateTime]::UtcNow -lt $deadline){$ruleRows=Get-GridRowCount 'AutomationRulesGrid';if($ruleRows -gt 0){break};Start-Sleep -Milliseconds 100}
  if($ruleRows -le 0){throw 'Scheduled Automation did not persist a paused rule.'}
  Invoke-ControlById 'EnableAutomationRuleButton';Start-Sleep -Milliseconds 200;Invoke-ControlById 'EmergencyStopAutomationButton'
 }
 if($page -eq 'Update Center'){
  Invoke-ControlById 'InspectCurrentPackageButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$hashResult=$null
  $hashCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'SHA-256')
  while([DateTime]::UtcNow -lt $deadline){$hashResult=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$hashCondition);if($hashResult){break};Start-Sleep -Milliseconds 150}
  if(!$hashResult){throw 'Update Center did not render package SHA-256 evidence.'}
 }
 if($page -eq 'Plugin Manager'){
  Invoke-ControlById 'ReviewPluginsButton';$deadline=[DateTime]::UtcNow.AddSeconds(20);$pluginRows=0
  while([DateTime]::UtcNow -lt $deadline){$pluginRows=Get-GridRowCount 'ObservationFacts';if($pluginRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($pluginRows -le 0){throw 'Plugin Manager did not render its fail-closed inventory result.'}
 }
 if($page -eq 'Network Profiles'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$profileRows=0
  while([DateTime]::UtcNow -lt $deadline){$profileRows=Get-GridRowCount 'ObservationFacts';if($profileRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($profileRows -le 0){throw 'Network Profiles did not render its measured preview.'}
 }
 if($page -in @('Windows Services','Scheduled Tasks')){
  Invoke-ControlById 'ScanButton'
  $statusCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'OperationStatus')
  $deadline=[DateTime]::UtcNow.AddSeconds(30)
  $scanFinished=$false
  while([DateTime]::UtcNow -lt $deadline){
   $scanStatus=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$statusCondition)
   if($scanStatus -and $scanStatus.Current.Name -like '*observasi terbaca.*'){$scanFinished=$true;break}
   Start-Sleep -Milliseconds 150
  }
  if(!$scanFinished){throw "$page diagnostic scan did not finish."}
  $filterCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ObservationFilter')
  $deadline=[DateTime]::UtcNow.AddSeconds(30)
  $filterControl=$null
  while([DateTime]::UtcNow -lt $deadline){
   $filterControl=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$filterCondition)
   if($filterControl){break};Start-Sleep -Milliseconds 150
  }
  $filterPattern=$null
  if(!$filterControl -or !$filterControl.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$filterPattern)){throw 'Observation filter is unavailable.'}
  $term=if($page -eq 'Windows Services'){'Win32_Service'}else{'MSFT_ScheduledTask'}
  $filterPattern.SetValue($term)
  $deadline=[DateTime]::UtcNow.AddSeconds(30)
  $rowCount=0
  while([DateTime]::UtcNow -lt $deadline){
   $rowCount=Get-GridRowCount 'ObservationFacts'
   if($rowCount -gt 0){break};Start-Sleep -Milliseconds 150
  }
  if($rowCount -le 0){throw "$page scan/filter returned no visible inventory rows."}
  $gridCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'ObservationFacts')
  $grid=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$gridCondition)
  if(!$grid -or !(Select-FirstGridRow $grid)){throw "$page inventory rows could not be selected."}
  $detailCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'FactDetailsText')
  $deadline=[DateTime]::UtcNow.AddSeconds(5)
  $detail=$null
  while([DateTime]::UtcNow -lt $deadline){
   $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
   if($detail -and $detail.Current.Name -like "*$term*"){break};Start-Sleep -Milliseconds 100
  }
  if(!$detail -or $detail.Current.Name -notlike "*$term*"){throw "$page selected item details were not rendered."}
  $filterPattern.SetValue('')
 }
 if($page -eq 'Test Center'){
  Invoke-ControlById 'RunSafeTestsButton'
  $deadline=[DateTime]::UtcNow.AddSeconds(30)
  $rowCount=0
  while([DateTime]::UtcNow -lt $deadline){
   $rowCount=Get-GridRowCount 'TestResultsGrid'
   if($rowCount -gt 0){break};Start-Sleep -Milliseconds 150
  }
  if($rowCount -le 0){throw 'Safe Test Center did not produce visible results.'}
  $gridCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'TestResultsGrid')
  $grid=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$gridCondition)
  if(!$grid -or !(Select-FirstGridRow $grid)){throw 'Safe Test Center results could not be selected.'}
  $detailCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'TestResultDetails')
  $deadline=[DateTime]::UtcNow.AddSeconds(5)
  $detail=$null
  while([DateTime]::UtcNow -lt $deadline){
   $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
   if($detail -and $detail.Current.Name -like '*application.runtime*'){break};Start-Sleep -Milliseconds 100
  }
  if(!$detail -or $detail.Current.Name -notlike '*application.runtime*'){throw 'Safe Test Center timestamped result details were not rendered.'}
  $workerResultCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'privileged-worker.ipc')
  $gridScroll=$null
  $grid.TryGetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern,[ref]$gridScroll) | Out-Null
  $workerEvidenceVerified=$false
  for($attempt=0;$attempt -lt 16 -and !$workerEvidenceVerified;$attempt++){
   $workerTarget=$grid.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$workerResultCondition)
   $workerRow=$workerTarget
   while($workerRow -and $workerRow.Current.ControlType -ne [System.Windows.Automation.ControlType]::DataItem){$workerRow=[System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($workerRow)}
   $workerSelection=$null
   if($workerRow -and $workerRow.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$workerSelection)){
    $workerSelection.Select()
    $workerDetailDeadline=[DateTime]::UtcNow.AddSeconds(3)
    while([DateTime]::UtcNow -lt $workerDetailDeadline){
     $detail=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$detailCondition)
     $detailValue=$null
     $detailPattern=$null
     if($detail -and $detail.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$detailPattern)){$detailValue=$detailPattern.Current.Value}
     elseif($detail){$detailValue=$detail.Current.Name}
     if($detailValue -like '*privileged-worker.ipc*' -and $detailValue -like '*PASS*' -and $detailValue -like '*power inspection IPC*' -and $detailValue -like '*PowrProf returned*'){$workerEvidenceVerified=$true;break}
     Start-Sleep -Milliseconds 100
    }
   }
   if(!$workerEvidenceVerified -and $gridScroll -and $gridScroll.Current.VerticallyScrollable){$gridScroll.Scroll([System.Windows.Automation.ScrollAmount]::NoAmount,[System.Windows.Automation.ScrollAmount]::LargeIncrement);Start-Sleep -Milliseconds 100}
  }
  if(!$workerEvidenceVerified){throw 'Packaged worker power-inspection IPC did not produce a PASS result with PowrProf evidence.'}
  $domainPageEvidence.packagedWorkerPowerInspectionVerified=$true
 }
 if($page -eq 'Settings'){
  foreach($id in @('ExportSettingsButton','ImportSettingsButton','ResetSettingsButton')){
   $controlCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id)
   $control=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$controlCondition)
   $invoke=$null
   if(!$control -or !$control.Current.IsEnabled -or !$control.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$invoke)){throw "Settings workflow control is unavailable: $id"}
  }
  $settingsFile=Join-Path $OutputDirectory 'settings-roundtrip.json'
  Set-SettingsFileDialogPath 'ExportSettingsButton' $settingsFile
  $deadline=[DateTime]::UtcNow.AddSeconds(10)
  while([DateTime]::UtcNow -lt $deadline -and !(Test-Path -LiteralPath $settingsFile)){Start-Sleep -Milliseconds 100}
  if(!(Test-Path -LiteralPath $settingsFile)){throw 'Settings export did not create its selected file.'}
  $exportedJson=Get-Content -LiteralPath $settingsFile -Raw
  $exported=ConvertFrom-Json -InputObject $exportedJson
  $expectedProperties=@('Goal','Language','Mode','SchemaVersion','SidebarCollapsed','Theme')
  if(@($exported.PSObject.Properties.Name | Sort-Object) -join ',' -ne (@($expectedProperties | Sort-Object) -join ',')){throw 'Settings export contains an unexpected document shape.'}
  if($exportedJson -match '(?i)credential|secret|token|password'){throw 'Settings export contains a credential-like field.'}
  $settingsFlowEvidence=[ordered]@{
   settingsExported=$true
   exportedProperties=@($expectedProperties)
   credentialLikeFieldsExcluded=$true
   importFileDialogCancellationPreservedPreferences=$false
   importWithApprovalRestoredExportedTheme=$false
   resetDeclinePreservedPreferences=$false
   resetApprovalRestoredDefaults=$false
   defaultsPersistedAfterRestart=$false
   singleInstanceSecondLaunchActivatedExisting=$false
   distinctDataDirectoryRunsSeparateInstance=$false
  }
  Select-ComboOptionById 'ThemeSelection' 'Light'
  Close-SettingsFileDialog 'ImportSettingsButton'
  if((Get-SelectedComboOptionById 'ThemeSelection') -ne 'Light'){throw 'Canceling settings import changed the active preferences.'}
  $settingsFlowEvidence.importFileDialogCancellationPreservedPreferences=$true
  Set-SettingsFileDialogPath 'ImportSettingsButton' $settingsFile
  Invoke-SettingsConfirmation $true
  $deadline=[DateTime]::UtcNow.AddSeconds(10)
  while([DateTime]::UtcNow -lt $deadline){
   if((Get-SelectedComboOptionById 'ThemeSelection') -eq 'Dark'){break}
   Start-Sleep -Milliseconds 100
  }
  if((Get-SelectedComboOptionById 'ThemeSelection') -ne 'Dark'){throw 'Settings import did not restore the exported theme.'}
  $settingsFlowEvidence.importWithApprovalRestoredExportedTheme=$true
  Select-ComboOptionById 'ThemeSelection' 'Light'
  Invoke-ControlById 'ResetSettingsButton'
  Invoke-SettingsConfirmation $false
  if((Get-SelectedComboOptionById 'ThemeSelection') -ne 'Light'){throw 'Declining settings reset changed the active preferences.'}
  $settingsFlowEvidence.resetDeclinePreservedPreferences=$true
  Invoke-ControlById 'ResetSettingsButton'
  Invoke-SettingsConfirmation $true
  $deadline=[DateTime]::UtcNow.AddSeconds(10)
  while([DateTime]::UtcNow -lt $deadline){
   if((Get-SelectedComboOptionById 'ThemeSelection') -eq 'Dark'){break}
   Start-Sleep -Milliseconds 100
  }
  if((Get-SelectedComboOptionById 'ThemeSelection') -ne 'Dark'){throw 'Confirmed settings reset did not restore defaults.'}
  $settingsFlowEvidence.resetApprovalRestoredDefaults=$true
  $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Settings')
 }
 if($page -eq 'Dashboard'){$search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('')}
 Start-Sleep -Milliseconds 150
 if($page -in @('Dashboard','Game Boost Engine','Game Library','DNS & Latency')){
  $slug=$page.ToLowerInvariant() -replace '\s+','-'
  Save-WindowScreenshot (Join-Path $OutputDirectory ($slug+'.png'))
 }
 if($page -eq 'Dashboard'){
  $dashboardReadings=[ordered]@{}
  $metricDeadline=[DateTime]::UtcNow.AddSeconds(30)
  do{
   foreach($metric in @('DashboardCpuLoad','DashboardMemoryPressure','DashboardGpuLoad','DashboardGpuDetail')){
    $metricCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$metric)
    $element=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$metricCondition)
    if(!$element){throw "Missing Dashboard metric: $metric"}
    $dashboardReadings[$metric]=$element.Current.Name
   }
   if($dashboardReadings.DashboardCpuLoad -match '%' -and $dashboardReadings.DashboardMemoryPressure -match 'GiB' -and ($dashboardReadings.DashboardGpuLoad -match '%' -or $dashboardReadings.DashboardGpuLoad -in @('Unavailable','Tidak tersedia'))){break}
   Start-Sleep -Milliseconds 250
  }while([DateTime]::UtcNow -lt $metricDeadline)
  if($dashboardReadings.DashboardCpuLoad -notmatch '%' -or $dashboardReadings.DashboardMemoryPressure -notmatch 'GiB'){throw 'Dashboard CPU and RAM bindings did not show actual samples.'}
  if($dashboardReadings.DashboardGpuLoad -notmatch '%' -and $dashboardReadings.DashboardGpuLoad -notin @('Unavailable','Tidak tersedia')){throw 'Dashboard GPU provider did not resolve to measured or unavailable state.'}
  $dashboardReadings | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'dashboard-telemetry-evidence.json') -Encoding UTF8
  Invoke-ControlById 'ToggleSidebarButton'
  Start-Sleep -Milliseconds 200
  Save-WindowScreenshot (Join-Path $OutputDirectory 'dashboard-compact.png')
  Invoke-ControlById 'ToggleSidebarButton'
  Select-ComboOptionById 'ThemeSelection' 'Light'
  Save-WindowScreenshot (Join-Path $OutputDirectory 'dashboard-light.png')
  Select-ComboOptionById 'ThemeSelection' 'Dark'
 }
}
if($results.Count -ne $routes.Count -or @($results | Where-Object {!$_.Selected}).Count -gt 0){throw "UI smoke selected $($results.Count) of $($routes.Count) requested routes."}
$results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'ui-navigation.json') -Encoding UTF8
$domainPageEvidence | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'domain-page-evidence.json') -Encoding UTF8
if(![string]::IsNullOrWhiteSpace($OnlyRoute)){
 Save-WindowScreenshot (Join-Path $OutputDirectory (($OnlyRoute.ToLowerInvariant() -replace '[^a-z0-9]+','-').Trim('-')+'.png'))
 $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
 if(!$p.WaitForExit(10000)){throw "Application did not exit cleanly after route: $OnlyRoute"}
 Write-Output "UI smoke passed for requested route: $OnlyRoute; exit code $($p.ExitCode)"
 return
}
$search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Settings')
$settingsCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Settings')
$settingsItems=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$settingsCondition)
$settingsSelected=$false
foreach($item in $settingsItems){$pattern=$null;if($item.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$pattern)){$pattern.Select();$settingsSelected=$true;break}}
if(!$settingsSelected){throw 'Settings route could not be restored for the final screenshot.'}
$search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('')
Start-Sleep -Milliseconds 150
Save-WindowScreenshot (Join-Path $OutputDirectory 'settings.png')
$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
if(!$p.WaitForExit(10000)){throw 'Application did not exit cleanly.'}
$p=Start-Process -FilePath $Executable -ArgumentList @('--data-dir',('"'+$DataDirectory+'"')) -PassThru
$pidCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id)
$deadline=[DateTime]::UtcNow.AddSeconds(30)
$window=$null
while([DateTime]::UtcNow -lt $deadline){
 $window=$root.FindFirst([System.Windows.Automation.TreeScope]::Children,$pidCondition)
 if($window){break};Start-Sleep -Milliseconds 300
}
if(!$window){throw 'Application did not reopen with the isolated settings data.'}
Start-Sleep -Milliseconds 700
$okItems=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$nameCondition)
foreach($ok in $okItems){$invoke=$null;if($ok.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$invoke)){$invoke.Invoke();break};if($ok.Current.NativeWindowHandle -ne 0){[HbxDialogNative]::SendMessage([IntPtr]$ok.Current.NativeWindowHandle,0xF5,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null;break}}
Start-Sleep -Seconds 2
$windows=$root.FindAll([System.Windows.Automation.TreeScope]::Children,$pidCondition)
foreach($candidate in $windows){if($candidate.Current.AutomationId -eq 'HyperBoostXMain'){$window=$candidate}}
if((Get-SelectedComboOptionById 'ThemeSelection') -ne 'Dark'){throw 'Confirmed default preferences did not persist after application restart.'}
$settingsFlowEvidence.defaultsPersistedAfterRestart=$true
$windowPattern=$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
$windowPattern.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Minimized)
$second=Start-Process -FilePath $Executable -ArgumentList @('--data-dir',('"'+$DataDirectory+'"')) -PassThru
if(!$second.WaitForExit(10000)){throw 'Second application launch did not exit after requesting activation.'}
if($second.ExitCode -ne 0){throw "Second application launch failed with exit code $($second.ExitCode)."}
$deadline=[DateTime]::UtcNow.AddSeconds(5)
while([DateTime]::UtcNow -lt $deadline -and $windowPattern.Current.WindowVisualState -ne [System.Windows.Automation.WindowVisualState]::Normal){Start-Sleep -Milliseconds 100}
if($windowPattern.Current.WindowVisualState -ne [System.Windows.Automation.WindowVisualState]::Normal){throw 'Second launch did not restore the existing application window.'}
$settingsFlowEvidence.singleInstanceSecondLaunchActivatedExisting=$true
$distinctDataDirectory=Join-Path $OutputDirectory 'distinct-instance-data'
New-Item -ItemType Directory -Force -Path $distinctDataDirectory | Out-Null
$other=Start-Process -FilePath $Executable -ArgumentList @('--data-dir',('"'+$distinctDataDirectory+'"')) -PassThru
$otherPidCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$other.Id)
Start-Sleep -Milliseconds 700
$otherStartupWindows=$root.FindAll([System.Windows.Automation.TreeScope]::Children,$otherPidCondition)
foreach($candidate in $otherStartupWindows){
 $otherOkItems=$candidate.FindAll([System.Windows.Automation.TreeScope]::Descendants,$nameCondition)
 foreach($ok in $otherOkItems){
  $invoke=$null
  if($ok.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern,[ref]$invoke)){$invoke.Invoke();break}
  if($ok.Current.NativeWindowHandle -ne 0){[HbxDialogNative]::SendMessage([IntPtr]$ok.Current.NativeWindowHandle,0xF5,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null;break}
 }
}
$deadline=[DateTime]::UtcNow.AddSeconds(30)
$otherWindow=$null
while([DateTime]::UtcNow -lt $deadline){
 $otherWindows=$root.FindAll([System.Windows.Automation.TreeScope]::Children,$otherPidCondition)
 foreach($candidate in $otherWindows){if($candidate.Current.AutomationId -eq 'HyperBoostXMain'){$otherWindow=$candidate;break}}
 if($otherWindow){break}
 Start-Sleep -Milliseconds 200
}
if(!$otherWindow -or $other.HasExited){throw 'A distinct isolated data directory did not start an independent application instance.'}
Start-Sleep -Seconds 3
Stop-Process -Id $other.Id -Force
if(!$other.WaitForExit(10000)){throw 'Isolated independent-instance test process did not stop during test cleanup.'}
if($p.HasExited){throw 'Closing the distinct-data instance also terminated the original application.'}
$settingsFlowEvidence.distinctDataDirectoryRunsSeparateInstance=$true
$settingsFlowEvidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'settings-workflow.json') -Encoding UTF8
$window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
if(!$p.WaitForExit(10000)){throw 'Restarted application did not exit cleanly.'}
Write-Output 'Settings workflows passed: export/privacy, import cancel/approval, reset decline/approval, persistence after restart.'
Write-Output 'Single-instance workflows passed: duplicate launch activated existing window; distinct data directory launched separately and was stopped by test cleanup.'
Write-Output ('UI smoke passed: '+$results.Count+' reference routes; exit code '+$p.ExitCode)
