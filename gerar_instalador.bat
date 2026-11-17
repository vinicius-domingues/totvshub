@echo off
setlocal enabledelayedexpansion
echo ========================================================
echo       TOTVSHub - Compilador de Instalador Inno Setup     
echo ========================================================

set "ISCC="

if exist "%ProgramFiles%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 7\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 7\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"

if "!ISCC!"=="" (
    for /f "tokens=*" %%i in ('where ISCC.exe 2^>nul') do (
        set "ISCC=%%i"
    )
)

if "!ISCC!"=="" (
    echo [ERRO] Compilador Inno Setup nao encontrado.
    pause
    exit /b 1
)

echo [INFO] Compilador encontrado: !ISCC!
echo [EXECUTANDO] Gerando instalador TOTVSHub_Setup_v2.4.0.exe...
echo.

if not exist "Output" mkdir "Output"

"!ISCC!" installer.iss

if !errorlevel!==0 (
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
