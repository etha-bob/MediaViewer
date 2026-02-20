<#
.SYNOPSIS
    Generates SHA-256 checksums for distribution files.

.PARAMETER Path
    Path to a file or a directory. If a directory is given, all *.zip files
    inside it are hashed.

.EXAMPLE
    .\scripts\generate-checksums.ps1 -Path .\MediaBrowser-Portable-v1.0.0.zip
    .\scripts\generate-checksums.ps1 -Path .
#>
param(
    [Parameter(Mandatory)]
    [string]$Path
)

$ErrorActionPreference = "Stop"

$targets = if (Test-Path $Path -PathType Container) {
    Get-ChildItem -Path $Path -Filter "*.zip"
} else {
    @(Get-Item $Path)
}

if ($targets.Count -eq 0) {
    Write-Warning "No files found at: $Path"
    exit 0
}

foreach ($file in $targets) {
    $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
    $checksumFile = "$($file.FullName).sha256"
    "$hash  $($file.Name)" | Out-File -FilePath $checksumFile -Encoding ASCII
    Write-Host "$hash  $($file.Name)"
    Write-Host "  -> $checksumFile"
}
