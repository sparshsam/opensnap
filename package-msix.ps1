# OpenSnap - MSIX package builder (optional distribution format)
# Prerequisites: Windows SDK (MakeAppx.exe + signtool.exe)
# Usage: .\package-msix.ps1
#
# NOTE: Run this AFTER dotnet publish -c Release -o release
#       Store-bound MSIX does not need code signing (Microsoft signs it).

param(
    [string]$Version = "0.7.0",
    [string]$InputDir = "release",
    [string]$OutputDir = "dist"
)

$ErrorActionPreference = "Stop"

$inputPath  = Join-Path $PSScriptRoot $InputDir
$outputPath = Join-Path $PSScriptRoot $OutputDir
$msixPath   = Join-Path $outputPath "OpenSnap-$Version.msix"

# Auto-detect MakeAppx.exe from the latest installed Windows SDK
$sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10"
$makeAppx = if (Test-Path "$sdkRoot\bin\*\x64\MakeAppx.exe") {
    (Get-ChildItem "$sdkRoot\bin\*\x64\MakeAppx.exe" | Sort-Object VersionInfo.ProductVersion -Descending | Select-Object -First 1).FullName
} elseif (Test-Path "$sdkRoot\App Certification Kit\MakeAppx.exe") {
    "$sdkRoot\App Certification Kit\MakeAppx.exe"
} else {
    $null
}

if (-not $makeAppx) {
    Write-Warning "MakeAppx.exe not found."
    Write-Warning "Install Windows SDK from: https://developer.microsoft.com/windows/downloads/windows-sdk/"
    exit 0
}

if (!(Test-Path $inputPath)) {
    Write-Error "Run 'dotnet publish -c Release -o release' first."
    exit 1
}

# Generate MSIX-required PNG assets from the app icon
$assetsDir = Join-Path $inputPath "Assets"
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
Add-Type -AssemblyName System.Drawing
$iconPath = Join-Path $PSScriptRoot "Resources\app.ico"
if (Test-Path $iconPath) {
    $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($iconPath)
    foreach ($size in @(44, 150, 50)) {
        $bmp = $icon.ToBitmap()
        $resized = New-Object System.Drawing.Bitmap($size, $size)
        $g = [System.Drawing.Graphics]::FromImage($resized)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($bmp, 0, 0, $size, $size)
        $g.Dispose()
        $name = if ($size -eq 50) { "StoreLogo.png" } else { "Square${size}x${size}Logo.png" }
        $resized.Save((Join-Path $assetsDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
        $resized.Dispose()
        $bmp.Dispose()
    }
    $icon.Dispose()
}

# Copy Package.appxmanifest as AppxManifest.xml into the payload
Copy-Item (Join-Path $PSScriptRoot "Package.appxmanifest") (Join-Path $inputPath "AppxManifest.xml") -Force

# Enumerate all files in the publish output and build a mapping
$relDir = Split-Path $inputPath -Leaf
$mapFile = Join-Path $env:TEMP "msix-map.txt"
$lines = @("[Files]")
Get-ChildItem $inputPath -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($inputPath.Length + 1)
    $lines += """$relDir\$relative"" ""$relative"""
}
$lines | Set-Content $mapFile -Encoding ASCII

Write-Host "=== Building MSIX ==="
Write-Host "MakeAppx: $makeAppx"
Write-Host "Version:  $Version"
Write-Host "Packaging $($lines.Count - 1) files..."
Write-Host ""

Push-Location $PSScriptRoot
try {
    & $makeAppx pack /f $mapFile /p $msixPath /o
    if ($LASTEXITCODE -ne 0) {
        Write-Error "MakeAppx failed (exit $LASTEXITCODE)"
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
    # Clean up temp manifest so it doesn't leak into the next publish
    Remove-Item (Join-Path $inputPath "AppxManifest.xml") -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "MSIX created: $msixPath"
Write-Host "Ready for Partner Center upload."
