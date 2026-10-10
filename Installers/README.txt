This folder will contain any compiled installers.

To build the installer (requires NSIS, https://nsis.sourceforge.io/):
  1. dotnet build SB/SB.csproj -c Release
  2. cd "Installer scripts"
  3. makensis ExtractVersionInfo.nsi      (builds the small helper that reads the version from SB.exe)
  4. makensis CreateInstaller.nsi         (writes Installers\SB_<version>.exe)
