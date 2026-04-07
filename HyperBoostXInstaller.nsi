!include "MUI2.nsh"

Name "HyperBoost X"
OutFile "HyperBoostXInstaller.exe"
InstallDir "$PROGRAMFILES64\HyperBoost X"
RequestExecutionLevel admin
!define MUI_ICON "wpf\Assets\HyperBoostX.ico"
!define MUI_UNICON "wpf\Assets\HyperBoostX.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

Section "Install"
  SetShellVarContext all
  ExecWait 'taskkill /IM HyperBoostLauncher.exe /F'
  ExecWait 'taskkill /IM HyperBoostX.exe /F'
  ExecWait 'taskkill /IM HyperBoostUI.exe /F'
  ExecWait 'taskkill /IM hyperboost_backend.exe /F'

  Delete "$INSTDIR\HyperBoostX.exe"
  RMDir /r "$INSTDIR\runtime"
  RMDir /r "$INSTDIR\launcher"
  RMDir /r "$INSTDIR\wpf"
  RMDir /r "$INSTDIR\backend"

  SetOutPath "$INSTDIR"
  File /oname=HyperBoostX.exe "release\package\launcher\HyperBoostLauncher.exe"

  SetOutPath "$INSTDIR\runtime\wpf"
  File /oname=HyperBoostUI.exe "release\package\wpf\HyperBoostX.exe"
  File /oname=HyperBoostUI.pdb "release\package\wpf\HyperBoostX.pdb"

  SetOutPath "$INSTDIR\runtime\backend"
  File /r "release\package\backend\*"

  CreateDirectory "$SMPROGRAMS\HyperBoost X"
  CreateShortCut "$DESKTOP\HyperBoost X.lnk" "$INSTDIR\HyperBoostX.exe"
  CreateShortCut "$SMPROGRAMS\HyperBoost X\HyperBoost X.lnk" "$INSTDIR\HyperBoostX.exe"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "DisplayName" "HyperBoost X"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "DisplayIcon" "$INSTDIR\HyperBoostX.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "Publisher" "MR.4NONY"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "DisplayVersion" "1.1.0-beta"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  ExecWait 'taskkill /IM HyperBoostLauncher.exe /F'
  ExecWait 'taskkill /IM HyperBoostX.exe /F'
  ExecWait 'taskkill /IM HyperBoostUI.exe /F'
  ExecWait 'taskkill /IM hyperboost_backend.exe /F'

  Delete "$DESKTOP\HyperBoost X.lnk"
  Delete "$SMPROGRAMS\HyperBoost X\HyperBoost X.lnk"
  RMDir /r "$SMPROGRAMS\HyperBoost X"
  Delete "$INSTDIR\HyperBoostX.exe"
  RMDir /r "$INSTDIR\runtime"
  RMDir /r "$INSTDIR\launcher"
  RMDir /r "$INSTDIR\wpf"
  RMDir /r "$INSTDIR\backend"
  Delete "$INSTDIR\Uninstall.exe"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX"
  RMDir /r "$INSTDIR"
SectionEnd
