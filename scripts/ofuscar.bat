@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0.."
pushd "%ROOT%"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

echo ========================================================
echo   TOTVSHub - Ofuscador e Protecao Anti-Engenharia Reversa
echo ========================================================

if not exist "TOTVSHub.exe" (
    echo [ERRO] TOTVSHub.exe nao encontrado. Execute compilar.bat primeiro.
    popd
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

set "OBFUSCAR_DIR=%ROOT%\tools\obfuscar"
set "OBFUSCAR_EXE=%OBFUSCAR_DIR%\Obfuscar.Console.exe"

if not exist "%OBFUSCAR_EXE%" (
    echo [INFO] Baixando pacote portatil oficial do Obfuscar...
    if not exist "%OBFUSCAR_DIR%" mkdir "%OBFUSCAR_DIR%"
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "$pkgUrl = 'https://www.nuget.org/api/v2/package/Obfuscar/2.2.38'; " ^
        "$zipPath = Join-Path '%OBFUSCAR_DIR%' 'obfuscar.zip'; " ^
        "Invoke-WebRequest -Uri $pkgUrl -OutFile $zipPath; " ^
        "Add-Type -AssemblyName System.IO.Compression.FileSystem; " ^
        "[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, '%OBFUSCAR_DIR%'); " ^
        "Remove-Item $zipPath -Force"
    
    if exist "%OBFUSCAR_DIR%\tools\Obfuscar.Console.exe" (
        copy /Y "%OBFUSCAR_DIR%\tools\Obfuscar.Console.exe" "%OBFUSCAR_EXE%" >nul
    )
)

if not exist "%OBFUSCAR_EXE%" (
    echo [AVISO] Nao foi possivel baixar o Obfuscar automaticamente.
    popd
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

if not exist "Dist_Protected" mkdir "Dist_Protected"

echo [EXECUTANDO] Ofuscando strings, renomeando metodos e protegendo fluxo...
"%OBFUSCAR_EXE%" "%~dp0obfuscar.xml"

set "OBF_ERR=!errorlevel!"
popd

if !OBF_ERR!==0 (
    echo.
    echo [SUCESSO] Binario protegido gerado com sucesso em:
    echo   Dist_Protected\TOTVSHub.exe
    echo.
) else (
    echo [ERRO] Falha durante a ofuscacao.
    if "%NOPAUSE%"=="" pause
    exit /b !OBF_ERR!
)

if "%NOPAUSE%"=="" pause
exit /b 0
