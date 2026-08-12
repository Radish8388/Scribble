[Setup]
AppName=Scribble
AppVersion=1.3.0
DefaultDirName={autopf}\Radish\Scribble
DefaultGroupName=Radish
SetupIconFile=Icons\scribble2.ico
UninstallDisplayIcon={app}\Scribble.exe
LicenseFile=LICENSE.txt
OutputBaseFilename=ScribbleSetup
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
AppPublisher=Radish
AppPublisherURL=https://radish-vert.vercel.app
AppId={{49497bd1-5096-4a0a-b684-916d225272ba}

[Files]
Source: "bin\Release\net10.0-windows\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Scribble"; Filename: "{app}\Scribble.exe"
Name: "{commondesktop}\Scribble"; Filename: "{app}\Scribble.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\Scribble.exe"; Description: "Launch Scribble"; Flags: nowait postinstall skipifsilent
