# define name of installer
	!system ".\ExtractVersionInfo.exe"
	!include "App-Version.txt"
	!include "MUI.nsh"
	!include "FileFunc.nsh"
	Name "SB, Version ${Version}"
	OutFile "..\Installers\SB_${Version}.exe"
 
# define installation directory
	InstallDir "$PROGRAMFILES\SB"
	InstallDirRegKey HKLM Software\SB InstallLocation
 
# for removing Start Menu shortcut in Windows 7
	RequestExecutionLevel admin

# define icon
	!define MUI_ICON "..\SB\icon.ico"

# install pages
	!insertmacro MUI_PAGE_WELCOME
	!insertmacro MUI_PAGE_DIRECTORY
	!insertmacro MUI_PAGE_INSTFILES
		# These indented statements modify settings for MUI_PAGE_FINISH
		!define MUI_FINISHPAGE_NOAUTOCLOSE
		!define MUI_FINISHPAGE_RUN "$INSTDIR\SB.exe"
		!define MUI_FINISHPAGE_RUN_CHECKED
		!define MUI_FINISHPAGE_RUN_TEXT "Start SB"
	!insertmacro MUI_PAGE_FINISH

# uninstall pages
	!insertmacro MUI_UNPAGE_WELCOME
	!insertmacro MUI_UNPAGE_CONFIRM
	!insertmacro MUI_UNPAGE_INSTFILES
	!insertmacro MUI_UNPAGE_FINISH
 
# language
	!insertmacro MUI_LANGUAGE "English"
	!insertmacro MUI_LANGUAGE "Japanese"

# start default section
Section
    # creating start menu shortcuts for all users
    SetShellVarContext all

    # set the installation directory as the destination for the following actions
    SetOutPath $INSTDIR
 
    # create the uninstaller
    WriteUninstaller "uninstall.exe"

    # install app files
    File "..\SB\bin\Release\GalaSoft.MvvmLight.dll"
    File "..\SB\bin\Release\GalaSoft.MvvmLight.Extras.dll"
    File "..\SB\bin\Release\GalaSoft.MvvmLight.Platform.dll"
    File "..\SB\bin\Release\SB.exe"
    File "..\SB\bin\Release\SB.exe.config"
    File "..\SB\bin\Release\SB.Business.dll"
    File "..\SB\bin\Release\SB.Controls.dll"
    File "..\SB\bin\Release\SB.Converters.dll"
    File "..\SB\bin\Release\SB.Icons.dll"
    File "..\SB\bin\Release\SB.Input.dll"
    File "..\SB\bin\Release\SB.Interactivity.dll"
    File "..\SB\bin\Release\SB.Models.dll"
    File "..\SB\bin\Release\SB.SoundPlayer.dll"
    File "..\SB\bin\Release\SB.Utils.dll"
    File "..\SB\bin\Release\SB.ViewModels.dll"
    File "..\SB\bin\Release\SB.Views.dll"
    File "..\SB\bin\Release\NAudio.dll"
    File "..\SB\bin\Release\NAudio.Vorbis.dll"
    File "..\SB\bin\Release\NVorbis.dll"
    File "..\SB\bin\Release\CommonServiceLocator.dll"
    File "..\SB\bin\Release\Octokit.dll"
    File "..\SB\bin\Release\System.Windows.Interactivity.dll"
    File "..\LICENSE"
    File "..\README.md"
 
    # create a shortcut named "new shortcut" in the start menu programs directory
    # point the new shortcut at the program uninstaller
    CreateDirectory "$SMPROGRAMS\SB"
    CreateShortCut "$SMPROGRAMS\SB\uninstall.lnk" "$INSTDIR\uninstall.exe"
    CreateShortCut "$SMPROGRAMS\SB\SB.lnk" "$INSTDIR\SB.exe"

    # add application to Add/Remove programs
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "DisplayName" "SB"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "QuietUninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "Publisher" "Kalejin"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "DisplayVersion" "${Version}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "DisplayIcon" "$INSTDIR\SB.exe"
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB" \
                 "EstimatedSize" "$0"
    WriteRegStr HKLM "Software\SB" \
                 "InstallLocation" "$\"$INSTDIR$\""
SectionEnd
 
# uninstaller section start
Section "uninstall"
 
    # creating start menu shortcuts for all users
    SetShellVarContext all

    # delete the uninstaller and app files
    Delete "$INSTDIR\uninstall.exe"
	Delete "$INSTDIR\GalaSoft.MvvmLight.dll"
    Delete "$INSTDIR\GalaSoft.MvvmLight.Extras.dll"
    Delete "$INSTDIR\GalaSoft.MvvmLight.Platform.dll"
    Delete "$INSTDIR\SB.exe"
    Delete "$INSTDIR\SB.exe.config"
    Delete "$INSTDIR\SB.Business.dll"
    Delete "$INSTDIR\SB.Controls.dll"
    Delete "$INSTDIR\SB.Converters.dll"
    Delete "$INSTDIR\SB.Icons.dll"
    Delete "$INSTDIR\SB.Input.dll"
    Delete "$INSTDIR\SB.Interactivity.dll"
    Delete "$INSTDIR\SB.Models.dll"
    Delete "$INSTDIR\SB.SoundPlayer.dll"
    Delete "$INSTDIR\SB.Utils.dll"
    Delete "$INSTDIR\SB.ViewModels.dll"
    Delete "$INSTDIR\SB.Views.dll"
    Delete "$INSTDIR\NAudio.dll"
    Delete "$INSTDIR\NAudio.Vorbis.dll"
    Delete "$INSTDIR\NVorbis.dll"
    Delete "$INSTDIR\CommonServiceLocator.dll"
    Delete "$INSTDIR\Octokit.dll"
    Delete "$INSTDIR\System.Windows.Interactivity.dll"
    Delete "$INSTDIR\LICENSE"
    Delete "$INSTDIR\README.md"
    StrCpy $0 "$INSTDIR"
    Call un.DeleteDirIfEmpty
 
    # remove the link from the start menu
    Delete "$SMPROGRAMS\SB\uninstall.lnk"
    Delete "$SMPROGRAMS\SB\SB.lnk"
    StrCpy $0 "$SMPROGRAMS\SB"
    Call un.DeleteDirIfEmpty

    # remove registry key
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SB"
    DeleteRegKey HKLM "Software\SB"
 
# uninstaller section end
SectionEnd

Function un.DeleteDirIfEmpty
  FindFirst $R0 $R1 "$0\*.*"
  strcmp $R1 "." 0 NoDelete
   FindNext $R0 $R1
   strcmp $R1 ".." 0 NoDelete
    ClearErrors
    FindNext $R0 $R1
    IfErrors 0 NoDelete
     FindClose $R0
     Sleep 1000
     RMDir "$0"
  NoDelete:
   FindClose $R0
FunctionEnd