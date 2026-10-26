@echo off
title DynamicTopBar - Production Release Publisher
cd /d "%~dp0"

echo ========================================================
echo    DynamicTopBar - Tahap 4 Production Release Publish
echo ========================================================
echo.

set CONFIG=Release
set PLATFORM=x64
set OUTPUT_DIR=publish\win-x64

echo [1/3] Running automated unit tests...
dotnet test DynamicTopBar.sln -c %CONFIG%
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Test suite failed! Publish aborted.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Cleaning previous publish directory...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
)

echo.
echo [3/3] Publishing DynamicTopBar.App (%CONFIG% - %PLATFORM%)...
dotnet publish src\DynamicTopBar.App\DynamicTopBar.App.csproj -c %CONFIG% -p:Platform=%PLATFORM% --self-contained false -o "%OUTPUT_DIR%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo  [SUCCESS] Production publish complete!
    echo  Location: %~dp0%OUTPUT_DIR%
    echo ========================================================
) else (
    echo.
    echo ========================================================
    echo  [ERROR] Publish failed!
    echo ========================================================
    pause
)
