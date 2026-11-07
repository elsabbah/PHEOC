@echo off
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -NoExit -ExecutionPolicy Bypass -File "%~dp0scripts\Start-Development.ps1"
