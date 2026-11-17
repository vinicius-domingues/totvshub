@echo off
setlocal
echo ========================================================
echo       TOTVSHub - Compilador de Instalador Inno Setup     
echo ========================================================

set "ISCC="

if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"

if "%ISCC%"=="" (
    where ISCC.exe >nul 2>&1
    if %errorlevel%==0 set "ISCC=ISCC.exe"
)

if "%ISCC%"=="" (
    echo [ERRO] Inno Setup nao encontrado instalado.
    echo Por favor, execute o instalador 'innosetup-7.1.1-x64.exe' baixado na pasta Downloads.
    echo.
    pause
    exit /b 1
)

echo [INFO] Compilador encontrado: %ISCC%
echo [EXECUTANDO] Gerando instalador TOTVSHub_Setup_v2.4.0.exe...
echo.

"%ISCC%" installer.iss

if %errorlevel%==0 (
    echo.
    echo ========================================================
    echo [SUCESSO] Instalador gerado com sucesso em:
    echo   Output\TOTVSHub_Setup_v2.4.0.exe
    echo ========================================================
) else (
    echo.
    echo [ERRO] Falha ao compilar o instalador Inno Setup.
)

pause
