@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0gerar_release.ps1"
set "EXIT_CODE=!errorlevel!"

if "%NOPAUSE%"=="" pause
exit /b !EXIT_CODE!
