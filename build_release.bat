@echo off
REM HyperBoost X - Build Release Executable
REM Publishes the WPF frontend as a self-contained Windows runtime folder.

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
set "publishDir=%~dp0wpf\bin\Release\net8.0-windows\win-x64\publish"
if exist "%publishDir%" rmdir /s /q "%publishDir%"
pushd "%~dp0wpf"
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false
if errorlevel 1 (
    echo.
    echo ERROR: Publish failed.
    pause
    popd
    exit /b 1
)

if not exist "%publishDir%" (
    echo.
    echo ERROR: Publish output was not found.
    pause
    popd
    exit /b 1
)

if exist "%~dp0release\wpf" rmdir /s /q "%~dp0release\wpf"
mkdir "%~dp0release\wpf" >nul 2>&1
xcopy "%publishDir%\*" "%~dp0release\wpf\" /y /q

echo.
echo Release built successfully.
echo Output copied to: "%~dp0release\wpf\"
pause
popd
exit /b 0
