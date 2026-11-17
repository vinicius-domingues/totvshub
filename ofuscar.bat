@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

echo ========================================================
echo   TOTVSHub - Ofuscador e Protecao Anti-Engenharia Reversa
echo ========================================================

if not exist "TOTVSHub.exe" (
    echo [ERRO] TOTVSHub.exe nao encontrado. Execute compilar.bat primeiro.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

set "OBFUSCAR_DIR=%~dp0tools\obfuscar"
set "OBFUSCAR_EXE=%OBFUSCAR_DIR%\Obfuscar.Console.exe"

if exist "%OBFUSCAR_EXE%" goto :run_obfuscar

echo [INFO] Baixando pacote portatil oficial do Obfuscar...
if not exist "%OBFUSCAR_DIR%" mkdir "%OBFUSCAR_DIR%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$pkgUrl = 'https://www.nuget.org/api/v2/package/Obfuscar/2.2.38'; $zipPath = Join-Path '%OBFUSCAR_DIR%' 'obfuscar.zip'; Invoke-WebRequest -Uri $pkgUrl -OutFile $zipPath; Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, '%OBFUSCAR_DIR%'); Remove-Item $zipPath -Force"

if exist "%OBFUSCAR_DIR%\tools\Obfuscar.Console.exe" (
    copy /Y "%OBFUSCAR_DIR%\tools\Obfuscar.Console.exe" "%OBFUSCAR_EXE%" >nul
)

if not exist "%OBFUSCAR_EXE%" (
    echo [AVISO] Nao foi possivel baixar o Obfuscar automaticamente.
    echo Baixe manualmente em https://github.com/obfuscar/obfuscar e coloque em tools\obfuscar\
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

:run_obfuscar
if not exist "Dist_Protected" mkdir "Dist_Protected"

echo [EXECUTANDO] Ofuscando strings, renomeando metodos e protegendo fluxo...
"%OBFUSCAR_EXE%" obfuscar.xml

if !errorlevel! neq 0 (
    echo.
    echo [ERRO] Falha durante a ofuscacao.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

if not exist "Dist_Protected\TOTVSHub.exe" (
    echo.
    echo [ERRO] Dist_Protected\TOTVSHub.exe nao foi gerado.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

echo.
echo [SUCESSO] Binario protegido gerado com sucesso em:
echo   Dist_Protected\TOTVSHub.exe
echo.

if "%NOPAUSE%"=="" pause
exit /b 0
