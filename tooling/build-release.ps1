[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$OutputDir = "release",
    [string]$DotnetPath = "$env:USERPROFILE\.dotnet\dotnet.exe",
    [switch]$SkipTests,
    [switch]$KeepStaging
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot "manifest.json"
$packagePath = Join-Path $repoRoot "package.json"
$packageLockPath = Join-Path $repoRoot "package-lock.json"
$changelogPath = Join-Path $repoRoot "CHANGELOG.md"
$packageReadmePath = Join-Path $repoRoot "docs\README.md"
$licensePath = Join-Path $repoRoot "LICENSE"
$noticesPath = Join-Path $repoRoot "THIRD-PARTY-NOTICES.md"
$iconPath = Join-Path $repoRoot "assets\icons\logo-256.png"
$pluginPath = Join-Path $repoRoot "src\OpenWaters\Plugin.cs"
$projectPath = Join-Path $repoRoot "src\OpenWaters\OpenWaters.csproj"
$testProjectPath = Join-Path $repoRoot "tests\OpenWaters.Core.Tests\OpenWaters.Core.Tests.csproj"
$outputPath = Join-Path $repoRoot $OutputDir
$stagingPath = Join-Path $outputPath "staging"

if (-not (Test-Path -LiteralPath $DotnetPath)) {
    throw "dotnet not found at '$DotnetPath'. Use -DotnetPath."
}

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "manifest.json not found."
}

if (-not (Test-Path -LiteralPath $changelogPath)) {
    throw "CHANGELOG.md not found."
}

if (-not (Test-Path -LiteralPath $packageReadmePath)) {
    throw "docs\README.md not found."
}

if (-not (Test-Path -LiteralPath $iconPath)) {
    throw "Icon not found at '$iconPath'."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = [string]$manifest.version_number
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "manifest.json has no version_number."
}

$pluginCode = Get-Content -LiteralPath $pluginPath -Raw
$pluginVersionMatch = [regex]::Match($pluginCode, 'PluginVersion\s*=\s*"([^"]+)"')
if (-not $pluginVersionMatch.Success) {
    throw "Could not read PluginVersion from Plugin.cs."
}
$pluginVersion = $pluginVersionMatch.Groups[1].Value
if ($pluginVersion -ne $version) {
    throw "Version mismatch: Plugin.cs has '$pluginVersion', manifest.json has '$version'."
}

if (-not (Test-Path -LiteralPath $packagePath)) {
    throw "package.json not found."
}

if (-not (Test-Path -LiteralPath $packageLockPath)) {
    throw "package-lock.json not found."
}

$package = Get-Content -LiteralPath $packagePath -Raw | ConvertFrom-Json
$packageVersion = [string]$package.version
if ($packageVersion -ne $version) {
    throw "Version mismatch: package.json has '$packageVersion', manifest.json has '$version'."
}

$packageLock = Get-Content -LiteralPath $packageLockPath -Raw
$lockVersionMatch = [regex]::Match($packageLock, '(?m)^\s{2}"version"\s*:\s*"([^"]+)"')
if (-not $lockVersionMatch.Success) {
    throw "Could not read the root version from package-lock.json."
}
$lockVersion = $lockVersionMatch.Groups[1].Value
if ($lockVersion -ne $version) {
    throw "Version mismatch: package-lock.json has '$lockVersion', manifest.json has '$version'."
}

Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Icon must be 256x256, got $($icon.Width)x$($icon.Height)."
    }
}
finally {
    $icon.Dispose()
}

if (-not $SkipTests) {
    Write-Host "Running core tests..."
    & $DotnetPath test $testProjectPath --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Core tests failed."
    }
}

Write-Host "Building $Configuration..."
& $DotnetPath build $projectPath -c $Configuration --nologo --verbosity minimal -p:SkipPluginCopy=true
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$builtDll = Join-Path $repoRoot "src\OpenWaters\bin\$Configuration\OpenWaters.dll"
if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Built DLL not found at '$builtDll'."
}

if (Test-Path -LiteralPath $stagingPath) {
    Remove-Item -LiteralPath $stagingPath -Recurse -Force
}
if (-not (Test-Path -LiteralPath $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

$pluginPackageDir = Join-Path $stagingPath "BepInEx\plugins\OpenWaters"
New-Item -ItemType Directory -Force -Path $pluginPackageDir | Out-Null

Copy-Item -LiteralPath $manifestPath -Destination $stagingPath
Copy-Item -LiteralPath $changelogPath -Destination $stagingPath
Copy-Item -LiteralPath $packageReadmePath -Destination (Join-Path $stagingPath "README.md")
Copy-Item -LiteralPath $licensePath -Destination $stagingPath
Copy-Item -LiteralPath $noticesPath -Destination $stagingPath
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $stagingPath "icon.png")
Copy-Item -LiteralPath $builtDll -Destination $pluginPackageDir

$zipName = "MkbewePP-OpenWaters-$version.zip"
$zipPath = Join-Path $outputPath $zipName
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $stagingPath "*") -DestinationPath $zipPath -CompressionLevel Optimal
if (-not $KeepStaging) {
    Remove-Item -LiteralPath $stagingPath -Recurse -Force
}

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
Write-Host ""
Write-Host "Release package: $zipPath"
Write-Host "SHA256: $($hash.Hash)"
Write-Host ""
Write-Host "Contents: manifest.json, icon.png, README.md, CHANGELOG.md, LICENSE, THIRD-PARTY-NOTICES.md, BepInEx/plugins/OpenWaters/OpenWaters.dll"
  Write-Host "No valheim.openwaters.cfg or OpenWaters.worldSettings are included."
