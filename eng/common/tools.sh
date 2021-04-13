#!/usr/bin/env bash

# Initialize variables if they aren't already defined.

# CI mode - set to true on CI server for PR validation build or official build.
ci=${ci:-false}

# Set to true to use the pipelines logger which will enable Azure logging output.
# https://github.com/Microsoft/azure-pipelines-tasks/blob/master/docs/authoring/commands.md
# This flag is meant as a temporary opt-opt for the feature while validate it across
# our consumers. It will be deleted in the future.
if [[ "$ci" == true ]]; then
  pipelines_log=${pipelines_log:-true}
else
  pipelines_log=${pipelines_log:-false}
fi

# Build configuration. Common values include 'Debug' and 'Release', but the repository may use other names.
configuration=${configuration:-'Debug'}

# Set to true to opt out of outputting binary log while running in CI
exclude_ci_binary_log=${exclude_ci_binary_log:-false}

if [[ "$ci" == true && "$exclude_ci_binary_log" == false ]]; then
  binary_log_default=true
else
  binary_log_default=false
fi

# Set to true to output binary log from msbuild. Note that emitting binary log slows down the build.
binary_log=${binary_log:-$binary_log_default}

# Turns on machine preparation/clean up code that changes the machine state (e.g. kills build processes).
prepare_machine=${prepare_machine:-false}

# True to restore toolsets and dependencies.
restore=${restore:-true}

# Allows restoring .NET Core Runtimes and SDKs from alternative feeds
runtimeSourceFeed=${runtimeSourceFeed:-""}
runtimeSourceFeedKey=${runtimeSourceFeedKey:-""}

# Adjusts msbuild verbosity level.
verbosity=${verbosity:-'minimal'}

# Set to true to reuse msbuild nodes. Recommended to not reuse on CI.
if [[ "$ci" == true ]]; then
  node_reuse=${node_reuse:-false}
else
  node_reuse=${node_reuse:-true}
fi

# Configures warning treatment in msbuild.
warn_as_error=${warn_as_error:-true}

# True to attempt using .NET Core already that meets requirements specified in global.json
# installed on the machine instead of downloading one.
use_installed_dotnet_cli=${use_installed_dotnet_cli:-true}

# Enable repos to use a particular version of the on-line dotnet-install scripts.
#    default URL: https://dot.net/v1/dotnet-install.sh
dotnetInstallScriptVersion=${dotnetInstallScriptVersion:-'v1'}

# True to use global NuGet cache instead of restoring packages to repository-local directory.
if [[ "$ci" == true ]]; then
  use_global_nuget_cache=${use_global_nuget_cache:-false}
else
  use_global_nuget_cache=${use_global_nuget_cache:-true}
fi

# Used when restoring .NET SDK from alternative feeds
runtime_source_feed=${runtime_source_feed:-''}
runtime_source_feed_key=${runtime_source_feed_key:-''}

