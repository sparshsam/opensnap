; OpenSnap — Inno Setup installer script
; Build: ISCC.exe setup.iss
; Usage:
;   ISCC.exe setup.iss                       # normal
;   ISCC.exe setup.iss /DALLUSERS            # per-machine install (admin)
;   installer.exe /VERYSILENT /ALLUSERS      # silent per-machine
;   installer.exe /VERYSILENT /CURRENTUSER   # silent per-user

#define MyAppName "OpenSnap"
#define MyAppVersion "0.9.0"
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
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=OpenSnap-Setup-v{#MyAppVersion}
SetupIconFile=Resources\app.ico
UninstallDisplayIcon={app}\OpenSnap.exe
UninstallDisplayName={#MyAppName} {#MyAppVersion}
Compression=lzma2
SolidCompression=yes
CloseApplications=force
DisableWelcomePage=no
DisableFinishedPage=no
ShowLanguageDialog=no
AppCopyright=© {#MyAppPublisher}
AppContact={#MyAppURL}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Screenshot Widget
MinVersion=10.0.17763.0

; Install mode: per-user (default, no admin) or per-machine (/ALLUSERS)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog

; Silent install support:
;   installer.exe /VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER
;   installer.exe /VERYSILENT /SUPPRESSMSGBOXES /ALLUSERS

; Branding — place 164×314 (WizardSmallImageFile) and 55×58 (WizardImageFile)
; BMPs in Resources/ to replace the default Inno Setup graphics.
; WizardSmallImageFile=Resources\wizard-small.bmp
; WizardImageFile=Resources\wizard-large.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Preserve user settings — %APPDATA%\OpenSnap is never touched by the installer

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent unchecked

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c taskkill /f /im OpenSnap.exe 2>nul"; Flags: runhidden

[Code]
function InitializeUninstall: Boolean;
begin
  Result := True;
end;
