@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0.."
pushd "%ROOT%"

set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
set "WPF=%FW%\WPF"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

taskkill /F /IM TOTVSHub.exe >nul 2>&1
if exist "TOTVSHub.exe.old" del /F /Q "TOTVSHub.exe.old" >nul 2>&1
if exist "TOTVSHub.exe" (
    ren "TOTVSHub.exe" "TOTVSHub.exe.old" >nul 2>&1
)

set "ICO_ARG="
if exist "assets\totvs_white.ico" set "ICO_ARG=/win32icon:assets\totvs_white.ico"

set "MANIFEST_ARG="
if exist "assets\app.manifest" set "MANIFEST_ARG=/win32manifest:assets\app.manifest"

"%FW%\csc.exe" /nologo /target:winexe /out:TOTVSHub.exe %ICO_ARG% %MANIFEST_ARG% ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Management.dll ^
  /reference:System.ServiceProcess.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Xaml.dll ^
  /reference:"%WPF%\PresentationCore.dll" ^
  /reference:"%WPF%\PresentationFramework.dll" ^
  /reference:"%WPF%\WindowsBase.dll" ^
  Program.cs ^
  Models\Models.cs ^
  Native\NativeWin32.cs ^
  Theme\UiTheme.cs ^
  Theme\TotvsLogo.cs ^
  Dialogs\Dialogs.cs ^
  Views\AlertCenterWindow.cs ^
  Views\MainWindow.Core.cs ^
  Views\MainWindow.Onboarding.cs ^
  Views\MainWindow.Tray.cs ^
  Views\MainWindow.Home.cs ^
  Views\MainWindow.Scanner.cs ^
  Views\MainWindow.Settings.cs ^
  Views\MainWindow.IniEditor.cs

set "CSC_ERR=!errorlevel!"
popd

if !CSC_ERR!==0 (
    echo [SUCESSO] TOTVSHub.exe gerado com sucesso!
) else (
    echo [ERRO] Falha na compilacao.
    if "%NOPAUSE%"=="" pause
    exit /b !CSC_ERR!
)

if "%NOPAUSE%"=="" pause
exit /b 0
