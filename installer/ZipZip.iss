#define MyAppName "ZipZip"
#define MyAppPublisher "ZipZip Project"
#define MyAppExeName "ZipZip.App.exe"

#ifndef StageDir
  #error StageDir define is required
#endif

#ifndef OutputDir
  #error OutputDir define is required
#endif

#ifndef AppVersion
  #define AppVersion "0.1.0-preview"
#endif

#ifndef OutputBaseFilename
  #define OutputBaseFilename "ZipZip-Setup"
#endif

[Setup]
AppId={{A7B62642-7B61-4E1C-9AA4-C8B726F85B82}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ZipZip
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UsePreviousAppDir=yes
ChangesAssociations=yes
CloseApplications=yes
CloseApplicationsFilter=ZipZip.App.exe,ZipZip.ShellExtension.exe
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile={#StageDir}\Assets\App\ZipZip.ico
UninstallDisplayIcon={app}\ZipZip.App.exe
#ifndef NumericVersion
  #define NumericVersion "0.2.1.0"
#endif
VersionInfoVersion={#NumericVersion}
VersionInfoCompany=ZipZip Project
VersionInfoProductName={#MyAppName}
VersionInfoDescription={#MyAppName} Installer
VersionInfoTextVersion={#AppVersion}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "바탕 화면 바로가기 만들기"; GroupDescription: "추가 작업:"; Flags: unchecked
Name: "explorermenu"; Description: "탐색기 우클릭 메뉴 등록"; GroupDescription: "통합 옵션:"; Flags: checkablealone checkedonce
Name: "associatefiles"; Description: "권장 확장자를 ZipZip과 연결"; GroupDescription: "통합 옵션:"; Flags: unchecked

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\ZipZip"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\App\ZipZip.ico"
Name: "{autodesktop}\ZipZip"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\App\ZipZip.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\ZipZip.ShellExtension.exe"; Parameters: "register ""{app}\ZipZip.App.exe"""; Flags: runhidden waituntilterminated; Tasks: explorermenu
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\InstallerSupport\Register-ZipZipFileAssociations.ps1"" -AppExecutablePath ""{app}\ZipZip.App.exe"""; Flags: runhidden waituntilterminated; Tasks: associatefiles
Filename: "{app}\{#MyAppExeName}"; Description: "ZipZip 실행"; Flags: nowait postinstall skipifsilent unchecked

[UninstallRun]
Filename: "{app}\ZipZip.ShellExtension.exe"; Parameters: "unregister"; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "UnregisterShell"
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\InstallerSupport\Unregister-ZipZipFileAssociations.ps1"""; Flags: runhidden waituntilterminated skipifdoesntexist; RunOnceId: "UnregisterAssociations"
