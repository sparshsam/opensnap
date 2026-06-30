@echo off
REM OpenSnap — build + installer packaging
REM Usage: build-installer.bat
REM Prerequisites: Inno Setup 6 at C:\Program Files (x86)\Inno Setup 6

setlocal
set VERSION=0.9.0
set PROJECT_DIR=C:\Users\spars\repos\opensnap
set ISS="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo === OpenSnap v%VERSION% — Build + Installer ===

cd /d %PROJECT_DIR%

REM Step 1: Restore
echo.
echo [1/3] Restoring packages...
dotnet restore
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM Step 2: Publish framework-dependent release
echo.
echo [2/3] Publishing release...
dotnet publish -c Release -o release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM Step 3: Build installer
echo.
echo [3/3] Building installer...
%ISS% setup.iss
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo.
echo === Done ===
echo Installer: dist\OpenSnap-Setup-v%VERSION%.exe
echo.
