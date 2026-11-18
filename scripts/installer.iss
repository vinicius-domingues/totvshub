; Script Inno Setup oficial para o TOTVSHub
; https://jrsoftware.org/isinfo.php

#define MyAppName "TOTVSHub"
#define MyAppVersion "2.5.0"
#define MyAppPublisher "TOTVS Hub Project"
#define MyAppURL "https://github.com/vinicius-domingues/totvshub"
#define MyAppExeName "TOTVSHub.exe"

#if FileExists("..\Dist_Protected\TOTVSHub.exe")
  #define SourceExe "Dist_Protected\TOTVSHub.exe"
#else
  #define SourceExe "TOTVSHub.exe"
#endif

[Setup]
AppId={{9F3B2167-7D9A-48A6-9E45-D98FA123C077}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
SourceDir=..
OutputDir=Output
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=TOTVSHub_Setup_v{#MyAppVersion}
SetupIconFile=assets\totvs_white.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "assets\totvs_white.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\totvs_white.svg"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\app.manifest"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\totvs_white.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\totvs_white.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
