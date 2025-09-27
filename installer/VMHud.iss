; Inno Setup script for VMHud v1.0.0
; Build prerequisites:
; 1) Publish the app: dotnet publish ..\src\VMHud.App -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
; 2) Compile this script with Inno Setup (iscc) or the GUI.

[Setup]
AppId={{6F6B4D8C-9F06-41E9-9C34-1E7B8E6C7C10}
AppName=VMHud
AppVersion=1.0.0
AppPublisher=Your Name or Org
DefaultDirName={autopf}\VMHud
DefaultGroupName=VMHud
UninstallDisplayIcon={app}\VMHud.App.exe
DisableProgramGroupPage=yes
OutputDir=dist
OutputBaseFilename=VMHud-1.0.0-Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=..\assets\icon.ico
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin

[Files]
Source: "..\src\VMHud.App\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\VMHud"; Filename: "{app}\VMHud.App.exe"; IconFilename: "{app}\assets\icon.ico"
Name: "{commondesktop}\VMHud"; Filename: "{app}\VMHud.App.exe"; Tasks: desktopicon; IconFilename: "{app}\assets\icon.ico"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\VMHud.App.exe"; Description: "Launch VMHud"; Flags: nowait postinstall skipifsilent

; Notes:
; - For per-user install without admin, change:
;   DefaultDirName={userappdata}\VMHud
;   PrivilegesRequired=lowest
;   Name: "{commondesktop}\..." -> use {userdesktop}
