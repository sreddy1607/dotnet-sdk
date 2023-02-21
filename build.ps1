#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string]$Platform="Any CPU",
    [string]$Verbosity="minimal",
    [switch]$SkipTests,
    [switch]$FullMSBuild,
    [switch]$RealSign,
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [Options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -Platform <PLATFORM>               Build the specified Platform (Any CPU)"
    Write-Host "  -Verbosity <VERBOSITY>             Build console output verbosity (minimal or diagnostic, default: minimal)"
    Write-Host "  -SkipTests                         Skip executing unit tests"
    Write-Host "  -FullMSBuild                       Run tests with the full .NET Framework version of MSBuild instead of the .NET Core version"
    Write-Host "  -RealSign                          Sign the output DLLs"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$RepoRoot = "$PSScriptRoot"
$PackagesPath = "$RepoRoot\packages"
$env:NUGET_PACKAGES = $PackagesPath
$DotnetCLIVersion = Get-Content "$RepoRoot\DotnetCLIVersion.txt"

# Disable first run since we want to control all package sources
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

$logPath = "$RepoRoot\bin\log"
if (!(Test-Path -Path $logPath)) {
    New-Item -Path $logPath -Force -ItemType 'Directory' | Out-Null
}

$signType = 'public'
if ($RealSign) {
    $signType = 'real'
}

msbuild /t:restore /p:RestorePackagesPath=$PackagesPath /m:1 /nologo /p:Configuration=$Configuration /p:Platform=$Platform /p:SignType=$signType $RepoRoot\src\VsixV3\PreEmptive.Solutions.Dotfuscator.CE.vsmanproj
msbuild /p:RestorePackagesPath=$PackagesPath /m:1 /nologo /p:Configuration=$Configuration /p:Platform=$Platform /p:SignType=$signType $RepoRoot\src\VsixV3\PreEmptive.Solutions.Dotfuscator.CE.vsmanproj

