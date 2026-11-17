@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

echo ========================================================
echo         TOTVSHub - Pipeline de Build Automatizado        
echo ========================================================
echo.
echo Etapas do pipeline:
echo   1. Compilacao (scripts\compilar.bat)
echo   2. Ofuscacao de codigo (scripts\ofuscar.bat)
echo   3. Assinatura Authenticode (scripts\assinar.bat)
echo   4. Geracao e assinatura do instalador (scripts\gerar_instalador.bat)
echo   5. Geracao de notas de release e checksums (scripts\gerar_release.bat)
echo ========================================================
echo.

:: -----------------------------------------------------------------------------
:: ETAPA 1: COMPILACAO
:: -----------------------------------------------------------------------------
echo [ETAPA 1/5] Iniciando compilacao do executavel C#...
call "%~dp0scripts\compilar.bat" /nopause
if !errorlevel! neq 0 (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 1: Falha ao compilar TOTVSHub.exe.
    goto :pipeline_error
)
if not exist "%~dp0TOTVSHub.exe" (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 1: Arquivo TOTVSHub.exe nao encontrado apos compilacao.
    goto :pipeline_error
)
echo [OK] Etapa 1 concluida com sucesso. TOTVSHub.exe gerado.
echo.

:: -----------------------------------------------------------------------------
:: ETAPA 2: OFUSCACAO
:: -----------------------------------------------------------------------------
echo [ETAPA 2/5] Ofuscando codigo e aplicando protecao anti-reversa...
call "%~dp0scripts\ofuscar.bat" /nopause
if !errorlevel! neq 0 (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 2: Falha durante ofuscacao via Obfuscar.
    goto :pipeline_error
)
if not exist "%~dp0Dist_Protected\TOTVSHub.exe" (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 2: Arquivo Dist_Protected\TOTVSHub.exe nao encontrado.
    goto :pipeline_error
)
echo [OK] Etapa 2 concluida com sucesso. Dist_Protected\TOTVSHub.exe gerado.
echo.

:: -----------------------------------------------------------------------------
:: ETAPA 3: ASSINATURA DO EXECUTAVEL
:: -----------------------------------------------------------------------------
echo [ETAPA 3/5] Assinando executavel protegido com carimbo de tempo...
call "%~dp0scripts\assinar.bat" /nopause
if !errorlevel! neq 0 (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 3: Falha ao assinar digitalmente o executavel.
    goto :pipeline_error
)
if not exist "%~dp0Dist_Protected\TOTVSHub.exe" (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 3: Arquivo Dist_Protected\TOTVSHub.exe nao encontrado apos assinatura.
    goto :pipeline_error
)
echo [OK] Etapa 3 concluida com sucesso. Executavel assinado.
echo.

:: -----------------------------------------------------------------------------
:: ETAPA 4: GERACAO E ASSINATURA DO INSTALADOR
:: -----------------------------------------------------------------------------
echo [ETAPA 4/5] Compilando e assinando instalador Inno Setup...
call "%~dp0scripts\gerar_instalador.bat" /nopause
if !errorlevel! neq 0 (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 4: Falha na geracao ou assinatura do instalador.
    goto :pipeline_error
)
if not exist "%~dp0Output\TOTVSHub_Setup_v2.4.0.exe" (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 4: Arquivo Output\TOTVSHub_Setup_v2.4.0.exe nao encontrado.
    goto :pipeline_error
)
echo [OK] Etapa 4 concluida com sucesso. Instalador gerado e assinado.
echo.

:: -----------------------------------------------------------------------------
:: ETAPA 5: GERACAO DE RELEASE NOTES E CHECKSUMS
:: -----------------------------------------------------------------------------
echo [ETAPA 5/5] Gerando hashes SHA-256 e documento de release...
call "%~dp0scripts\gerar_release.bat" /nopause
if !errorlevel! neq 0 (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 5: Falha na geracao de release notes.
    goto :pipeline_error
)
if not exist "%~dp0RELEASE_NOTES.md" (
    echo.
    echo [FALHA NO PIPELINE] Erro na Etapa 5: Arquivo RELEASE_NOTES.md nao encontrado.
    goto :pipeline_error
)
echo [OK] Etapa 5 concluida com sucesso. RELEASE_NOTES.md atualizado.
echo.

:: -----------------------------------------------------------------------------
:: CONCLUSAO COM SUCESSO
:: -----------------------------------------------------------------------------
echo ========================================================
echo     [SUCESSO TOTAL] PIPELINE CONCLUIDO COM EXITO!       
echo ========================================================
echo   - Binario compilado:    TOTVSHub.exe
echo   - Binario protegido:    Dist_Protected\TOTVSHub.exe
echo   - Instalador assinado:  Output\TOTVSHub_Setup_v2.4.0.exe
echo   - Checksums e Release:  RELEASE_NOTES.md
echo ========================================================
echo.

if "%NOPAUSE%"=="" pause
exit /b 0

:pipeline_error
echo.
echo ========================================================
echo     [ERRO] O PIPELINE FOI ABORTADO DEVIDO A UMA FALHA!  
echo ========================================================
echo.
if "%NOPAUSE%"=="" pause
exit /b 1
