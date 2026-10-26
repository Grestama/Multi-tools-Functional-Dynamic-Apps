# DynamicTopBar - Production Release Publisher (PowerShell)
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputDir = "publish/win-x64",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   DynamicTopBar - Production Release Publisher" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

if (-not $SkipTests) {
    Write-Host "`n[1/3] Executing automated test suite..." -ForegroundColor Yellow
    dotnet test "$rootDir/DynamicTopBar.sln" -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Test suite failed! Aborting release publish."
        exit $LASTEXITCODE
    }
} else {
    Write-Host "`n[1/3] Skipping tests as requested." -ForegroundColor Gray
}

Write-Host "`n[2/3] Preparing output directory: $OutputDir..." -ForegroundColor Yellow
$targetPath = Join-Path $rootDir $OutputDir
if (Test-Path $targetPath) {
    Remove-Item -Recurse -Force $targetPath
}
New-Item -ItemType Directory -Path $targetPath -Force | Out-Null

Write-Host "`n[3/3] Publishing DynamicTopBar.App ($Configuration, $Platform)..." -ForegroundColor Yellow
dotnet publish "$rootDir/src/DynamicTopBar.App/DynamicTopBar.App.csproj" `
    -c $Configuration `
    -p:Platform=$Platform `
    --self-contained false `
    -o $targetPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host " [SUCCESS] Production publish completed successfully!" -ForegroundColor Green
    Write-Host " Artifacts directory: $targetPath" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
} else {
    Write-Error "Publish command failed with exit code $LASTEXITCODE."
}
