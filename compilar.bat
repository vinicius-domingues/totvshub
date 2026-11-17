@echo off
set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
set "WPF=%FW%\WPF"

"%FW%\csc.exe" /nologo /target:winexe /out:TOTVSHub.exe ^
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
  TOTVSHub.cs

if %errorlevel%==0 (
    echo [SUCESSO] TOTVSHub.exe gerado com sucesso!
) else (
    echo [ERRO] Falha na compilacao.
)
pause