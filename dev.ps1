<#
.SYNOPSIS
    DynamicTopBar - Development & Testing CLI Tool
.DESCRIPTION
    Skrip pembantu untuk mempermudah alur kerja pengembangan, testing, dan trial & error.
.EXAMPLE
    .\dev.ps1           # Menjalankan aplikasi (default)
    .\dev.ps1 run       # Menutup proses lama & menjalankan aplikasi
    .\dev.ps1 test      # Menjalankan seluruh automated unit test suite
    .\dev.ps1 kill      # Menghentikan background process yang aktif
    .\dev.ps1 clean     # Clean build solusi
    .\dev.ps1 publish   # Membuat paket rilis mandiri (standalone .exe)
#>

param (
    [ValidateSet("run", "test", "kill", "clean", "publish", "menu")]
    [string]$Action = "run"
)

$RootDir = $PSScriptRoot
$ProjectFile = Join-Path $RootDir "src\DynamicTopBar.App\DynamicTopBar.App.csproj"
$SolutionFile = Join-Path $RootDir "DynamicTopBar.sln"

function Stop-AppProcess {
    Write-Host "[1/2] Menutup proses DynamicTopBar lama di background..." -ForegroundColor Yellow
    Stop-Process -Name "DynamicTopBar.App" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
}

function Run-App {
    Stop-AppProcess
    Write-Host "[2/2] Mengompilasi dan menjalankan DynamicTopBar (x64)..." -ForegroundColor Cyan
    dotnet run --project $ProjectFile -p:Platform=x64
}

function Run-Tests {
    Write-Host "==> Menjalankan 153 Automated Unit Tests..." -ForegroundColor Magenta
    dotnet test $SolutionFile
}

function Clean-Build {
    Stop-AppProcess
    Write-Host "==> Membersihkan solusi..." -ForegroundColor Yellow
    dotnet clean $SolutionFile
    Write-Host "==> Membangun ulang solusi (x64)..." -ForegroundColor Cyan
    dotnet build $SolutionFile -p:Platform=x64
}

function Publish-App {
    Stop-AppProcess
    Write-Host "==> Mempublikasikan rilis standalone portable (Release x64)..." -ForegroundColor Green
    dotnet publish $ProjectFile -c Release -p:Platform=x64 -r win-x64 --self-contained true
    $PublishDir = Join-Path $RootDir "src\DynamicTopBar.App\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\publish"
    Write-Host "==> Rilis siap di: $PublishDir" -ForegroundColor Green
}

switch ($Action.ToLower()) {
    "run"     { Run-App }
    "test"    { Run-Tests }
    "kill"    { Stop-AppProcess; Write-Host "Proses berhasil dihentikan." -ForegroundColor Green }
    "clean"   { Clean-Build }
    "publish" { Publish-App }
    default   { Run-App }
}
