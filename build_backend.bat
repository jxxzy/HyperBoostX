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
"%PYTHON_EXE%" -m PyInstaller --clean --noconfirm --onefile --name hyperboost_backend --add-data "data;data" --hidden-import flask_sock --hidden-import wmi --hidden-import psutil backend_server.py
if errorlevel 1 (
    echo ERROR: PyInstaller build failed.
    pause
    exit /b 1
)

if not exist "dist\hyperboost_backend.exe" (
    echo ERROR: Expected output dist\hyperboost_backend.exe not found.
    pause
    exit /b 1
)

mkdir "%~dp0release\backend" >nul 2>&1
copy /y "dist\hyperboost_backend.exe" "%~dp0release\backend\" >nul

echo.
echo Backend executable created successfully.
echo Output copied to: "%~dp0release\backend\hyperboost_backend.exe"
pause
exit /b 0
