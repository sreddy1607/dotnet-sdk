#!/usr/bin/env bash

# make NuGet network operations more robust
export NUGET_ENABLE_EXPERIMENTAL_HTTP_RETRY=true
export NUGET_EXPERIMENTAL_MAX_NETWORK_TRY_COUNT=6
export NUGET_EXPERIMENTAL_NETWORK_RETRY_DELAY_MILLISECONDS=1000

export MicrosoftNETBuildExtensionsTargets=$HELIX_CORRELATION_PAYLOAD/ex/msbuildExtensions/Microsoft/Microsoft.NET.Build.Extensions/Microsoft.NET.Build.Extensions.targets
export DOTNET_ROOT=$HELIX_CORRELATION_PAYLOAD/d
export PATH=$DOTNET_ROOT:$PATH

export TestExecutionDirectory=$(pwd)/testExecutionDirectory
mkdir $TestExecutionDirectory
export DOTNET_CLI_HOME=$TestExecutionDirectory/.dotnet
cp -a $HELIX_CORRELATION_PAYLOAD/t/TestExecutionDirectoryFiles/. $TestExecutionDirectory/

# call dotnet new so the first run message doesn't interfere with the first test
dotnet new --debug:ephemeral-hive

find $TestExecutionDirectory/. -name nuget.config
dotnet nuget list source --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet6-transport --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet6-internal-transport --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet7-transport --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet7-internal-transport --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source richnav --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source vs-impl --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet-libraries-transport --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet-tools-transport --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet-libraries --configfile $TestExecutionDirectory/nuget.config
dotnet nuget remove source dotnet-eng --configfile $TestExecutionDirectory/nuget.config
dotnet nuget list source --configfile $TestExecutionDirectory/nuget.config

cp $HELIX_CORRELATION_PAYLOAD/t/TestExecutionDirectoryFiles/testAsset.props ./
export TestPackagesRoot=$(pwd)/Assets/TestPackages
dotnet build ./Assets/TestPackages/Microsoft.NET.TestPackages.csproj /t:Build -p:VersionPropsIsImported=false
cp $TestPackagesRoot/TestPackages/. $TestExecutionDirectory/TestPackages -R -v
find $TestExecutionDirectory/TestPackages -name *.nupkg
dotnet nuget add source $TestExecutionDirectory/TestPackages
