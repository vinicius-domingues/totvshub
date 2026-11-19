@echo off
setlocal
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0gerar_release.ps1"
set "EXIT_CODE=%errorlevel%"
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" goto :done
)
pause
:done
exit /b %EXIT_CODE%
