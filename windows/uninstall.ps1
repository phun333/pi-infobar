# Completely remove Pi Stats for the current user.
# Leaves your Pi agent logs (~/.pi/agent) untouched.

$ErrorActionPreference = "SilentlyContinue"

$installDir = Join-Path $env:LOCALAPPDATA "PiStats"
$runKey     = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$shortcut   = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Pi Stats.lnk"
$settings   = Join-Path $env:APPDATA "PiStats"
$home       = [Environment]::GetFolderPath("UserProfile")

Write-Host "> Stopping PiStats..." -ForegroundColor Cyan
Get-Process PiStats | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host "> Removing auto-start, files, shortcut, settings, caches..." -ForegroundColor Cyan
Remove-ItemProperty -Path $runKey -Name "PiStats"
Remove-Item -Recurse -Force $installDir
Remove-Item -Force $shortcut
Remove-Item -Recurse -Force $settings
Remove-Item -Force (Join-Path $home ".pi\pi-infobar-windows-cache.json")
Remove-Item -Force (Join-Path $home ".pi\pi-infobar-windows-remote-cache.json")
Remove-Item -Recurse -Force (Join-Path $home ".pi\remote-agent-sessions")

Write-Host "OK Pi Stats fully removed. (Your ~/.pi/agent logs were left alone.)" -ForegroundColor Green
