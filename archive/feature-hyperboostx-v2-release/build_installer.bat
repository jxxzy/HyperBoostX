@echo off
REM HyperBoostX - Build Windows Installer
REM Requires NSIS installed and makensis available in PATH.

cd /d "%~dp0"
echo Building Windows installer...

if not exist "release\package" (
    echo ERROR: release\package directory not found.
    echo Run package_release.bat first to assemble the final release files.
    pause
    exit /b 1
)

where makensis >nul 2>&1
if errorlevel 1 (
    if exist "C:\Program Files (x86)\NSIS\makensis.exe" (
        set "MAKENSIS=C:\Program Files (x86)\NSIS\makensis.exe"
    ) else if exist "C:\Program Files\NSIS\makensis.exe" (
        set "MAKENSIS=C:\Program Files\NSIS\makensis.exe"
    ) else (
        echo ERROR: makensis not found in PATH.
        echo Install NSIS from https://nsis.sourceforge.io/Download and try again.
        pause
        exit /b 1
    )
)

if not defined MAKENSIS (
    set "MAKENSIS=makensis"
)

echo Running NSIS...
"%MAKENSIS%" "HyperBoostXInstaller.nsi"
if errorlevel 1 (
    echo ERROR: NSIS build failed.
    pause
    exit /b 1
)

echo.
echo Installer created successfully: "%~dp0HyperBoostXInstaller.exe"
pause
exit /b 0
