# Install Pi Stats for the current user.
#
#   .\install.ps1                # build, install to %LOCALAPPDATA%\PiStats, auto-start, run
#   .\install.ps1 -NoAutoStart   # install but don't start with Windows
#   .\install.ps1 -NoBuild       # use the existing windows\dist\PiStats.exe
#
# Per-user install: no admin rights needed. Uninstall with .\uninstall.ps1

param(
    [switch]$NoAutoStart,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

$installDir = Join-Path $env:LOCALAPPDATA "PiStats"
$targetExe  = Join-Path $installDir "PiStats.exe"
$sourceExe  = Join-Path $PSScriptRoot "dist\PiStats.exe"
$runKey     = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"

# 1) Build the single-file exe (unless told to skip).
if (-not $NoBuild) {
    Write-Host "> Building PiStats.exe..." -ForegroundColor Cyan
    & "$PSScriptRoot\build.ps1"
}
if (-not (Test-Path $sourceExe)) {
    Write-Host "X  $sourceExe not found. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

# 2) Stop any running instance.
Get-Process PiStats -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# 3) Copy to the per-user install folder.
Write-Host "> Installing to $installDir" -ForegroundColor Cyan
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Copy-Item $sourceExe $targetExe -Force

# 4) Start Menu shortcut.
$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$shortcut  = Join-Path $startMenu "Pi Stats.lnk"
$ws = New-Object -ComObject WScript.Shell
$lnk = $ws.CreateShortcut($shortcut)
$lnk.TargetPath = $targetExe
$lnk.WorkingDirectory = $installDir
$lnk.Description = "Pi Stats"
$lnk.Save()
Write-Host "> Start Menu shortcut created" -ForegroundColor Cyan

# 5) Start with Windows (Run key -> installed exe).
if (-not $NoAutoStart) {
    New-ItemProperty -Path $runKey -Name "PiStats" -Value "`"$targetExe`"" -PropertyType String -Force | Out-Null
    Write-Host "> Auto-start enabled (runs at sign-in)" -ForegroundColor Cyan
}

# 6) Launch it now.
Start-Process $targetExe
Write-Host "OK Installed and running. Look for the pi icon in the system tray." -ForegroundColor Green
