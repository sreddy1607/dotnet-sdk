## Welcome to dotnet sdk test CI

This repo contains core functionality needed to create .NET projects that is shared between VisualStudio and CLI.

* MSBuild tasks can be found under [/src/Tasks/Microsoft.NET.Build.Tasks/](src/Tasks/Microsoft.NET.Build.Tasks).

Please refer to [dotnet/project-system](https://github.com/dotnet/project-system) for the project system work that is specific to Visual Studio.

## Build status

|Windows x64 |
|:------:|
|[![](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/sdk/DotNet-Core-Sdk%203.0%20(Windows)%20(YAML)%20(Official))](https://dev.azure.com/dnceng/internal/_build?definitionId=140)|

## Testing a local build

To test your locally built SDK, run `eng\dogfood.cmd` after building. That script starts a new Powershell with the environment configured to redirect SDK resolution to your build.

From that shell your SDK will be available in:

- any Visual Studio instance launched (via `& devenv.exe`)
- `dotnet build`
- `msbuild`

## Installing the SDK
[Official builds](https://dotnet.microsoft.com/download/dotnet-core)

[Latest builds](https://github.com/dotnet/installer#installers-and-binaries)

## How do I engage and contribute?

We welcome you to try things out, [file issues](https://github.com/dotnet/sdk/issues), make feature requests and join us in design conversations. Also be sure to check out our [project documentation](documentation) and [Developer Guide](documentation/project-docs/developer-guide.md).

This project has adopted the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct) to clarify expected behavior in our community.
