; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!


#define MyAppExeName "VRSuspender.exe"
#define VRSuspender "[[[VRSUSPENDER]]]"
#define Debug "bin\Debug\"
#define Folder Str(VRSuspender) + Str(Debug)
#define MyVersion GetFileVersion(Str(Folder) + "VRSuspender.exe")
#define MyAppName "VR Suspender"
#define MyAppVersion Str(MyVersion)
#define MyAppPublisher "Pascal Pharand"
#define MyAppURL "https://github.com/Hyrules/VRSuspender"



[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{49F10A1F-88A8-4171-8C9E-2FFE34D9733E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableDirPage=false
DefaultGroupName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion} Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
SetupIconFile="{#VRSuspender}\Resources\icon.ico"
;InfoBeforeFile="{#VRSuspender}Build\README.txt"
UninstallDisplayIcon="{#VRSuspender}\Resources\icon.ico"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]

Source: "{#Folder}*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Folder}*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Folder}VRSuspender.exe"; DestDir: "{app}"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}"; Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
function hasDotNetCore(version: string) : boolean;
var
    runtimes: TArrayOfString;
    I: Integer;
    versionCompare: Integer;
    registryKey: string;
begin
    registryKey := 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App';
    if(not IsWin64) then
       registryKey :=  'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App';
       
    Log('[.NET] Look for version ' + version);
       
    if not RegGetValueNames(HKLM, registryKey, runtimes) then
    begin
      Log('[.NET] Issue getting runtimes from registry');
      Result := False;
      Exit;
    end;
    
    for I := 0 to GetArrayLength(runtimes)-1 do
    begin
      versionCompare := CompareVersion(runtimes[I], version);
      Log(Format('[.NET] Compare: %s/%s = %d', [runtimes[I], version, versionCompare]));
      if(not (versionCompare = -1)) then
      begin
        Log(Format('[.NET] Selecting %s', [runtimes[I]]));
        Result := True;
          Exit;
      end;
    end;
    Log('[.NET] No compatible found');
    Result := False;
end;

function InitializeSetup(): Boolean;
begin
    if not hasDotNetCore('v3.1', 0) then begin
        MsgBox('WinHue requires Microsoft .NET Core 3.0.'#13#13
            'Please install this version,'#13
            'and then re-run the VR Suspender setup program.', mbInformation, MB_OK);
        result := false;
    end else
        result := true;
end;