# Build a distributable Pi Stats for Windows.
#
#   .\build.ps1                 # self-contained single-file .exe (win-x64)
#   .\build.ps1 -FrameworkDependent   # small .exe, needs .NET 8 Desktop Runtime
#
# Output lands in windows\dist\.

param(
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

$proj = "PiStats\PiStats.csproj"
$dist = "dist"
$rid  = "win-x64"

Write-Host "> Cleaning..." -ForegroundColor Cyan
if (Test-Path $dist) { Remove-Item -Recurse -Force $dist }
New-Item -ItemType Directory -Path $dist | Out-Null

# Ensure the app icon exists (generate from the π logo if missing).
if (-not (Test-Path "PiStats\Assets\AppIcon.ico")) {
    Write-Host "> Generating app icon..." -ForegroundColor Cyan
    dotnet run --project $proj -c Debug -- --makeicon "PiStats\Assets\AppIcon.ico"
}

if ($FrameworkDependent) {
    Write-Host "> Publishing framework-dependent single file..." -ForegroundColor Cyan
    dotnet publish $proj -c Release -r $rid --self-contained false `
        -p:PublishSingleFile=true `
        -o $dist
} else {
    Write-Host "> Publishing self-contained single file (win-x64)..." -ForegroundColor Cyan
    dotnet publish $proj -c Release -r $rid `
        -p:PublishSingleFile=true `
        -o $dist
}

# Keep only the .exe (single-file bundles everything else).
Get-ChildItem $dist -Exclude "PiStats.exe" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$exe = Join-Path $dist "PiStats.exe"
if (Test-Path $exe) {
    $size = "{0:N1} MB" -f ((Get-Item $exe).Length / 1MB)
    Write-Host "OK Built $exe ($size)" -ForegroundColor Green
} else {
    Write-Host "X  Build failed - PiStats.exe not found." -ForegroundColor Red
    exit 1
}
