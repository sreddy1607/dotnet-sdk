// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Xunit;
using static Microsoft.NET.TestFramework.Commands.MSBuildTest;

namespace Microsoft.NET.Publish.Tests
{
    public class GivenThatWeWantToPublishAHelloWorldProject
    {
        private TestAssetsManager _testAssetsManager;

        public GivenThatWeWantToPublishAHelloWorldProject()
        {
            _testAssetsManager = TestAssetsManager.TestProjectsAssetsManager;
        }

        [Fact]
        public void It_publishes_portable_apps_to_the_publish_folder_and_the_app_should_run()
        {
            var helloWorldAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld")
                .WithSource()
                .Restore();

            var publishCommand = new PublishCommand(Stage0MSBuild, helloWorldAsset.TestRoot);
            var publishResult = publishCommand.Execute();

            publishResult.Should().Pass();

            var publishDirectory = publishCommand.GetOutputDirectory();

            publishDirectory.Should().OnlyHaveFiles(new[] {
                "HelloWorld.dll",
                "HelloWorld.pdb",
                "HelloWorld.deps.json",
                "HelloWorld.runtimeconfig.json"
            });

            Command.Create(RepoInfo.DotNetHostPath, new[] { Path.Combine(publishDirectory.FullName, "HelloWorld.dll") })
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void It_runs_portable_apps_from_a_different_path_after_building()
        {
            var helloWorldAsset = _testAssetsManager
                   .CopyTestAsset("HelloWorld")
                   .WithSource()
                   .Restore();

            var buildCommand = new BuildCommand(Stage0MSBuild, helloWorldAsset.TestRoot);

            buildCommand.Execute().Should().Pass();

            var runCommand = Command.Create(RepoInfo.DotNetHostPath, new[]
            {
                "run3",
                "--no-build",
                "--project",
                buildCommand.FullPathProjectFile
            });

            string workingDirectory = Directory.GetParent(helloWorldAsset.Path).FullName;

            runCommand.WorkingDirectory(workingDirectory)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }

        //  Blocked by https://github.com/dotnet/cli/issues/4447
        //[Fact]
        public void It_runs_portable_apps_from_a_different_path_without_building()
        {
            var helloWorldAsset = _testAssetsManager
                   .CopyTestAsset("HelloWorld")
                   .WithSource()
                   .Restore();

            var runCommand = Command.Create(RepoInfo.DotNetHostPath, new[]
            {
                "run3",
                "--project",
                Path.Combine(helloWorldAsset.Path, "HelloWorld.csproj")
            });

            string workingDirectory = Directory.GetParent(helloWorldAsset.Path).FullName;

            runCommand.WorkingDirectory(workingDirectory)
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }

        [Fact]
        public void It_publishes_self_contained_apps_to_the_publish_folder_and_the_app_should_run()
        {
            var rid = RuntimeEnvironment.GetRuntimeIdentifier();

            var helloWorldAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld", "SelfContained")
                .WithSource()
                .Restore(relativePath: "", args: $"/p:RuntimeIdentifiers={rid}");

            var publishCommand = new PublishCommand(Stage0MSBuild, helloWorldAsset.TestRoot);
            var publishResult = publishCommand.Execute($"/p:RuntimeIdentifier={rid}");

            publishResult.Should().Pass();

            var publishDirectory = publishCommand.GetOutputDirectory(selfContained: true);
            var selfContainedExecutable = $"HelloWorld{Constants.ExeSuffix}";

            var libPrefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib";

            publishDirectory.Should().HaveFiles(new[] {
                selfContainedExecutable,
                "HelloWorld.dll",
                "HelloWorld.pdb",
                "HelloWorld.deps.json",
                "HelloWorld.runtimeconfig.json",
                $"{libPrefix}coreclr{Constants.DynamicLibSuffix}",
                $"{libPrefix}hostfxr{Constants.DynamicLibSuffix}",
                $"{libPrefix}hostpolicy{Constants.DynamicLibSuffix}",
                $"mscorlib.dll",
                $"System.Private.CoreLib.dll",
            });

            Command.Create(Path.Combine(publishDirectory.FullName, selfContainedExecutable), new string[] { })
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World!");
        }
    }
}