param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$PackageFolder
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path $repoRoot "artifacts\release\v$Version"
$bundleRoot = Join-Path $releaseRoot "bundle"
$zipName = "Hel-v$Version-win-x64.zip"
$zipPath = Join-Path $releaseRoot $zipName
$topLevelChecksumsPath = Join-Path $releaseRoot "SHA256SUMS.txt"

if (-not (Test-Path $PackageFolder)) {
    throw "PackageFolder does not exist: $PackageFolder"
}

if (Test-Path $bundleRoot) {
    Remove-Item $bundleRoot -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

New-Item -ItemType Directory -Path $bundleRoot -Force | Out-Null

# Copy the full Visual Studio sideload package output
Copy-Item (Join-Path $PackageFolder "*") $bundleRoot -Recurse -Force

# Add config override example
$configOverrideExamplePath = Join-Path $bundleRoot "config.local.example.json"
@'
{
  "Output": {
    "LogsRoot": "%DOCUMENTS%\\Hel\\Logs"
  }
}
'@ | Set-Content -Path $configOverrideExamplePath -Encoding UTF8

# Add release install notes
$installReadmePath = Join-Path $bundleRoot "README-INSTALL.txt"
@"
Hel v$Version - Install Notes

Included in this ZIP:
- signed MSIX package
- public certificate (.cer)
- Install.ps1
- Add-AppDevPackage.ps1
- dependency packages
- config.local.example.json
- SHA256SUMS.txt

Recommended install flow:
1. Open the included .cer file
2. Install it for Current User
3. Place it in the Trusted People store
4. Run Add-AppDevPackage.ps1
   or Install.ps1
5. Launch Hel after install completes

Config override:
- Packaged defaults ship in config.json
- Local override path: %LOCALAPPDATA%\Hel\config.local.json

Defaults:
- Logs: %LOCALAPPDATA%\Hel\Logs
- Output: user-selected folder
"@ | Set-Content -Path $installReadmePath -Encoding UTF8

# Remove public symbols from release ZIP if present
Get-ChildItem $bundleRoot -Filter *.appxsym -File -Recurse | Remove-Item -Force

# Inner checksums for all bundled files
$innerFiles = Get-ChildItem $bundleRoot -File -Recurse | Sort-Object FullName
$innerChecksumsPath = Join-Path $bundleRoot "SHA256SUMS.txt"

$innerChecksumLines = foreach ($file in $innerFiles) {
    $relativePath = $file.FullName.Substring($bundleRoot.Length + 1)
    $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "{0}  {1}" -f $hash, $relativePath
}

$innerChecksumLines | Set-Content -Path $innerChecksumsPath -Encoding UTF8

Compress-Archive -Path (Join-Path $bundleRoot "*") -DestinationPath $zipPath -CompressionLevel Optimal -Force

# Create top-level checksums for GitHub release assets
$topLevelFiles = Get-ChildItem $releaseRoot -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | Sort-Object Name
$topLevelChecksumLines = foreach ($file in $topLevelFiles) {
    $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "{0}  {1}" -f $hash, $file.Name
}

$topLevelChecksumLines | Set-Content -Path $topLevelChecksumsPath -Encoding UTF8

Write-Host "Release bundle created:"
Write-Host "  $releaseRoot"
Write-Host ""
Write-Host "Assets:"
Get-ChildItem $releaseRoot | ForEach-Object { Write-Host "  $($_.Name)" }
