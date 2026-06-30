# OpenSnap — MSIX package builder (optional distribution format)
# Prerequisites: Windows SDK (MakeAppx.exe + signtool.exe)
# Usage: .\package-msix.ps1
#
# NOTE: Run this AFTER dotnet publish -c Release -o release
#       Requires a code-signing certificate for production.

param(
    [string]$Version = "0.7.0",
    [string]$Publisher = "CN=YourName, O=YourOrg, L=YourCity, S=YourState, C=US",
    [string]$InputDir = "release",
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $PSScriptRoot "Package.appxmanifest"
$inputPath    = Join-Path $PSScriptRoot $InputDir
$outputPath   = Join-Path $PSScriptRoot $OutputDir
$msixPath     = Join-Path $outputPath "OpenSnap-$Version.msix"
$makeAppx     = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\MakeAppx.exe"
$signtool     = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"

if (!(Test-Path $inputPath)) {
    Write-Error "Run 'dotnet publish -c Release -o release' first."
    exit 1
}

# Create the mapping file
$mapping = @"
[Files]
"$inputPath\*" "$(Split-Path $inputPath -Leaf)\*"
"@
$mapFile = Join-Path $env:TEMP "msix-map.txt"
$mapping | Set-Content $mapFile -Encoding ASCII

Write-Host "=== Building MSIX ==="
Write-Host "Version: $Version"
Write-Host ""

# Build
if (!(Test-Path $makeAppx)) {
    Write-Warning "MakeAppx.exe not found — install Windows SDK 10.0.19041.0+"
    Write-Warning "Skip or install from: https://developer.microsoft.com/windows/downloads/windows-sdk/"
    exit 0
}

& $makeAppx pack /f $mapFile /p $msixPath /o

if ($LASTEXITCODE -ne 0) {
    Write-Error "MakeAppx failed"
    exit $LASTEXITCODE
}

Write-Host "MSIX created: $msixPath"

# Sign (only if cert available)
if (Test-Path $signtool) {
    Write-Host ""
    Write-Host "=== Signing MSIX ==="
    Write-Host "To sign, provide a PFX or use an installed certificate:"
    Write-Host ""
    Write-Host "  signtool sign /a /fd SHA256 /f cert.pfx /p PASSWORD $msixPath"
    Write-Host ""
    Write-Host "(Skipping — no cert specified for automated build.)"
} else {
    Write-Warning "signtool.exe not found — MSIX is unsigned."
}

Write-Host ""
Write-Host "Done: $msixPath"
