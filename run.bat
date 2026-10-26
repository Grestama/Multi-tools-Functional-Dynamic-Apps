@echo off
title DynamicTopBar - Dev Runner
cd /d "%~dp0"

echo ========================================================
echo    DynamicTopBar - Menutup instance lama ^& menjalankan...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command "Stop-Process -Name 'DynamicTopBar.App' -Force -ErrorAction SilentlyContinue"

dotnet run --project "src\DynamicTopBar.App\DynamicTopBar.App.csproj" -p:Platform=x64

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================================
    echo  [ERROR] Aplikasi berhenti atau gagal kompilasi!
    echo ========================================================
    echo Tekan tombol apa saja untuk menutup jendela ini...
    pause >nul
)
