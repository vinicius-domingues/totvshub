@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "NOPAUSE="
set "PASSTHRU="

for %%a in (%*) do (
    if /i "%%~a"=="/nopause" (
        set "NOPAUSE=1"
    ) else (
        if defined PASSTHRU (
            set "PASSTHRU=!PASSTHRU! %%a"
        ) else (
            set "PASSTHRU=%%a"
        )
    )
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0assinar.ps1" !PASSTHRU!
set "EXIT_CODE=!errorlevel!"

if "%NOPAUSE%"=="" pause
exit /b !EXIT_CODE!
