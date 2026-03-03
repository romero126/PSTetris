<#
.SYNOPSIS
    Builds the PSTetris module into a publishable layout.
.DESCRIPTION
    Compiles the C# project in Release mode and stages the module
    artifacts (DLL + manifest) into an output directory ready for
    Publish-Module.
.EXAMPLE
    .\Build-Module.ps1
    .\Build-Module.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$ProjectRoot = $PSScriptRoot
$ProjectDir  = Join-Path $ProjectRoot 'src\PSTetris'
$OutputDir   = Join-Path $ProjectRoot 'output\PSTetris'

Write-Host "Building PSTetris ($Configuration)..." -ForegroundColor Cyan
dotnet build $ProjectDir -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$BinDir = Join-Path $ProjectDir "bin\$Configuration\netstandard2.0"

Copy-Item (Join-Path $BinDir 'PSTetris.dll') -Destination $OutputDir
Copy-Item (Join-Path $ProjectDir 'PSTetris.psd1') -Destination $OutputDir

Write-Host "Module staged at: $OutputDir" -ForegroundColor Green
Write-Host 'Run .\Publish-Module.ps1 to publish to PSGallery.' -ForegroundColor Yellow
