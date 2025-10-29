; ============================================================
; ScreenControl 安装脚本
; ============================================================

#define MyAppName "ScreenControl"
#define MyAppVersion "1.6.3"
#define MyAppPublisher "ScreenControl Team"
#define MyAppExeName "ScreenControl.exe"

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ch"; MessagesFile: "ChineseSimplified.isl"

[CustomMessages]
; ---------- English ----------
en.DesktopIconGroup=Shortcuts
en.DesktopIcon=Create a desktop shortcut
en.AppIconName=ScreenControl
en.StartApp=Start ScreenControl
en.DefaultGroupName=ScreenControl

; ---------- Chinese (Simplified) ----------
ch.DesktopIconGroup=快捷方式
ch.DesktopIcon=创建桌面快捷方式
ch.AppIconName=屏幕控制
ch.StartApp=启动 屏幕控制
ch.DefaultGroupName=屏幕控制

[Setup]
AppId={{26D8D1E2-5F43-4E8D-8894-5E98CD1B1D1A}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={cm:DefaultGroupName}

; 输出目录：上级 Output 文件夹
OutputDir=..\Output\
OutputBaseFilename=Setup-{#MyAppName}-{#MyAppVersion}

; 图标路径：
SetupIconFile=..\..\res\screencontrol.ico

Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:DesktopIconGroup}"; Flags: unchecked

[Files]
; 发布产物路径
Source: "..\..\bin\Release\net9.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{cm:AppIconName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{cm:AppIconName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:StartApp}"; Flags: postinstall skipifsilent nowait runascurrentuser
