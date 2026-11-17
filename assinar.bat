@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0assinar.ps1" %*
pause
