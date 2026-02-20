<#
.SYNOPSIS
    Packages MediaBrowser into a portable ZIP distribution.

.DESCRIPTION
    Publishes the .NET self-contained single-file executable, copies supporting
    files, creates a ZIP archive, and generates a SHA-256 checksum file.

.PARAMETER Version
    Version string to embed in the package name (e.g. "v1.0.0-20260220-abc1234").

.PARAMETER OutputDir
    Directory where the package ZIP is written. Defaults to the repo root.

.EXAMPLE
    .\scripts\package-portable.ps1 -Version "v1.0.0-20260220-abc1234"
#>
param(
    [string]$Version = "v1.0.0-dev",
    [string]$OutputDir = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot "source\MediaBrowser\MediaBrowser.csproj"
$PublishDir = Join-Path $RepoRoot "publish"
$PackageName = "MediaBrowser-Portable-${Version}"
$PackageDir = Join-Path $OutputDir $PackageName

Write-Host "=== MediaBrowser Portable Packager ===" -ForegroundColor Cyan
Write-Host "Version   : $Version"
Write-Host "Project   : $ProjectPath"
Write-Host "Output    : $OutputDir"

# ── Step 1: Publish ──────────────────────────────────────────────────────────
Write-Host "`n[1/4] Publishing self-contained executable…" -ForegroundColor Yellow
dotnet publish $ProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Platform=x64 `
    --output $PublishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# ── Step 2: Assemble package ─────────────────────────────────────────────────
Write-Host "`n[2/4] Assembling package directory…" -ForegroundColor Yellow

if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
New-Item -ItemType Directory -Force -Path $PackageDir | Out-Null
New-Item -ItemType Directory -Force -Path "$PackageDir\config" | Out-Null
New-Item -ItemType Directory -Force -Path "$PackageDir\cache" | Out-Null

# Executable
Copy-Item (Join-Path $PublishDir "MediaBrowser.exe") "$PackageDir\"

# Documentation
foreach ($doc in @("README.md", "LICENSE")) {
    $src = Join-Path $RepoRoot $doc
    if (Test-Path $src) { Copy-Item $src "$PackageDir\" }
}

# Default config
@{
    version         = $Version
    defaultZoomLevel = 200
    cacheSize       = 500
    autoPlayVideos  = $false
    theme           = "system"
    lastDirectory   = ""
    lastSortField   = "Name"
    lastSortAscending = $true
    lastFilterType  = "All"
    windowWidth     = 1200
    windowHeight    = 800
} | ConvertTo-Json -Depth 3 | Out-File -FilePath "$PackageDir\config\config.json" -Encoding UTF8

# Portable README
@"
# MediaBrowser Portable

Version : $Version
Built   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') UTC

## Usage
1. Run MediaBrowser.exe
2. Click 'Browse' to select a media directory
3. Use the toolbar to filter and sort media
4. Adjust thumbnail size with the zoom slider or Ctrl+Plus / Ctrl+Minus

## Supported Formats
Images   : JPEG, PNG, BMP, GIF, TIFF, WebP, PSP, TGA, PCX, XYZ
Videos   : MP4, AVI, MOV, MKV, WebM
Animated : GIF, APNG

## Portable Mode
All settings are stored in the config\ directory.
No installation or registry modifications required.
Can run from any folder, including a USB drive.

## Keyboard Shortcuts
Ctrl+Plus  : Increase thumbnail size
Ctrl+Minus : Decrease thumbnail size
"@ | Out-File -FilePath "$PackageDir\README-PORTABLE.txt" -Encoding UTF8

Write-Host "  Package contents:"
Get-ChildItem -Recurse $PackageDir | ForEach-Object {
    Write-Host "    $($_.FullName.Replace($PackageDir, ''))"
}

# ── Step 3: ZIP ───────────────────────────────────────────────────────────────
Write-Host "`n[3/4] Creating ZIP archive…" -ForegroundColor Yellow
$ZipPath = Join-Path $OutputDir "${PackageName}.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path $PackageDir -DestinationPath $ZipPath -CompressionLevel Optimal
Write-Host "  Created: $ZipPath ($(([math]::Round((Get-Item $ZipPath).Length / 1MB, 1))) MB)"

# ── Step 4: Checksum ──────────────────────────────────────────────────────────
Write-Host "`n[4/4] Generating SHA-256 checksum…" -ForegroundColor Yellow
$Hash = (Get-FileHash $ZipPath -Algorithm SHA256).Hash
$ChecksumPath = "${ZipPath}.sha256"
"${Hash}  ${PackageName}.zip" | Out-File -FilePath $ChecksumPath -Encoding ASCII
Write-Host "  SHA-256: $Hash"
Write-Host "  Written: $ChecksumPath"

Write-Host "`n✅ Packaging complete!" -ForegroundColor Green
Write-Host "   $ZipPath"
