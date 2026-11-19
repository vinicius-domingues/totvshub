@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0.."
pushd "%ROOT%"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

echo ========================================================
echo       TOTVSHub - Compilador de Instalador Inno Setup     
echo ========================================================

set "ISCC="

if exist "D:\Inno Setup 7\ISCC.exe" set "ISCC=D:\Inno Setup 7\ISCC.exe"
if exist "D:\Inno Setup 6\ISCC.exe" set "ISCC=D:\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 7\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"

if "!ISCC!"=="" (
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1" /v InstallLocation 2^>nul') do (
        if exist "%%~bISCC.exe" set "ISCC=%%~bISCC.exe"
        if exist "%%~b\ISCC.exe" set "ISCC=%%~b\ISCC.exe"
    )
)

if "!ISCC!"=="" (
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1" /v InstallLocation 2^>nul') do (
        if exist "%%~bISCC.exe" set "ISCC=%%~bISCC.exe"
        if exist "%%~b\ISCC.exe" set "ISCC=%%~b\ISCC.exe"
    )
)

if "!ISCC!"=="" (
    for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1" /v InstallLocation 2^>nul') do (
        if exist "%%~bISCC.exe" set "ISCC=%%~bISCC.exe"
        if exist "%%~b\ISCC.exe" set "ISCC=%%~b\ISCC.exe"
    )
)

if "!ISCC!"=="" (
    for /f "tokens=*" %%i in ('where ISCC.exe 2^>nul') do (
        set "ISCC=%%i"
    )
)

if "!ISCC!"=="" (
    echo [ERRO] Compilador Inno Setup nao encontrado.
    popd
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

set "SETUP_VER=2.5.0"
for /f "tokens=2 delims=""" %%v in ('findstr /i "MyAppVersion" "%~dp0installer.iss"') do set "SETUP_VER=%%v"
set "SETUP_NAME=TOTVSHub_Setup_v!SETUP_VER!.exe"

echo [INFO] Compilador encontrado: !ISCC!
echo [EXECUTANDO] Gerando instalador !SETUP_NAME!...
echo.

if not exist "Output" mkdir "Output"

"!ISCC!" "%~dp0installer.iss"

if !errorlevel! neq 0 (
    echo.
    echo [ERRO] Falha ao compilar o instalador Inno Setup.
    popd
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

if not exist "Output\!SETUP_NAME!" (
    echo.
    echo [ERRO] Arquivo Output\!SETUP_NAME! nao foi encontrado apos a compilacao.
    popd
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

echo.
echo ========================================================
echo [SUCESSO] Instalador gerado com sucesso em:
echo   Output\!SETUP_NAME!
echo ========================================================
echo.
echo [ASSINANDO] Assinando instalador com carimbo de tempo...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0assinar.ps1" -TargetExe "Output\!SETUP_NAME!"

set "SIGN_ERR=!errorlevel!"
popd

if !SIGN_ERR! neq 0 (
    echo.
    echo [ERRO] Falha ao assinar digitalmente o instalador.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

echo.
echo ========================================================
echo [CONCLUIDO] Instalador compilado e assinado digitalmente com sucesso!
echo ========================================================
echo.

if "%NOPAUSE%"=="" pause
exit /b 0
