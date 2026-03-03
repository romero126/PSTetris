<#
.SYNOPSIS
    Publishes the PSTetris module to the PowerShell Gallery.
.DESCRIPTION
    Takes the staged module from the output directory (produced by
    Build-Module.ps1) and publishes it to PSGallery using the
    provided NuGet API key.
.EXAMPLE
    .\Publish-Module.ps1 -NuGetApiKey 'your-api-key-here'
.EXAMPLE
    $env:NUGET_API_KEY = 'your-api-key-here'
    .\Publish-Module.ps1
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$NuGetApiKey = $env:NUGET_API_KEY,

    [Parameter()]
    [string]$Repository = 'PSGallery',

    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

$ModulePath = Join-Path $PSScriptRoot 'output\PSTetris'

if (-not (Test-Path $ModulePath)) {
    throw "Module not found at '$ModulePath'. Run .\Build-Module.ps1 first."
}

if ([string]::IsNullOrWhiteSpace($NuGetApiKey)) {
    throw 'NuGet API key is required. Pass -NuGetApiKey or set $env:NUGET_API_KEY.'
}

$manifest = Test-ModuleManifest -Path (Join-Path $ModulePath 'PSTetris.psd1')
Write-Host "Publishing PSTetris v$($manifest.Version) to $Repository..." -ForegroundColor Cyan

$publishParams = @{
    Path        = $ModulePath
    NuGetApiKey = $NuGetApiKey
    Repository  = $Repository
}

if ($WhatIf) {
    $publishParams['WhatIf'] = $true
}

Publish-Module @publishParams

Write-Host 'Published successfully.' -ForegroundColor Green
