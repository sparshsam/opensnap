# Deployment

## Publish framework-dependent release

```cmd
dotnet publish -c Release -o release
```

Output: `release\OpenSnap.exe` (~170 KB, requires .NET 8 Runtime installed).

## Build Inno Setup installer

```cmd
build-installer.bat
```

Requires [Inno Setup 6](https://jrsoftware.org/isdl.php) at `C:\Program Files (x86)\Inno Setup 6`.

Output: `dist\OpenSnap-Setup-v0.9.0.exe`

### Silent install

```
OpenSnap-Setup-v0.9.0.exe /VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER
```

Per-machine (admin required):
```
OpenSnap-Setup-v0.9.0.exe /VERYSILENT /ALLUSERS
```

## Build MSIX

```powershell
cd C:\Users\spars\repos\opensnap
dotnet publish -c Release -o release
.\package-msix.ps1 -Version 0.9.0
```

Requires Windows SDK (MakeAppx.exe). Output: `dist\OpenSnap-0.9.0.msix`

## Desktop shortcut

```powershell
.\create-desktop-shortcut.ps1
```

## Release workflow

1. Update version in `OpenShot.csproj`, `setup.iss`, `build-installer.bat`
2. Commit and tag: `git tag v0.x.x && git push origin --tags`
3. Build installer + MSIX on Windows
4. Upload MSIX to **Microsoft Partner Center** and publish to Store
5. Create GitHub release:

```bash
gh release create v0.x.x --title "v0.x.x — Title" --notes-file RELEASE_NOTES.md
gh release upload v0.x.x dist/OpenSnap-Setup-v0.x.x.exe
```

> **Note:** The **Microsoft Store** is the recommended distribution channel. The standalone MSIX and EXE are provided for manual installation and testing — the MSIX is not code-signed. Always update the Store first, then upload standalone builds to GitHub Releases.
