@echo off
setlocal EnableDelayedExpansion
set "SCRIPT_DIR=%~dp0"
set "ROOT=%SCRIPT_DIR%.."
pushd "%ROOT%"

set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
set "WPF=%FW%\WPF"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

taskkill /F /IM TOTVSHub.exe >nul 2>&1
taskkill /F /IM TOTVSHub_Corp.exe >nul 2>&1
if exist "TOTVSHub.exe.old" del /F /Q "TOTVSHub.exe.old" >nul 2>&1
if exist "TOTVSHub.exe" (
    ren "TOTVSHub.exe" "TOTVSHub.exe.old" >nul 2>&1
)

set "ICO_ARG="
if exist "assets\totvs_white.ico" set "ICO_ARG=/win32icon:assets\totvs_white.ico"
if not defined ICO_ARG if exist "totvs_white.ico" set "ICO_ARG=/win32icon:totvs_white.ico"

set "MANIFEST_ARG="
if exist "assets\app.manifest" set "MANIFEST_ARG=/win32manifest:assets\app.manifest"

set "SOURCES=Program.cs Models\Models.cs Models\DbAccessModels.cs Native\NativeWin32.cs Theme\UiTheme.cs Theme\TotvsLogo.cs Dialogs\Dialogs.cs Views\AlertCenterWindow.cs Views\MainWindow.Core.cs Views\MainWindow.Onboarding.cs Views\MainWindow.Tray.cs Views\MainWindow.Home.cs Views\MainWindow.Scanner.cs Views\MainWindow.Settings.cs Views\MainWindow.IniEditor.cs Views\MainWindow.DbAccess.cs Views\MainWindow.SmartView.cs Views\MainWindow.Tabs.cs Views\MainWindow.Logs.cs Views\MainWindow.Overview.cs"

set "REFS=/reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Management.dll /reference:System.ServiceProcess.dll /reference:System.Windows.Forms.dll /reference:System.Xaml.dll /reference:"%WPF%\PresentationCore.dll" /reference:"%WPF%\PresentationFramework.dll" /reference:"%WPF%\WindowsBase.dll""

echo [1/2] Compilando TOTVSHub.exe (Padrao)...
"%FW%\csc.exe" /nologo /target:winexe /out:TOTVSHub.exe %ICO_ARG% %MANIFEST_ARG% %REFS% %SOURCES%
set "CSC_ERR=!errorlevel!"

if !CSC_ERR!==0 (
    echo [SUCESSO] TOTVSHub.exe gerado com sucesso!
) else (
    echo [ERRO] Falha na compilacao de TOTVSHub.exe.
    popd
    if "%NOPAUSE%"=="" pause
    exit /b !CSC_ERR!
)

set "MANIFEST_CORP_ARG="
if exist "assets\app_corp.manifest" set "MANIFEST_CORP_ARG=/win32manifest:assets\app_corp.manifest"

echo.
echo [2/2] Compilando TOTVSHub_Corp.exe (Corp)...
"%FW%\csc.exe" /nologo /target:winexe /define:CORP /out:TOTVSHub_Corp.exe %ICO_ARG% %MANIFEST_CORP_ARG% %REFS% %SOURCES%
set "CSC_CORP_ERR=!errorlevel!"

popd

if !CSC_CORP_ERR!==0 (
    echo [SUCESSO] TOTVSHub_Corp.exe gerado com sucesso!
    if not exist "Release_All" mkdir "Release_All"
    if not exist "Release_Corp" mkdir "Release_Corp"
    copy /y "TOTVSHub.exe" "Release_All\TOTVSHub.exe" >nul 2>&1
    copy /y "TOTVSHub_Corp.exe" "Release_Corp\TOTVSHub.exe" >nul 2>&1
) else (
    echo [ERRO] Falha na compilacao de TOTVSHub_Corp.exe.
    if "%NOPAUSE%"=="" pause
    exit /b !CSC_CORP_ERR!
)

echo.
echo ========================================
echo  AMBAS AS VERSOES COMPILADAS COM SUCESSO!
echo ========================================
if "%NOPAUSE%"=="" pause
exit /b 0
