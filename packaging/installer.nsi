Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
Name "HyperBoostX v3"
OutFile "..\dist\HyperBoostX-v3-Setup-x64.exe"
InstallDir "$LOCALAPPDATA\Programs\HyperBoostX-v3"
InstallDirRegKey HKCU "Software\HyperBoostX-v3" "InstallLocation"
RequestExecutionLevel user
SetCompressor /SOLID lzma
VIProductVersion "3.0.0.0"
VIAddVersionKey "ProductName" "HyperBoostX"
VIAddVersionKey "FileDescription" "HyperBoostX v3 Installer (unsigned development build)"
VIAddVersionKey "FileVersion" "3.0.0"
VIAddVersionKey "LegalCopyright" "MR.4NONY 2026"
!define MUI_ABORTWARNING
!define MUI_ICON "..\src\HyperBoostX.App\Assets\HyperBoostX.ico"
!define MUI_UNICON "..\src\HyperBoostX.App\Assets\HyperBoostX.ico"
!define MUI_WELCOMEPAGE_TEXT "Install HyperBoostX v3 for this Windows account.$\r$\n$\r$\nDiagnostics run locally. Windows changes require preview and approval inside the app.$\r$\n$\r$\nThis unsigned build has not passed all Stable release gates. See RELEASE_STATUS.md for verified scope."
!define MUI_FINISHPAGE_RUN "$INSTDIR\HyperBoostX.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Open HyperBoostX"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

Function .onInit
 ${IfNot} ${RunningX64}
  MessageBox MB_ICONSTOP "This package requires 64-bit Windows."
  Abort
 ${EndIf}
 System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\HyperBoostX.v3.Desktop") p.r0'
 ${If} $0 != 0
  System::Call 'kernel32::CloseHandle(p r0)'
  IfSilent +2
  MessageBox MB_ICONSTOP "Close HyperBoostX before installing or repairing this version."
  SetErrorLevel 2
  Abort
 ${EndIf}
FunctionEnd

Section "HyperBoostX" SEC_MAIN
 SetShellVarContext current
 SetOutPath "$INSTDIR"
 !include "payload-install.nsh"
 WriteUninstaller "$INSTDIR\Uninstall.exe"
 WriteRegStr HKCU "Software\HyperBoostX-v3" "InstallLocation" "$INSTDIR"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "DisplayName" "HyperBoostX v3"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "DisplayVersion" "3.0.0"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "Publisher" "MR.4NONY"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "UninstallString" '"$INSTDIR\Uninstall.exe"'
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "InstallLocation" "$INSTDIR"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "DisplayIcon" "$INSTDIR\HyperBoostX.exe"
 WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "NoModify" 1
 WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3" "NoRepair" 1
 CreateDirectory "$SMPROGRAMS\HyperBoostX v3"
 CreateShortcut "$SMPROGRAMS\HyperBoostX v3\HyperBoostX.lnk" "$INSTDIR\HyperBoostX.exe"
 CreateShortcut "$SMPROGRAMS\HyperBoostX v3\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
 CreateShortcut "$DESKTOP\HyperBoostX v3.lnk" "$INSTDIR\HyperBoostX.exe"
SectionEnd

Function un.onInit
 System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\HyperBoostX.v3.Desktop") p.r0'
 ${If} $0 != 0
  System::Call 'kernel32::CloseHandle(p r0)'
  IfSilent +2
  MessageBox MB_ICONSTOP "Close HyperBoostX before uninstalling. Use Restore first if you want to review Windows changes."
  SetErrorLevel 2
  Abort
 ${EndIf}
 IfSilent done
 MessageBox MB_OKCANCEL "Windows settings will be kept. To restore managed changes first, click Cancel and open HyperBoostX > Restore.$\r$\n$\r$\nLocal history and encrypted recovery data will be retained for your safety." IDOK done
 Abort
 done:
FunctionEnd

Section "Uninstall"
 SetShellVarContext current
 !include "payload-uninstall.nsh"
 Delete "$DESKTOP\HyperBoostX v3.lnk"
 Delete "$SMPROGRAMS\HyperBoostX v3\HyperBoostX.lnk"
 Delete "$SMPROGRAMS\HyperBoostX v3\Uninstall.lnk"
 RMDir "$SMPROGRAMS\HyperBoostX v3"
 DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3"
 DeleteRegKey HKCU "Software\HyperBoostX-v3"
 Delete "$INSTDIR\Uninstall.exe"
 RMDir "$INSTDIR"
SectionEnd
