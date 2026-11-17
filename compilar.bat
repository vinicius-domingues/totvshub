@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "NOPAUSE="
for %%a in (%*) do (
    if /i "%%~a"=="/nopause" set "NOPAUSE=1"
)

set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
set "WPF=%FW%\WPF"

if not exist "%FW%\csc.exe" (
    echo [ERRO] Compilador csc.exe nao encontrado no diretorio framework.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

taskkill /F /IM TOTVSHub.exe >nul 2>&1

set "ICO_ARG="
if exist "totvs_white.ico" set "ICO_ARG=/win32icon:totvs_white.ico"

set "MANIFEST_ARG="
if exist "app.manifest" set "MANIFEST_ARG=/win32manifest:app.manifest"

echo [COMPILANDO] Compilando TOTVSHub.exe via C# (.NET Framework 4.8)...
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

if !errorlevel! neq 0 (
    echo.
    echo [ERRO] Falha na compilacao.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

if not exist "TOTVSHub.exe" (
    echo.
    echo [ERRO] Executavel TOTVSHub.exe nao foi gerado.
    if "%NOPAUSE%"=="" pause
    exit /b 1
)

echo [SUCESSO] TOTVSHub.exe gerado com sucesso!
if "%NOPAUSE%"=="" pause
exit /b 0
