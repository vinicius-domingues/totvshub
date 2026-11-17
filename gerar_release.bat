@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0gerar_release.ps1"
pause
