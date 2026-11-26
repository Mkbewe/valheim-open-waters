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

# r2modman/Thunderstore reject a fourth component and leading zeros. Keep three
# parts and derive the test patch from the current release patch (0.1.2 ->
# 0.1.2001). Patch zero has no thousands prefix, so its tests start at 101
# (0.2.0 -> 0.2.101) to stay three-digit and leave 0.2.1..0.2.100 for releases.
$testPatchBase = [Math]::Max([int]$patch * 1000, 100)
$counterLimit = if ([int]$patch -eq 0) { 899 } else { 999 }

$counter = 0
if (Test-Path -LiteralPath $counterPath) {
    $raw = (Get-Content -LiteralPath $counterPath -Raw).Trim()
    if ($raw -match '^(\d+\.\d+\.\d+):(\d+)$') {
        if ($Matches[1] -eq $baseVersion) {
            $counter = [int]$Matches[2]
        }
    } elseif ($raw -match '^\d+$') {
        # Legacy counter without a stored base version. Patch-zero test builds
        # used 0.2.1..0.2.3, which do not collide with the new 0.2.101.. block,
        # so start that block clean; higher patches keep their old counter.
        if ([int]$patch -ne 0) {
            $counter = [int]$raw
        }
    }
}
$counter++
if ($counter -gt $counterLimit) {
    throw "Test build counter $counter exceeds the reserved block for $baseVersion ($counterLimit builds). Delete $counterPath to start a new block."
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
    [System.IO.File]::WriteAllText($counterPath, "$baseVersion`:$counter", $utf8)

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
