# OpenSnap - MSIX package builder (optional distribution format)
# Prerequisites: Windows SDK (MakeAppx.exe + signtool.exe)
# Usage: .\package-msix.ps1
#
# NOTE: Run this AFTER dotnet publish -c Release -o release
#       Store-bound MSIX does not need code signing (Microsoft signs it).

param(
    [string]$Version = "1.0.0",
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

# Copy pre-made MSIX assets from branding folder
$assetsDir = Join-Path $inputPath "Assets"
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
$brandingDir = Join-Path $PSScriptRoot "assets\branding"

# Use real brand assets, fall back to generated ones
$storeLogo = Join-Path $brandingDir "logo-50.png"
if (Test-Path $storeLogo) {
    Copy-Item $storeLogo (Join-Path $assetsDir "StoreLogo.png") -Force
}

# For Square44x44 and Square150x150, scale the 50px logo if available
$srcIcon = Join-Path $brandingDir "logo-white.png"
if (Test-Path $srcIcon) {
    Add-Type -AssemblyName System.Drawing
    foreach ($size in @(44, 150)) {
        $bmp = [System.Drawing.Image]::FromFile($srcIcon)
        $resized = New-Object System.Drawing.Bitmap($size, $size)
        $g = [System.Drawing.Graphics]::FromImage($resized)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($bmp, 0, 0, $size, $size)
        $g.Dispose()
        $name = "Square${size}x${size}Logo.png"
        $resized.Save((Join-Path $assetsDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
        $resized.Dispose()
        $bmp.Dispose()
    }
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
