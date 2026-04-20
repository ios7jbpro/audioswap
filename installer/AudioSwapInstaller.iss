#define MyAppName "AudioSwap"
#define MyAppPublisher "Baris"
#define MyAppVersion GetEnv("AUDIOSWAP_APP_VERSION")
#define MyPayloadDir GetEnv("AUDIOSWAP_PUBLISH_DIR")

[Setup]
AppId={{D0567C38-E3DF-4D67-BAB5-D7E720362602}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
Compression=lzma
SolidCompression=yes
OutputDir=..\dist\inno
OutputBaseFilename=AudioSwap-Setup-{#MyAppVersion}
UninstallDisplayIcon={app}\AudioSwap.exe
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startup"; Description: "Launch AudioSwap when you sign in"; GroupDescription: "Startup:"; Flags: checkedonce

[Files]
Source: "{#MyPayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\AudioSwap.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\AudioSwap.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\AudioSwap.exe"; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "{app}\AudioSwap.exe"; Description: "Launch AudioSwap"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{commonstartup}\{#MyAppName}.lnk"
