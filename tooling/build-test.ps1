[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$DotnetPath = "$env:USERPROFILE\.dotnet\dotnet.exe",
    [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot "manifest.json"
$packagePath = Join-Path $repoRoot "package.json"
$packageLockPath = Join-Path $repoRoot "package-lock.json"
$pluginPath = Join-Path $repoRoot "src\OpenWaters\Plugin.cs"
$counterPath = Join-Path $repoRoot "tooling\test-build-counter.txt"
$buildScript = Join-Path $PSScriptRoot "build-release.ps1"
$utf8 = New-Object System.Text.UTF8Encoding($false)

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$baseVersion = [string]$manifest.version_number
if ($baseVersion -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
    throw "Expected a three-part base version in manifest.json, got '$baseVersion'."
}
$major = $Matches[1]
$minor = $Matches[2]
$patch = $Matches[3]

# r2modman/Thunderstore reject a fourth component and fall back to 0.0.0,
# which defeats the purpose of a unique cache entry. Keep three parts and
# derive the test patch from the current release patch (0.1.2 -> 0.1.2001).
$testPatchBase = [int]$patch * 1000

$counter = 1
if (Test-Path -LiteralPath $counterPath) {
    $raw = (Get-Content -LiteralPath $counterPath -Raw).Trim()
    if ($raw -match '^\d+$') {
        $counter = [int]$raw + 1
    }
}
$testVersion = "$major.$minor.$($testPatchBase + $counter)"

function Replace-Version([string]$text, [string]$pattern, [string]$value) {
    return [regex]::Replace($text, $pattern, {
        param($match)
        return $match.Groups[1].Value + $value + $match.Groups[2].Value
    })
}

$files = @($manifestPath, $packagePath, $packageLockPath, $pluginPath)
$originals = @{}
foreach ($file in $files) {
    $originals[$file] = [System.IO.File]::ReadAllText($file)
}

try {
    [System.IO.File]::WriteAllText(
        $manifestPath,
        (Replace-Version $originals[$manifestPath] '("version_number"\s*:\s*")[^"]+(")' $testVersion),
        $utf8)
    [System.IO.File]::WriteAllText(
        $packagePath,
        (Replace-Version $originals[$packagePath] '(?m)^(\s{2}"version"\s*:\s*")[^"]+(")' $testVersion),
        $utf8)
    [System.IO.File]::WriteAllText(
        $packageLockPath,
        (Replace-Version $originals[$packageLockPath] '(?m)^(\s{2}"version"\s*:\s*")[^"]+(")' $testVersion),
        $utf8)
    [System.IO.File]::WriteAllText(
        $pluginPath,
        (Replace-Version $originals[$pluginPath] '(PluginVersion\s*=\s*")[^"]+(")' $testVersion),
        $utf8)
    [System.IO.File]::WriteAllText($counterPath, [string]$counter, $utf8)

    $arguments = @{
        Configuration = $Configuration
        OutputDir     = "release/test"
        DotnetPath    = $DotnetPath
    }
    if ($SkipTests) {
        $arguments["SkipTests"] = $true
    }
    & $buildScript @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Test build failed."
    }

    Write-Host ""
    Write-Host "Test package: $(Join-Path $repoRoot "release\test\MkbewePP-OpenWaters-$testVersion.zip")"
    Write-Host "Version: $testVersion (source files restored to base version)."
}
finally {
    foreach ($file in $files) {
        [System.IO.File]::WriteAllText($file, $originals[$file], $utf8)
    }
}
