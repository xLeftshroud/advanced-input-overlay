<#
.SYNOPSIS
    Build a versioned, portable release zip for Advanced Input Overlay.

.DESCRIPTION
    Produces dist/AdvancedInputOverlay-v<Version>.zip with the standard layout:

        AdvancedInputOverlay-v<Version>/
        +-- AdvancedInputOverlay.exe       (self-contained, single-file, no .pdb)
        +-- README.md
        +-- LICENSE
        +-- samples/
        |   +-- *.json, *.png
        +-- tools/
            +-- Convert-ObsLayout.ps1

    Source code (src/), build intermediates (bin/, obj/), IDE caches, git
    metadata, debug symbols, and the user's runtime config.json are excluded.

    The script can be run from any working directory; paths resolve relative
    to the repo root (the parent of scripts/).

.PARAMETER Version
    Semantic version string without the leading 'v' (e.g. "1.0.0", "1.2.3-rc1").

.PARAMETER Rid
    .NET Runtime Identifier for the publish target. Default: win-x64.

.PARAMETER SkipBuild
    Reuse an existing publish/ output instead of re-running dotnet publish.
    Useful when iterating on the staging / zipping logic only.

.EXAMPLE
    .\scripts\Build-Release.ps1 -Version 1.0.0

.EXAMPLE
    .\scripts\Build-Release.ps1 -Version 1.0.0 -Rid win-arm64
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = 'Semver string without leading v (e.g. 1.0.0)')]
    [ValidatePattern('^\d+\.\d+\.\d+(-[\w\.]+)?$')]
    [string]$Version,

    [string]$Rid = 'win-x64',

    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
[Environment]::CurrentDirectory = $PWD.Path

# Resolve repo root (parent of the scripts/ folder this file lives in)
$repoRoot   = Split-Path -Parent $PSScriptRoot
$csproj     = Join-Path $repoRoot 'src\AdvancedInputOverlay.csproj'
$publishDir = Join-Path $repoRoot 'publish'
$distDir    = Join-Path $repoRoot 'dist'
$exeName    = 'AdvancedInputOverlay.exe'

if (-not (Test-Path -LiteralPath $csproj)) {
    throw "Could not find $csproj — is this script located inside the repo's scripts/ folder?"
}

# ---------------------------------------------------------------- build ----

if ($SkipBuild) {
    Write-Host "==> Skipping build (re-using $publishDir)" -ForegroundColor Yellow
    if (-not (Test-Path -LiteralPath (Join-Path $publishDir $exeName))) {
        throw "publish/$exeName not found — drop -SkipBuild to rebuild."
    }
}
else {
    Write-Host "==> Cleaning previous publish/ and dist/..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue

    Write-Host "==> dotnet publish (rid=$Rid, self-contained, single-file)..." -ForegroundColor Cyan
    dotnet publish $csproj `
        -c Release `
        -r $Rid `
        --self-contained `
        -p:PublishSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit code $LASTEXITCODE)"
    }
}

# -------------------------------------------------------------- stage ------

Remove-Item -Recurse -Force $distDir -ErrorAction SilentlyContinue

$releaseName = "AdvancedInputOverlay-v$Version"
$stagingDir  = Join-Path $distDir $releaseName

Write-Host "==> Staging release contents to $stagingDir..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null

# Mandatory assets
Copy-Item -LiteralPath (Join-Path $publishDir $exeName)   -Destination $stagingDir
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md')  -Destination $stagingDir
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE')    -Destination $stagingDir
Copy-Item -Recurse     -LiteralPath (Join-Path $repoRoot 'samples') -Destination $stagingDir
Copy-Item -Recurse     -LiteralPath (Join-Path $repoRoot 'tools')   -Destination $stagingDir

# ---------------------------------------------------------------- zip ------

$zipPath = "$stagingDir.zip"

Write-Host "==> Compressing -> $zipPath" -ForegroundColor Cyan
Compress-Archive -Path $stagingDir -DestinationPath $zipPath -Force

# --------------------------------------------------------------- summary ---

$zipSizeMB    = [math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 1)
$stagingSizeMB = [math]::Round(
    ((Get-ChildItem -Recurse -LiteralPath $stagingDir | Measure-Object -Property Length -Sum).Sum / 1MB),
    1
)

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host "  OK: AdvancedInputOverlay v$Version ($Rid)" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green
Write-Host "  Staged folder : $stagingDir ($stagingSizeMB MB uncompressed)"
Write-Host "  Release zip   : $zipPath ($zipSizeMB MB)"
Write-Host ""
Write-Host "  Next steps:"
Write-Host "    1. git tag v$Version && git push --tags"
Write-Host "    2. Upload $zipPath to https://github.com/<you>/<repo>/releases/new"
Write-Host ""
