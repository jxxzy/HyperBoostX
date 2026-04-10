@echo off
REM HyperBoost X - Build Launcher Runtime
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
dotnet publish launcher\HyperBoostLauncher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
if errorlevel 1 (
    echo.
    echo ERROR: Launcher publish failed.
    pause
    exit /b 1
)

set "publishDir=%~dp0launcher\bin\Release\net8.0-windows\win-x64"
if not exist "%publishDir%\HyperBoostLauncher.exe" (
    echo.
    echo ERROR: Publish output was not found.
    pause
    exit /b 1
)

mkdir "%~dp0release\launcher" >nul 2>&1
xcopy "%publishDir%\*" "%~dp0release\launcher\" /y /q

echo.
echo Launcher built successfully.
echo Output copied to: "%~dp0release\launcher\"
pause
exit /b 0
