// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Xunit;
using FluentAssertions;
using System.Xml.Linq;
using System.Linq;
using System;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToBuildAnAppWithTransitiveProjectRefs : SdkTest
    {
        public GivenThatWeWantToBuildAnAppWithTransitiveProjectRefs(ITestOutputHelper log) : base(log)
        {
        }

        [Fact(Skip="only helix failed tests")]
        public void It_builds_the_project_successfully()
        {
            // NOTE the project dependencies in AppWithTransitiveProjectRefs:
            // TestApp --depends on--> MainLibrary --depends on--> AuxLibrary
            // (TestApp transitively depends on AuxLibrary)

            var testAsset = _testAssetsManager
                .CopyTestAsset("AppWithTransitiveProjectRefs", "BuildAppWithTransitiveProjectRef")
                .WithSource();

            VerifyAppBuilds(testAsset);
        }

        void VerifyAppBuilds(TestAsset testAsset)
        {
            var appProjectDirectory = Path.Combine(testAsset.TestRoot, "TestApp");

            var buildCommand = new BuildCommand(Log, appProjectDirectory);
            var outputDirectory = buildCommand.GetOutputDirectory("netcoreapp1.1");

            buildCommand
                .Execute()
                .Should()
                .Pass();

            outputDirectory.Should().OnlyHaveFiles(new[] {
                "TestApp.dll",
                "TestApp.pdb",
                "TestApp.deps.json",
                "TestApp.runtimeconfig.json",
                "TestApp.runtimeconfig.dev.json",
                "MainLibrary.dll",
                "MainLibrary.pdb",
                "AuxLibrary.dll",
                "AuxLibrary.pdb",
            });

            new DotnetCommand(Log, Path.Combine(outputDirectory.FullName, "TestApp.dll"))
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("This string came from MainLibrary!")
                .And
                .HaveStdOutContaining("This string came from AuxLibrary!");
        }

        [WindowsOnlyFact(Skip="only helix failed tests")]
        public void The_clean_target_removes_all_files_from_the_output_folder()
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("AppWithTransitiveProjectRefs")
                .WithSource();

            var appProjectDirectory = Path.Combine(testAsset.TestRoot, "TestApp");

            var buildCommand = new BuildCommand(Log, appProjectDirectory);

            buildCommand
                .Execute()
                .Should()
                .Pass();

            var outputDirectory = buildCommand.GetOutputDirectory("netcoreapp1.1");

            outputDirectory.Should().OnlyHaveFiles(new[] {
                "TestApp.dll",
                "TestApp.pdb",
                "TestApp.deps.json",
                "TestApp.runtimeconfig.dev.json",
                "TestApp.runtimeconfig.json",
                "MainLibrary.dll",
                "MainLibrary.pdb",
                "AuxLibrary.dll",
                "AuxLibrary.pdb"
            });

            var cleanCommand = new MSBuildCommand(Log, "Clean", buildCommand.FullPathProjectFile);

            cleanCommand
                .Execute()
                .Should()
                .Pass();

            outputDirectory.Should().OnlyHaveFiles(Array.Empty<string>());
        }

        [Fact(Skip="only helix failed tests")]
        public void It_does_not_build_the_project_successfully()
        {
            // NOTE the project dependencies in AppWithTransitiveProjectRefs:
            // TestApp --depends on--> MainLibrary --depends on--> AuxLibrary
            // (TestApp transitively depends on AuxLibrary)

            var testAsset = _testAssetsManager
                .CopyTestAsset("AppWithTransitiveProjectRefs", "BuildAppWithTransitiveProjectRefDisabled")
                .WithSource();

            var appProjectDirectory = Path.Combine(testAsset.TestRoot, "TestApp");
            var buildCommand = new BuildCommand(Log, appProjectDirectory);
            buildCommand
                .Execute("/p:DisableTransitiveProjectReferences=true")
                .Should()
                .Fail();
        }
    }
}
