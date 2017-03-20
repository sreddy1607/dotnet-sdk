// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Xunit;
using FluentAssertions;
using static Microsoft.NET.TestFramework.Commands.MSBuildTest;
using System.Runtime.InteropServices;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToControlGeneratedAssemblyInfo : SdkTest
    {
        [Theory]
        [InlineData("AssemblyInformationVersionAttribute")]
        [InlineData("AssemblyFileVersionAttribute")]
        [InlineData("AssemblyVersionAttribute")]
        [InlineData("AssemblyCompanyAttribute")]
        [InlineData("AssemblyConfigurationAttribute")]
        [InlineData("AssemblyCopyrightAttribute")]
        [InlineData("AssemblyDescriptionAttribute")]
        [InlineData("AssemblyTitleAttribute")]
        [InlineData("NeutralResourcesLanguageAttribute")]
        [InlineData("All")]
        public void It_respects_opt_outs(string attributeToOptOut)
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld", identifier: Path.DirectorySeparatorChar + attributeToOptOut)
                .WithSource()
                .Restore();

            var buildCommand = new BuildCommand(Stage0MSBuild, testAsset.TestRoot);
            buildCommand
                .Execute(
                    "/p:Version=1.2.3-beta",
                    "/p:FileVersion=4.5.6.7",
                    "/p:AssemblyVersion=8.9.10.11",
                    "/p:Company=TestCompany",
                    "/p:Configuration=Release",
                    "/p:Copyright=TestCopyright",
                    "/p:Description=TestDescription",
                    "/p:Product=TestProduct",
                    "/p:AssemblyTitle=TestTitle",
                    "/p:NeutralLanguage=fr",
                    attributeToOptOut == "All" ?
                        "/p:GenerateAssemblyInfo=false" :
                        $"/p:Generate{attributeToOptOut}=false"
                    )
                .Should()
                .Pass();

            var expectedInfo = new SortedDictionary<string, string>
            {
                { "AssemblyInformationalVersionAttribute", "1.2.3-beta" },
                { "AssemblyFileVersionAttribute", "4.5.6.7" },
                { "AssemblyVersionAttribute", "8.9.10.11" },
                { "AssemblyCompanyAttribute", "TestCompany" },
                { "AssemblyConfigurationAttribute", "Release" },
                { "AssemblyCopyrightAttribute", "TestCopyright" },
                { "AssemblyDescriptionAttribute", "TestDescription" },
                { "AssemblyProductAttribute", "TestProduct" },
                { "AssemblyTitleAttribute", "TestTitle" },
                { "NeutralResourcesLanguageAttribute", "fr" },
            };

            if (attributeToOptOut == "All")
            {
                expectedInfo.Clear();
            }
            else
            {
                expectedInfo.Remove(attributeToOptOut);
            }

            expectedInfo.Add("TargetFrameworkAttribute", ".NETCoreApp,Version=v1.1");

            var assemblyPath = Path.Combine(buildCommand.GetOutputDirectory("netcoreapp1.1", "Release").FullName, "HelloWorld.dll");
            var actualInfo = AssemblyInfo.Get(assemblyPath);

            actualInfo.Should().Equal(expectedInfo);
        }

        [Theory]
        [InlineData("netcoreapp1.1")]
        [InlineData("net45")]
        public void It_respects_version_prefix(string targetFramework)
        {
            if (targetFramework == "net45" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var testAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld", identifier: targetFramework)
                .WithSource()
                .Restore("", $"/p:OutputType=Library;TargetFramework={targetFramework}");

            var buildCommand = new BuildCommand(Stage0MSBuild, testAsset.TestRoot);
            buildCommand
                .Execute($"/p:OutputType=Library;TargetFramework={targetFramework};VersionPrefix=1.2.3")
                .Should()
                .Pass();

            var assemblyPath = Path.Combine(buildCommand.GetOutputDirectory(targetFramework).FullName, "HelloWorld.dll");
            var info = AssemblyInfo.Get(assemblyPath);

            info["AssemblyVersionAttribute"].Should().Be("1.2.3.0");
            info["AssemblyFileVersionAttribute"].Should().Be("1.2.3.0");
            info["AssemblyInformationalVersionAttribute"].Should().Be("1.2.3");
        }

        [Theory]
        [InlineData("netcoreapp1.1")]
        [InlineData("net45")]
        public void It_respects_version_changes_on_incremental_build(string targetFramework)
        {
            if (targetFramework == "net45" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            // Given a project that has already been built
            var testAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld", identifier: targetFramework)
                .WithSource()
                .Restore("", $"/p:OutputType=Library;TargetFramework={targetFramework}");
            BuildProject(versionPrefix: "1.2.3");
            var fullBuildAssemblyInfo = FindAssemblyInfo();

            // When the same project is built again using a different VersionPrefix proeprty
            var incrementalBuildCommand = BuildProject(versionPrefix: "1.2.4");
            var incrementalBuildAssemblyInfo = FindAssemblyInfo();

            // Then the version of the built assembly shall match the provided VersionPrefix
            var assemblyPath = Path.Combine(incrementalBuildCommand.GetOutputDirectory(targetFramework).FullName, "HelloWorld.dll");
            var info = AssemblyInfo.Get(assemblyPath);
            info["AssemblyVersionAttribute"].Should().Be("1.2.4.0");

            // And the assembly info filename must have changed
            incrementalBuildAssemblyInfo.Should().NotBe(fullBuildAssemblyInfo);

            // And the previous assembly info must have been deleted (by IncrementalClean)
            File.Exists(fullBuildAssemblyInfo).Should().BeFalse();

            BuildCommand BuildProject(string versionPrefix)
            {
                var command = new BuildCommand(Stage0MSBuild, testAsset.TestRoot);
                command.Execute($"/p:OutputType=Library;TargetFramework={targetFramework};VersionPrefix={versionPrefix}")
                       .Should()
                       .Pass();
                return command;
            }

            string FindAssemblyInfo()
            {
                var expectedAssemblyInfoLocation = Path.Combine(testAsset.TestRoot, "obj", "Debug", targetFramework);
                var matches = Directory.EnumerateFiles(expectedAssemblyInfoLocation, "*AssemblyInfo*.cs", SearchOption.TopDirectoryOnly).ToList();
                matches.Count.Should().Be(1, "only a single assembly info file should exist");
                return matches[0];
            }
        }
    }
}
