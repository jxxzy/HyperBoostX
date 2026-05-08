!include "MUI2.nsh"
!include "LogicLib.nsh"

Name "HyperBoost X"
OutFile "HyperBoostXInstaller.exe"
InstallDir "$PROGRAMFILES64\HyperBoost X"
RequestExecutionLevel admin
VIProductVersion "1.2.6.0"
VIAddVersionKey "ProductName" "HyperBoost X"
VIAddVersionKey "CompanyName" "MR.4NONY"
VIAddVersionKey "FileDescription" "HyperBoost X Installer"
VIAddVersionKey "FileVersion" "1.2.6"
VIAddVersionKey "ProductVersion" "1.2.6"
VIAddVersionKey "LegalCopyright" "Copyright (c) MR.4NONY"
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

Var ExistingInstallDir
Var ExistingUninstaller

Function UninstallPreviousVersion
  StrCpy $ExistingInstallDir ""
  StrCpy $ExistingUninstaller ""

  ReadRegStr $ExistingInstallDir HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "InstallLocation"
  ${If} $ExistingInstallDir == ""
    StrCpy $ExistingInstallDir "$INSTDIR"
  ${EndIf}

  StrCpy $ExistingUninstaller "$ExistingInstallDir\Uninstall.exe"
  IfFileExists "$ExistingUninstaller" 0 done

  MessageBox MB_ICONINFORMATION|MB_OK "HyperBoost X versi lama terdeteksi.$\r$\n$\r$\nInstaller akan menghapus aplikasi lama terlebih dulu, lalu memasang versi terbaru.$\r$\n$\r$\nConfig, backup, dan setting user di %LocalAppData% akan tetap disimpan."
  DetailPrint "Previous HyperBoost X installation detected."
  DetailPrint "Removing old application files from $ExistingInstallDir and keeping user config in %LocalAppData%."
  ExecWait '"$ExistingUninstaller" /S _?=$ExistingInstallDir'

done:
FunctionEnd

Section "Install"
  SetShellVarContext all
  Call UninstallPreviousVersion
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
  File /r "release\package\wpf\*"

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
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "DisplayVersion" "1.2.6"
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

  DetailPrint "Removing installed application files only."
  DetailPrint "User config, backups, logs, and automation state under %LocalAppData%\\HyperBoost X are preserved."

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
