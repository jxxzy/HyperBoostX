@echo off
REM HyperBoostX - Build Launcher Runtime
REM Publishes the internal launcher executable used by installer and portable app.

cd /d "%~dp0"
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH.
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Publishing launcher...
set "releaseLauncherDir=%~dp0release\launcher"
if exist "%releaseLauncherDir%" rmdir /s /q "%releaseLauncherDir%"
mkdir "%releaseLauncherDir%" >nul 2>&1
dotnet publish launcher\HyperBoostLauncher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "%releaseLauncherDir%"
if errorlevel 1 (
    echo.
    echo ERROR: Launcher publish failed.
    pause
    exit /b 1
)

if not exist "%releaseLauncherDir%\HyperBoostLauncher.exe" (
    echo.
    echo ERROR: Publish output was not found.
    pause
    exit /b 1
)

echo.
echo Launcher built successfully.
echo Output copied to: "%releaseLauncherDir%\"
pause
exit /b 0