# Resolve any symlinks in the given path.
function ResolvePath {
  local path=$1

  while [[ -h $path ]]; do
    local dir="$( cd -P "$( dirname "$path" )" && pwd )"
    path="$(readlink "$path")"

    # if $path was a relative symlink, we need to resolve it relative to the path where the
    # symlink file was located
    [[ $path != /* ]] && path="$dir/$path"
  done

  # return value
  _ResolvePath="$path"
}

# ReadVersionFromJson [json key]
function ReadGlobalVersion {
  local key=$1

  if command -v jq &> /dev/null; then
    _ReadGlobalVersion="$(jq -r ".[] | select(has(\"$key\")) | .\"$key\"" "$global_json_file")"
  elif [[ "$(cat "$global_json_file")" =~ \"$key\"[[:space:]\:]*\"([^\"]+) ]]; then
    _ReadGlobalVersion=${BASH_REMATCH[1]}
  fi

  if [[ -z "$_ReadGlobalVersion" ]]; then
    Write-PipelineTelemetryError -category 'Build' "Error: Cannot find \"$key\" in $global_json_file"
    ExitWithExitCode 1
  fi
}

function InitializeDotNetCli {
  if [[ -n "${_InitializeDotNetCli:-}" ]]; then
    return
  fi

  local install=$1
  local runtimeSourceFeedArg=""
  local runtimeSourceFeedKeyArg=""
  if [[ $# == 3 ]]; then
    runtimeSourceFeedArg=$2
    runtimeSourceFeedKeyArg=$3
  fi

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  export DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we want to control all package sources
  export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Disable telemetry on CI
  if [[ $ci == true ]]; then
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
  fi

  # LTTNG is the logging infrastructure used by Core CLR. Need this variable set
  # so it doesn't output warnings to the console.
  export LTTNG_HOME="$HOME"

  # Source Build uses DotNetCoreSdkDir variable
  if [[ -n "${DotNetCoreSdkDir:-}" ]]; then
    export DOTNET_INSTALL_DIR="$DotNetCoreSdkDir"
  fi

  # Find the first path on $PATH that contains the dotnet.exe
  if [[ "$use_installed_dotnet_cli" == true && $global_json_has_runtimes == false && -z "${DOTNET_INSTALL_DIR:-}" ]]; then
    local dotnet_path=`command -v dotnet`
    if [[ -n "$dotnet_path" ]]; then
      ResolvePath "$dotnet_path"
      export DOTNET_INSTALL_DIR=`dirname "$_ResolvePath"`
    fi
  fi

  ReadGlobalVersion "dotnet"
  local dotnet_sdk_version=$_ReadGlobalVersion
  local dotnet_root=""

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version,
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if [[ $global_json_has_runtimes == false && -n "${DOTNET_INSTALL_DIR:-}" && -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
    dotnet_root="$DOTNET_INSTALL_DIR"
  else
    dotnet_root="$repo_root/.dotnet"

    export DOTNET_INSTALL_DIR="$dotnet_root"

    if [[ ! -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
      if [[ "$install" == true ]]; then
        InstallDotNetSdk "$dotnet_root" "$dotnet_sdk_version" "unset" $runtimeSourceFeedArg $runtimeSourceFeedKeyArg
      else
        Write-PipelineTelemetryError -category 'InitializeToolset' "Unable to find dotnet with SDK version '$dotnet_sdk_version'"
        ExitWithExitCode 1
      fi
    fi
  fi

  # Add dotnet to PATH. This prevents any bare invocation of dotnet in custom
  # build steps from using anything other than what we've downloaded.
  Write-PipelinePrependPath -path "$dotnet_root"

  Write-PipelineSetVariable -name "DOTNET_MULTILEVEL_LOOKUP" -value "0"
  Write-PipelineSetVariable -name "DOTNET_SKIP_FIRST_TIME_EXPERIENCE" -value "1"

  # return value
  _InitializeDotNetCli="$dotnet_root"
}

function InstallDotNetSdk {
  local root=$1
  local version=$2
  local architecture="unset"
<<<<<<< HEAD
  if [[ $# -ge 3 ]]; then
    architecture=$3
  fi
  InstallDotNet "$root" "$version" $architecture 'sdk' 'false' $runtime_source_feed $runtime_source_feed_key
=======
  if [[ $# == 3 ]]; then
    architecture=$3
  fi
  InstallDotNet "$root" "$version" $architecture "not-a-runtime" 0 $runtimeSourceFeed $runtimeSourceFeedKey
>>>>>>> 5565e6b21b7a11560fb88e73dce4c097fac6260d
}

function InstallDotNet {
  local root=$1
  local version=$2

  GetDotNetInstallScript "$root"
  local install_script=$_GetDotNetInstallScript

  local archArg=''
<<<<<<< HEAD
  if [[ -n "${3:-}" ]] && [ "$3" != 'unset' ]; then
    archArg="--architecture $3"
  fi
  local runtimeArg=''
  if [[ -n "${4:-}" ]] && [ "$4" != 'sdk' ]; then
=======
  if [[ -n "${3:-}" && "$3" != "unset"  ]]; then
    archArg="--architecture $3"
  fi
  local runtimeArg=''
  if [[ -n "${4:-}" && "$4" != "not-a-runtime" ]]; then
>>>>>>> 5565e6b21b7a11560fb88e73dce4c097fac6260d
    runtimeArg="--runtime $4"
  fi
  local skipNonVersionedFilesArg=""
  if [[ "$#" -ge "5" ]] && [[ "$5" != 'false' ]]; then
    skipNonVersionedFilesArg="--skip-non-versioned-files"
  fi
  bash "$install_script" --version $version --install-dir "$root" $archArg $runtimeArg $skipNonVersionedFilesArg || {
    local exit_code=$?
<<<<<<< HEAD
    echo "Failed to install dotnet SDK from public location (exit code '$exit_code')."
=======
>>>>>>> 5565e6b21b7a11560fb88e73dce4c097fac6260d

    local runtimeSourceFeed=''
    if [[ -n "${6:-}" ]]; then
      runtimeSourceFeed="--azure-feed $6"
    fi

    local runtimeSourceFeedKey=''
    if [[ -n "${7:-}" ]]; then
      # The 'base64' binary on alpine uses '-d' and doesn't support '--decode'
      # '-d'. To work around this, do a simple detection and switch the parameter
      # accordingly.
      decodeArg="--decode"
      if base64 --help 2>&1 | grep -q "BusyBox"; then
          decodeArg="-d"
      fi
      decodedFeedKey=`echo $7 | base64 $decodeArg`
      runtimeSourceFeedKey="--feed-credential $decodedFeedKey"
<<<<<<< HEAD
=======
    else
      Write-PipelineTelemetryError -category 'InitializeToolset' "Failed to install dotnet SDK from public location (exit code '$exit_code')."
>>>>>>> 5565e6b21b7a11560fb88e73dce4c097fac6260d
    fi

    if [[ -n "$runtimeSourceFeed" || -n "$runtimeSourceFeedKey" ]]; then
      bash "$install_script" --version $version --install-dir "$root" $archArg $runtimeArg $skipNonVersionedFilesArg $runtimeSourceFeed $runtimeSourceFeedKey || {
        local exit_code=$?
        Write-PipelineTelemetryError -category 'InitializeToolset' "Failed to install dotnet SDK from custom location '$runtimeSourceFeed' (exit code '$exit_code')."
        ExitWithExitCode $exit_code
      }
    else
<<<<<<< HEAD
      if [[ $exit_code != 0 ]]; then
        Write-PipelineTelemetryError -category 'InitializeToolset' "Failed to install dotnet SDK from public location (exit code '$exit_code')."
      fi
=======
>>>>>>> 5565e6b21b7a11560fb88e73dce4c097fac6260d
      ExitWithExitCode $exit_code
    fi
  }
}

function with_retries {
  local maxRetries=5
  local retries=1
  echo "Trying to run '$@' for maximum of $maxRetries attempts."
  while [[ $((retries++)) -le $maxRetries ]]; do
    "$@"

    if [[ $? == 0 ]]; then
      echo "Ran '$@' successfully."
      return 0
    fi

    timeout=$((3**$retries-1))
    echo "Failed to execute '$@'. Waiting $timeout seconds before next attempt ($retries out of $maxRetries)." 1>&2
    sleep $timeout
  done

  echo "Failed to execute '$@' for $maxRetries times." 1>&2

  return 1
}

function GetDotNetInstallScript {
  local root=$1
  local install_script="$root/dotnet-install.sh"
  local install_script_url="https://dot.net/$dotnetInstallScriptVersion/dotnet-install.sh"

  if [[ ! -a "$install_script" ]]; then
    mkdir -p "$root"

    echo "Downloading '$install_script_url'"

    # Use curl if available, otherwise use wget
    if command -v curl > /dev/null; then
      # first, try directly, if this fails we will retry with verbose logging
      curl "$install_script_url" -sSL --retry 10 --create-dirs -o "$install_script" || {
        if command -v openssl &> /dev/null; then
          echo "Curl failed; dumping some information about dotnet.microsoft.com for later investigation"
          echo | openssl s_client -showcerts -servername dotnet.microsoft.com  -connect dotnet.microsoft.com:443
        fi
        echo "Will now retry the same URL with verbose logging."
        with_retries curl "$install_script_url" -sSL --verbose --retry 10 --create-dirs -o "$install_script" || {
          local exit_code=$?
          Write-PipelineTelemetryError -category 'InitializeToolset' "Failed to acquire dotnet install script (exit code '$exit_code')."
          ExitWithExitCode $exit_code
        }
      }
    else
      with_retries wget -v -O "$install_script" "$install_script_url" || {
        local exit_code=$?
        Write-PipelineTelemetryError -category 'InitializeToolset' "Failed to acquire dotnet install script (exit code '$exit_code')."
        ExitWithExitCode $exit_code
      }
    fi
  fi
  # return value
  _GetDotNetInstallScript="$install_script"
}

function InitializeBuildTool {
  if [[ -n "${_InitializeBuildTool:-}" ]]; then
    return
  fi
<<<<<<< HEAD

  InitializeDotNetCli $restore
=======
  
  InitializeDotNetCli $restore $runtimeSourceFeed $runtimeSourceFeedKey
>>>>>>> 5565e6b21b7a11560fb88e73dce4c097fac6260d

  # return values
  _InitializeBuildTool="$_InitializeDotNetCli/dotnet"
  _InitializeBuildToolCommand="msbuild"
  _InitializeBuildToolFramework="netcoreapp3.1"
}

# Set RestoreNoCache as a workaround for https://github.com/NuGet/Home/issues/3116
function GetNuGetPackageCachePath {
  if [[ -z ${NUGET_PACKAGES:-} ]]; then
    if [[ "$use_global_nuget_cache" == true ]]; then
      export NUGET_PACKAGES="$HOME/.nuget/packages"
    else
      export NUGET_PACKAGES="$repo_root/.packages"
      export RESTORENOCACHE=true
    fi
  fi

  # return value
  _GetNuGetPackageCachePath=$NUGET_PACKAGES
}

function InitializeNativeTools() {
  if [[ -n "${DisableNativeToolsetInstalls:-}" ]]; then
    return
  fi
  if grep -Fq "native-tools" $global_json_file
  then
    local nativeArgs=""
    if [[ "$ci" == true ]]; then
      nativeArgs="--installDirectory $tools_dir"
    fi
    "$_script_dir/init-tools-native.sh" $nativeArgs
  fi
}

function InitializeToolset {
  if [[ -n "${_InitializeToolset:-}" ]]; then
    return
  fi

  GetNuGetPackageCachePath

  ReadGlobalVersion "Microsoft.DotNet.Arcade.Sdk"

  local toolset_version=$_ReadGlobalVersion
  local toolset_location_file="$toolset_dir/$toolset_version.txt"

  if [[ -a "$toolset_location_file" ]]; then
    local path=`cat "$toolset_location_file"`
    if [[ -a "$path" ]]; then
      # return value
      _InitializeToolset="$path"
      return
    fi
  fi

  if [[ "$restore" != true ]]; then
    Write-PipelineTelemetryError -category 'InitializeToolset' "Toolset version $toolset_version has not been restored."
    ExitWithExitCode 2
  fi

  local proj="$toolset_dir/restore.proj"

  local bl=""
  if [[ "$binary_log" == true ]]; then
    bl="/bl:$log_dir/ToolsetRestore.binlog"
  fi

  echo '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' > "$proj"
  MSBuild-Core "$proj" $bl /t:__WriteToolsetLocation /clp:ErrorsOnly\;NoSummary /p:__ToolsetLocationOutputFile="$toolset_location_file"

  local toolset_build_proj=`cat "$toolset_location_file"`

  if [[ ! -a "$toolset_build_proj" ]]; then
    Write-PipelineTelemetryError -category 'Build' "Invalid toolset path: $toolset_build_proj"
    ExitWithExitCode 3
  fi

  # return value
  _InitializeToolset="$toolset_build_proj"
}

function ExitWithExitCode {
  if [[ "$ci" == true && "$prepare_machine" == true ]]; then
    StopProcesses
  fi
  exit $1
}

function StopProcesses {
  echo "Killing running build processes..."
  pkill -9 "dotnet" || true
  pkill -9 "vbcscompiler" || true
  return 0
}

function MSBuild {
  local args=$@
  if [[ "$pipelines_log" == true ]]; then
    InitializeBuildTool
    InitializeToolset

    if [[ "$ci" == true ]]; then
      export NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS=20
      export NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS=20
      Write-PipelineSetVariable -name "NUGET_PLUGIN_HANDSHAKE_TIMEOUT_IN_SECONDS" -value "20"
      Write-PipelineSetVariable -name "NUGET_PLUGIN_REQUEST_TIMEOUT_IN_SECONDS" -value "20"
    fi

    local toolset_dir="${_InitializeToolset%/*}"
    # new scripts need to work with old packages, so we need to look for the old names/versions
    local selectedPath=
    local possiblePaths=()
    possiblePaths+=( "$toolset_dir/$_InitializeBuildToolFramework/Microsoft.DotNet.ArcadeLogging.dll" )
    possiblePaths+=( "$toolset_dir/$_InitializeBuildToolFramework/Microsoft.DotNet.Arcade.Sdk.dll" )
    possiblePaths+=( "$toolset_dir/netcoreapp2.1/Microsoft.DotNet.ArcadeLogging.dll" )
    possiblePaths+=( "$toolset_dir/netcoreapp2.1/Microsoft.DotNet.Arcade.Sdk.dll" )
    for path in "${possiblePaths[@]}"; do
      if [[ -f $path ]]; then
        selectedPath=$path
        break
      fi
    done
    if [[ -z "$selectedPath" ]]; then
      Write-PipelineTelemetryError -category 'Build'  "Unable to find arcade sdk logger assembly."
      ExitWithExitCode 1
    fi
    args+=( "-logger:$selectedPath" )
  fi

  MSBuild-Core ${args[@]}
}

function MSBuild-Core {
  if [[ "$ci" == true ]]; then
    if [[ "$binary_log" != true && "$exclude_ci_binary_log" != true ]]; then
      Write-PipelineTelemetryError -category 'Build'  "Binary log must be enabled in CI build, or explicitly opted-out from with the -noBinaryLog switch."
      ExitWithExitCode 1
    fi

    if [[ "$node_reuse" == true ]]; then
      Write-PipelineTelemetryError -category 'Build'  "Node reuse must be disabled in CI build."
      ExitWithExitCode 1
    fi
  fi

  InitializeBuildTool

  local warnaserror_switch=""
  if [[ $warn_as_error == true ]]; then
    warnaserror_switch="/warnaserror"
  fi

  function RunBuildTool {
    export ARCADE_BUILD_TOOL_COMMAND="$_InitializeBuildTool $@"

    "$_InitializeBuildTool" "$@" || {
      local exit_code=$?
      # We should not Write-PipelineTaskError here because that message shows up in the build summary
      # The build already logged an error, that's the reason it failed. Producing an error here only adds noise.
      echo "Build failed with exit code $exit_code. Check errors above."
      if [[ "$ci" == "true" ]]; then
        Write-PipelineSetResult -result "Failed" -message "msbuild execution failed."
        # Exiting with an exit code causes the azure pipelines task to log yet another "noise" error
        # The above Write-PipelineSetResult will cause the task to be marked as failure without adding yet another error
        ExitWithExitCode 0
      else
        ExitWithExitCode $exit_code
      fi
    }
  }

  RunBuildTool "$_InitializeBuildToolCommand" /m /nologo /clp:Summary /v:$verbosity /nr:$node_reuse $warnaserror_switch /p:TreatWarningsAsErrors=$warn_as_error /p:ContinuousIntegrationBuild=$ci "$@"
}

ResolvePath "${BASH_SOURCE[0]}"
_script_dir=`dirname "$_ResolvePath"`

. "$_script_dir/pipeline-logging-functions.sh"

eng_root=`cd -P "$_script_dir/.." && pwd`
repo_root=`cd -P "$_script_dir/../.." && pwd`
artifacts_dir="$repo_root/artifacts"
toolset_dir="$artifacts_dir/toolset"
tools_dir="$repo_root/.tools"
log_dir="$artifacts_dir/log/$configuration"
temp_dir="$artifacts_dir/tmp/$configuration"

global_json_file="$repo_root/global.json"
# determine if global.json contains a "runtimes" entry
global_json_has_runtimes=false
if command -v jq &> /dev/null; then
  if jq -er '. | select(has("runtimes"))' "$global_json_file" &> /dev/null; then
    global_json_has_runtimes=true
  fi
elif [[ "$(cat "$global_json_file")" =~ \"runtimes\"[[:space:]\:]*\{ ]]; then
  global_json_has_runtimes=true
fi

# HOME may not be defined in some scenarios, but it is required by NuGet
if [[ -z $HOME ]]; then
  export HOME="$repo_root/artifacts/.home/"
  mkdir -p "$HOME"
fi

mkdir -p "$toolset_dir"
mkdir -p "$temp_dir"
mkdir -p "$log_dir"

Write-PipelineSetVariable -name "Artifacts" -value "$artifacts_dir"
Write-PipelineSetVariable -name "Artifacts.Toolset" -value "$toolset_dir"
Write-PipelineSetVariable -name "Artifacts.Log" -value "$log_dir"
Write-PipelineSetVariable -name "Temp" -value "$temp_dir"
Write-PipelineSetVariable -name "TMP" -value "$temp_dir"

# Import custom tools configuration, if present in the repo.
if [ -z "${disable_configure_toolset_import:-}" ]; then
  configure_toolset_script="$eng_root/configure-toolset.sh"
  if [[ -a "$configure_toolset_script" ]]; then
    . "$configure_toolset_script"
  fi
fi

# TODO: https://github.com/dotnet/arcade/issues/1468
# Temporary workaround to avoid breaking change.
# Remove once repos are updated.
if [[ -n "${useInstalledDotNetCli:-}" ]]; then
  use_installed_dotnet_cli="$useInstalledDotNetCli"
fi
