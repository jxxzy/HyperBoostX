@echo off
REM HyperBoostX - Quick Start Script
REM This script starts the Python backend server

echo.
echo ╔════════════════════════════════════════════╗
echo ║        HyperBoostX Backend Server         ║
echo ║     Starting Flask REST API on :5000       ║
echo ╚════════════════════════════════════════════╝
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.8+ from https://www.python.org/downloads/
    pause
    exit /b 1
)

REM Navigate to app directory
cd /d "%~dp0"
if not exist "app" (
    echo ERROR: app directory not found
    echo Make sure you're running this script from the HyperBoostX root directory
    pause
    exit /b 1
)

cd app

REM Check if virtual environment exists
if not exist "venv" (
    echo Creating Python virtual environment...
    python -m venv venv
    if errorlevel 1 (
        echo ERROR: Failed to create virtual environment
        pause
        exit /b 1
    )
)

REM Activate virtual environment
call venv\Scripts\activate.bat
if errorlevel 1 (
    echo ERROR: Failed to activate virtual environment
    pause
    exit /b 1
)

REM Install/upgrade dependencies
echo.
echo Installing Python dependencies...
pip install -r requirements.txt --quiet
if errorlevel 1 (
    echo ERROR: Failed to install dependencies
    pause
    exit /b 1
)

REM Start backend server
echo.
echo ✓ Starting HyperBoostX Backend Server
echo   URL: http://127.0.0.1:5000
echo   Press CTRL+C to stop
echo.
echo Waiting for requests...
echo.
python backend_server.py

REM Only pause if not called from master launcher
if "%~1" neq "background" (
    pause
)
