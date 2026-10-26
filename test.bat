@echo off
title DynamicTopBar - Unit Test Runner
cd /d "%~dp0"

echo ========================================================
echo    Menjalankan Seluruh Unit Test (291 Tests, 27 Fixtures)
echo ========================================================
echo.

dotnet test DynamicTopBar.sln

echo.
echo ========================================================
echo Pengujian selesai. Tekan tombol apa saja untuk keluar...
echo ========================================================
pause >nul
