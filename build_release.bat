@echo off
REM HyperBoost X - Build Release Executable
REM Publishes the WPF frontend as a self-contained Windows executable.

cd /d "%~dp0"
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Publishing WPF release...
cd /d "%~dp0wpf"
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true
if errorlevel 1 (
    echo.
    echo ERROR: Publish failed.
    pause
    exit /b 1
)

set "publishDir=%~dp0wpf\bin\Release\net8.0-windows\win-x64\publish"
if not exist "%publishDir%" (
    echo.
    echo ERROR: Publish output was not found.
    pause
    exit /b 1
)

mkdir "%~dp0release\wpf" >nul 2>&1
xcopy "%publishDir%\*" "%~dp0release\wpf\" /y /q

echo.
echo Release built successfully.
echo Output copied to: "%~dp0release\wpf\"
pause
exit /b 0