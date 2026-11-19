@echo off
set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
set "WPF=%FW%\WPF"

taskkill /F /IM TOTVSHub.exe >nul 2>&1
taskkill /F /IM TOTVSHub_Corp.exe >nul 2>&1

set "ICO_ARG="
if exist "totvs_white.ico" set "ICO_ARG=/win32icon:totvs_white.ico"

set "SOURCES=Program.cs Models\Models.cs Models\DbAccessModels.cs Native\NativeWin32.cs Theme\UiTheme.cs Theme\TotvsLogo.cs Dialogs\Dialogs.cs Views\AlertCenterWindow.cs Views\MainWindow.Core.cs Views\MainWindow.Onboarding.cs Views\MainWindow.Tray.cs Views\MainWindow.Home.cs Views\MainWindow.Scanner.cs Views\MainWindow.Settings.cs Views\MainWindow.IniEditor.cs Views\MainWindow.DbAccess.cs Views\MainWindow.SmartView.cs Views\MainWindow.Tabs.cs Views\MainWindow.Logs.cs"

set "REFS=/reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Management.dll /reference:System.ServiceProcess.dll /reference:System.Windows.Forms.dll /reference:System.Xaml.dll /reference:"%WPF%\PresentationCore.dll" /reference:"%WPF%\PresentationFramework.dll" /reference:"%WPF%\WindowsBase.dll""

echo [1/2] Compilando TOTVSHub.exe (Padrao)...
"%FW%\csc.exe" /nologo /target:winexe /out:TOTVSHub.exe %ICO_ARG% %REFS% %SOURCES%

if %errorlevel%==0 (
    echo [SUCESSO] TOTVSHub.exe gerado com sucesso!
) else (
    echo [ERRO] Falha na compilacao de TOTVSHub.exe.
    if "%1" neq "/nopause" pause
    exit /b %errorlevel%
)

echo.
echo [2/2] Compilando TOTVSHub_Corp.exe (Corp)...
"%FW%\csc.exe" /nologo /target:winexe /define:CORP /out:TOTVSHub_Corp.exe %ICO_ARG% %REFS% %SOURCES%

if %errorlevel%==0 (
    echo [SUCESSO] TOTVSHub_Corp.exe gerado com sucesso!
) else (
    echo [ERRO] Falha na compilacao de TOTVSHub_Corp.exe.
    if "%1" neq "/nopause" pause
    exit /b %errorlevel%
)

echo.
echo ========================================
echo  AMBAS AS VERSOES COMPILADAS COM SUCESSO!
echo ========================================
if "%1" neq "/nopause" pause
