@echo off
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..\") do set "REPO_ROOT=%%~fI"
cd /d "%REPO_ROOT%"
REM HyperBoostX - Build Release Executable
REM Publishes the WPF frontend as a self-contained Windows runtime folder.

cd /d "%REPO_ROOT%"
echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Publishing WPF release...
set "publishDir=%REPO_ROOT%\wpf\bin\Release\net8.0-windows\win-x64\publish"
if exist "%publishDir%" rmdir /s /q "%publishDir%"
pushd "%REPO_ROOT%\wpf"
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

if exist "%REPO_ROOT%\release\wpf" rmdir /s /q "%REPO_ROOT%\release\wpf"
mkdir "%REPO_ROOT%\release\wpf" >nul 2>&1
xcopy "%publishDir%\*" "%REPO_ROOT%\release\wpf\" /y /q

echo.
echo Release built successfully.
echo Output copied to: "%REPO_ROOT%\release\wpf\"
pause
popd
exit /b 0
