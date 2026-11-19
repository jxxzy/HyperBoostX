param([string]$Executable,[string]$DataDirectory,[string]$OutputDirectory,[string]$NavigationCatalog=(Join-Path $PSScriptRoot '..\src\HyperBoostX.App\Assets\navigation.json'))
$ErrorActionPreference='Stop'
trap {
 $failure=$_
 if($other -and !$other.HasExited){Stop-Process -Id $other.Id -Force}
 if($second -and !$second.HasExited){Stop-Process -Id $second.Id -Force}
 if($p -and !$p.HasExited){Stop-Process -Id $p.Id -Force}
 Write-Error -ErrorRecord $failure -ErrorAction Continue
 throw $failure
}
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class HbxDialogNative { [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); }'
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
function Get-GridRowCount([string]$AutomationId){
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$AutomationId)
 $grid=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $pattern=$null
 if(!$grid -or !$grid.TryGetCurrentPattern([System.Windows.Automation.GridPattern]::Pattern,[ref]$pattern)){return 0}
 return $pattern.Current.RowCount
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
function Set-SettingsFileDialogPath([string]$AutomationId,[string]$Path){
 Invoke-ControlById $AutomationId
 $dialog=Get-SettingsFileDialog $AutomationId
 [HbxDialogNative]::SetForegroundWindow([IntPtr]$dialog.Current.NativeWindowHandle) | Out-Null
 Start-Sleep -Milliseconds 150
 [System.Windows.Forms.SendKeys]::SendWait('%n')
 [System.Windows.Forms.SendKeys]::SendWait('^a')
 [System.Windows.Forms.SendKeys]::SendWait($Path)
 [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
}
function Cancel-SettingsFileDialog([string]$AutomationId){
 Invoke-ControlById $AutomationId
 $dialog=Get-SettingsFileDialog $AutomationId
 [HbxDialogNative]::SetForegroundWindow([IntPtr]$dialog.Current.NativeWindowHandle) | Out-Null
 [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
 Start-Sleep -Milliseconds 300
}
function Invoke-SettingsConfirmation([bool]$Accept){
 $expected=if($Accept){@('yes','ya')}else{@('no','tidak')}
 $automationId=if($Accept){'6'}else{'7'}
 $windowCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Window)
 $buttonCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$automationId)
 $deadline=[DateTime]::UtcNow.AddSeconds(10)
 while([DateTime]::UtcNow -lt $deadline){
  $dialogs=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$windowCondition)
  foreach($dialog in $dialogs){
   $button=$dialog.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$buttonCondition)
   if($button -and $expected -contains $button.Current.Name.Trim().ToLowerInvariant()){
    $handle=[IntPtr]$dialog.Current.NativeWindowHandle
    if($handle -eq [IntPtr]::Zero){throw 'Settings confirmation dialog has no native handle.'}
    [HbxDialogNative]::SendMessage($handle,0x0111,[IntPtr][int]$automationId,[IntPtr]::Zero) | Out-Null
    return
   }
  }
  Start-Sleep -Milliseconds 100
 }
 throw 'Settings confirmation dialog button was not found.'
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
$routes=Get-Content -LiteralPath $NavigationCatalog -Raw | ConvertFrom-Json
if($routes.Count -ne 73){throw "Expected 73 reference routes, found $($routes.Count)."}
$results=@()
$settingsFlowEvidence=$null
$searchCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'SearchFeatures')
$search=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$searchCondition)
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
 $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($page)
 Start-Sleep -Milliseconds 100
 $condition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,$page)
 $all=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)
 $selected=$false
 foreach($item in $all){
  $pattern=$null
  if($item.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$pattern)){$pattern.Select();$selected=$true;break}
 }
 $results += [pscustomobject]@{Page=$page;Selected=$selected}
 if(!$selected){throw "Navigation failed: $page"}
 Start-Sleep -Milliseconds 80
 $titleCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty,'CurrentPageTitle')
 $pageTitle=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$titleCondition)
 if(!$pageTitle -or $pageTitle.Current.Name -ne $page){throw "Route selection did not load the expected page: $page"}
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
  if($duplicateRows -le 0 -or $largeRows -ne 2){throw "File analysis did not render expected duplicate/large-file rows: duplicate=$duplicateRows large=$largeRows"}
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
  if(!$measured){throw 'App Load measurement did not render a measured process-creation result.'}
 }
 if($page -eq 'RGB Software Detector'){
  Invoke-ControlById 'ScanButton';$deadline=[DateTime]::UtcNow.AddSeconds(30);$rgbRows=0
  while([DateTime]::UtcNow -lt $deadline){$rgbRows=Get-GridRowCount 'ObservationFacts';if($rgbRows -gt 0){break};Start-Sleep -Milliseconds 150}
  if($rgbRows -le 0){throw 'RGB Software Detector returned no visible observation or explicit no-match result.'}
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
  $deadline=[DateTime]::UtcNow.AddSeconds(20);$fpsResult=$null
  $fpsCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Average FPS')
  while([DateTime]::UtcNow -lt $deadline){$fpsResult=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$fpsCondition);if($fpsResult){break};Start-Sleep -Milliseconds 150}
  if(!$fpsResult){throw 'Stutter Analyzer did not render calculated Average FPS.'}
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
  Cancel-SettingsFileDialog 'ImportSettingsButton'
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
  Invoke-ControlById 'ToggleSidebarButton'
  Start-Sleep -Milliseconds 200
  Save-WindowScreenshot (Join-Path $OutputDirectory 'dashboard-compact.png')
  Invoke-ControlById 'ToggleSidebarButton'
  Select-ComboOptionById 'ThemeSelection' 'Light'
  Save-WindowScreenshot (Join-Path $OutputDirectory 'dashboard-light.png')
  Select-ComboOptionById 'ThemeSelection' 'Dark'
 }
}
if($results.Count -ne 73 -or @($results | Where-Object {!$_.Selected}).Count -gt 0){throw 'Not all 73 reference routes were selected by UI Automation.'}
$search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Settings')
$settingsCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty,'Settings')
$settingsItems=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,$settingsCondition)
$settingsSelected=$false
foreach($item in $settingsItems){$pattern=$null;if($item.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern,[ref]$pattern)){$pattern.Select();$settingsSelected=$true;break}}
if(!$settingsSelected){throw 'Settings route could not be restored for the final screenshot.'}
$search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('')
Start-Sleep -Milliseconds 150
$results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'ui-navigation.json') -Encoding UTF8
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
