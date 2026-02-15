; Script Inno Setup per DMInps
; Versione: 1.0.9
; Autore: Dario Giorgio Zani

#define MyAppName "DMInps"
#define MyAppVersion "1.0.9"
#define MyAppPublisher "Dario Giorgio Zani - MMG Lumezzane (BS)"
#define MyAppExeName "DMInps.exe"
#define MyAppDescription "Crea Relazione Diabete per INPS"

[Setup]
; Identificatore univoco dell'applicazione
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=
AppSupportURL=
AppUpdatesURL=
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
; Richiede privilegi amministratore per l'installazione
PrivilegesRequired=admin
; Disabilita la pagina di selezione directory per utenti non admin
DisableDirPage=no
DisableProgramGroupPage=yes
; Compressione
Compression=lzma2/ultra64
SolidCompression=yes
; Output
OutputDir=.\Installer
OutputBaseFilename=DMInps_Setup_v{#MyAppVersion}
; Icona
SetupIconFile=.\DMInps.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
; Info aggiuntive
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
; Architettura
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Wizard
WizardStyle=modern
; Licenza e info (opzionale)
;LicenseFile=LICENSE.txt
;InfoBeforeFile=README.txt

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; File principale dell'applicazione
Source: ".\bin\Release\net8.0-windows\win-x64\publish\DMInps.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\DMInps.pdb"; DestDir: "{app}"; Flags: ignoreversion
; DLL native richieste
Source: ".\bin\Release\net8.0-windows\win-x64\publish\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\libSkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\QuestPdfSkia.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\bin\Release\net8.0-windows\win-x64\publish\wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
; File dati
Source: ".\bin\Release\net8.0-windows\win-x64\publish\atc_a10.csv"; DestDir: "{app}"; Flags: ignoreversion
; Cartella font
Source: ".\bin\Release\net8.0-windows\win-x64\publish\LatoFont\*"; DestDir: "{app}\LatoFont"; Flags: ignoreversion recursesubdirs createallsubdirs
; Tool di debug (opzionale)
Source: ".\bin\Release\net8.0-windows\win-x64\publish\debugEstraiCodiciMedici.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Disinstalla {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Elimina file di configurazione locale durante la disinstallazione (opzionale)
Type: filesandordirs; Name: "{localappdata}\dgzani\DMInps"

[Code]
// Funzione per verificare se l'applicazione e' in esecuzione
function IsAppRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec('tasklist', '/FI "IMAGENAME eq DMInps.exe" /NH', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    // Se il processo e' in esecuzione, chiedi all'utente di chiuderlo
  end;
end;

// Evento di inizializzazione dell'installazione
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Qui puoi aggiungere controlli pre-installazione
end;

// Evento di inizializzazione della disinstallazione
function InitializeUninstall(): Boolean;
begin
  Result := True;
  // Avvisa l'utente di chiudere l'applicazione prima della disinstallazione
  if MsgBox('Assicurati che DMInps sia chiuso prima di procedere con la disinstallazione.' + #13#10 + #13#10 + 'Vuoi continuare?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
  end;
end;
