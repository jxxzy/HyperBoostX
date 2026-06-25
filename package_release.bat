@echo off
REM HyperBoostX - Package Final Release Folder
REM Builds launcher, backend, and WPF UI, then assembles installer and portable layouts.

cd /d "%~dp0"
echo Packaging final release...

call build_backend.bat
if errorlevel 1 (
    echo ERROR: Backend build failed.
    exit /b 1
)

call build_release.bat
if errorlevel 1 (
    echo ERROR: WPF release build failed.
    exit /b 1
)

call build_launcher.bat
if errorlevel 1 (
    echo ERROR: Launcher build failed.
    exit /b 1
)

set "releaseDir=%~dp0release"
set "packageDir=%releaseDir%\package"
set "appDir=%releaseDir%\app"

if exist "%packageDir%" rmdir /s /q "%packageDir%"
if exist "%appDir%" rmdir /s /q "%appDir%"

mkdir "%packageDir%\backend" >nul 2>&1
mkdir "%packageDir%\launcher" >nul 2>&1
mkdir "%packageDir%\wpf" >nul 2>&1
mkdir "%appDir%\runtime\backend" >nul 2>&1
mkdir "%appDir%\runtime\wpf" >nul 2>&1

copy /y "%releaseDir%\backend\hyperboost_backend.exe" "%packageDir%\backend\hyperboost_backend.exe" >nul
copy /y "%releaseDir%\launcher\HyperBoostLauncher.exe" "%packageDir%\launcher\HyperBoostLauncher.exe" >nul
if exist "%releaseDir%\launcher\HyperBoostLauncher.pdb" copy /y "%releaseDir%\launcher\HyperBoostLauncher.pdb" "%packageDir%\launcher\HyperBoostLauncher.pdb" >nul
xcopy "%releaseDir%\wpf\*" "%packageDir%\wpf\" /e /i /y /q >nul

copy /y "%releaseDir%\launcher\HyperBoostLauncher.exe" "%appDir%\HyperBoostX.exe" >nul
copy /y "%releaseDir%\backend\hyperboost_backend.exe" "%appDir%\runtime\backend\hyperboost_backend.exe" >nul
xcopy "%releaseDir%\wpf\*" "%appDir%\runtime\wpf\" /e /i /y /q >nul

echo.
echo Final release package created successfully.
echo Installer assets: "%packageDir%"
echo Portable app: "%appDir%\HyperBoostX.exe"
pause
exit /b 0
