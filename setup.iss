; OpenSnap — Inno Setup installer script
; Build: ISCC.exe setup.iss

#define MyAppName "OpenSnap"
#define MyAppVersion "0.6.0"
#define MyAppPublisher "Sparsh"
#define MyAppURL "https://github.com/sparshsam/opensnap"
#define MyAppExeName "OpenSnap.exe"

[Setup]
AppId={{B8A3C1E0-4F2D-4A8E-9C7B-5D6F1E2A3B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
OutputDir=dist
OutputBaseFilename=OpenSnap-Setup-v{#MyAppVersion}
SetupIconFile=Resources\app.ico
UninstallDisplayIcon={app}\OpenSnap.exe
UninstallDisplayName={#MyAppName} {#MyAppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
CloseApplications=force
DisableWelcomePage=no
DisableFinishedPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Preserve user settings — %APPDATA%\OpenSnap is never touched by the installer

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c taskkill /f /im OpenSnap.exe 2>nul"; Flags: runhidden

[Code]
function InitializeUninstall: Boolean;
begin
  Result := True;
end;
