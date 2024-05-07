# We can't use already installed dotnet cli since we need to install additional shared runtimes.
# We could potentially try to find an existing installation that has all the required runtimes,
# but it's unlikely one will be available.

useInstalledDotNetCli="false"

# Working around issue https://github.com/dotnet/arcade/issues/7327
DisableNativeToolsetInstalls=true