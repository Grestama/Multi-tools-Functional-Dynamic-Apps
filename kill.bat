@echo off
title DynamicTopBar - Process Killer
cd /d "%~dp0"

echo Menghentikan seluruh proses DynamicTopBar yang berjalan...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Stop-Process -Name 'DynamicTopBar.App' -Force -ErrorAction SilentlyContinue; Write-Host 'Semua proses DynamicTopBar berhasil dihentikan.' -ForegroundColor Green"

timeout /t 2 >nul
