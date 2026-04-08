@echo off
REM HyperBoost X - Build Python Backend Executable
REM Uses PyInstaller to bundle the Python backend into a single .exe.

cd /d "%~dp0app"
echo Checking Python installation...
set "PYTHON_EXE=%CD%\venv\Scripts\python.exe"
if exist "%PYTHON_EXE%" goto python_ready
set "PYTHON_EXE=python"

:python_ready
"%PYTHON_EXE%" --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH.
    echo Please install Python 3.8+ and try again.
    pause
    exit /b 1
)

echo Ensuring PyInstaller is installed...
"%PYTHON_EXE%" -m pip show pyinstaller >nul 2>&1
if errorlevel 1 (
    echo Installing PyInstaller...
    "%PYTHON_EXE%" -m pip install pyinstaller --quiet
    if errorlevel 1 (
        echo ERROR: Failed to install PyInstaller.
        pause
        exit /b 1
    )
)

echo Building backend executable with PyInstaller...
set "PYI_DIST=%~dp0release\pyinstaller\dist"
set "PYI_WORK=%~dp0release\pyinstaller\build"
set "PYI_SPEC=%~dp0release\pyinstaller\spec"
if exist "%PYI_DIST%" rmdir /s /q "%PYI_DIST%"
if exist "%PYI_WORK%" rmdir /s /q "%PYI_WORK%"
if exist "%PYI_SPEC%" rmdir /s /q "%PYI_SPEC%"
mkdir "%PYI_DIST%" >nul 2>&1
mkdir "%PYI_WORK%" >nul 2>&1
mkdir "%PYI_SPEC%" >nul 2>&1
"%PYTHON_EXE%" -m PyInstaller --clean --noconfirm --onefile --name hyperboost_backend --distpath "%PYI_DIST%" --workpath "%PYI_WORK%" --specpath "%PYI_SPEC%" --add-data "data;data" --hidden-import flask_sock --hidden-import wmi --hidden-import psutil backend_server.py
if errorlevel 1 (
    echo ERROR: PyInstaller build failed.
    pause
    exit /b 1
)

if not exist "%PYI_DIST%\hyperboost_backend.exe" (
    echo ERROR: Expected output %PYI_DIST%\hyperboost_backend.exe not found.
    pause
    exit /b 1
)

mkdir "%~dp0release\backend" >nul 2>&1
copy /y "%PYI_DIST%\hyperboost_backend.exe" "%~dp0release\backend\" >nul

echo.
echo Backend executable created successfully.
echo Output copied to: "%~dp0release\backend\hyperboost_backend.exe"
pause
exit /b 0
