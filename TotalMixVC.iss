; The version needs to be passed explicitly via /DAppVersion=0.1.0 as GitVersion is used to
; determine the release version. Alternatively, the option below may be uncommented if required.
; #define AppVersion "1.0"

; The build configuration needs to be passed explicitly via /DAppBuildConfiguration=Release or
; the option below may be uncommented if required.
; #define AppBuildConfiguration "Release"

#define AppName "TotalMix Volume Control"
#define AppPublisher "Fotis Gimian"
#define AppRepoURL "https://github.com/fgimian/totalmix-volume-control"
#define AppRepoIssuesURL AppRepoURL + "/issues"
#define AppRepoReleasesURL AppRepoURL + "/releases"
#define AppIconPath "src\TotalMixVC\Icons\TotalMixVC.ico"
#define AppPublishPath "src\TotalMixVC\bin\" + AppBuildConfiguration + "\net9.0-windows\win-x64\publish"
#define AppExeName "TotalMixVC.exe"
#define AppVersion GetStringFileInfo(AddBackslash(AppPublishPath) + AppExeName, "ProductVersion")

[Setup]
AppId={{1542C049-A295-4E81-B1A4-2BE9D8F8F939}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppRepoURL}
AppSupportURL={#AppRepoIssuesURL}
AppUpdatesURL={#AppRepoReleasesURL}
DefaultDirName={autopf64}\{#AppName}
DisableProgramGroupPage=yes
LicenseFile=.\LICENSE
SetupIconFile=.\{#AppIconPath}
UninstallDisplayIcon={app}\TotalMixVC.exe
OutputDir=.\artifacts
OutputBaseFilename=TotalMixVCSetup-v{#AppVersion}-{#AppBuildConfiguration}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\{#AppPublishPath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
