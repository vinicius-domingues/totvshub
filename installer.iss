; Script Inno Setup oficial para o TOTVSHub
; https://jrsoftware.org/isinfo.php

#define MyAppName "TOTVSHub"
#define MyAppVersion "2.4.0"
#define MyAppPublisher "TOTVS Hub Project"
#define MyAppURL "https://github.com/vinicius-domingues/totvshub"
#define MyAppExeName "TOTVSHub.exe"

[Setup]
AppId={{9F3B2167-7D9A-48A6-9E45-D98FA123C077}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=TOTVSHub_Setup_v{#MyAppVersion}
SetupIconFile=totvs_white.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "totvs_white.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "totvs_white.svg"; DestDir: "{app}"; Flags: ignoreversion
Source: "app.manifest"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\totvs_white.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\totvs_white.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
