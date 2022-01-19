// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.NET.TestFramework.ProjectConstruction;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToGenerateGlobalUsings_DotNet : SdkTest
    {
        public GivenThatWeWantToGenerateGlobalUsings_DotNet(ITestOutputHelper log) : base(log) { }

        [RequiresMSBuildVersionFact("17.0.0.32901")]
        public void It_can_generate_global_usings_and_builds_successfully()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.AdditionalProperties["ImplicitUsings"] = "enable";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g.cs";

            var buildCommand = new BuildCommand(testAsset);
            buildCommand
                .Execute()
                .Should()
                .Pass();

            var outputDirectory = buildCommand.GetIntermediateDirectory(tfm);

            outputDirectory.Should().HaveFile(globalUsingsFileName);

            File.ReadAllText(Path.Combine(outputDirectory.FullName, globalUsingsFileName)).Should().Be(
@"// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
");
        }

        [Fact]
        public void Implicit_Usings_Are_Not_Enabled_By_Default()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g.cs";

            var buildCommand = new BuildCommand(testAsset);
            buildCommand
                .Execute()
                .Should()
                .Fail();

            var outputDirectory = buildCommand.GetIntermediateDirectory(tfm);

            outputDirectory.Should().NotHaveFile(globalUsingsFileName);
        }

        [RequiresMSBuildVersionFact("17.0.0.32901")]
        public void It_can_remove_specific_usings_in_project_file()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.AdditionalProperties["ImplicitUsings"] = "enable";
            testProject.AddItem("Using", new Dictionary<string, string> { ["Remove"] = "System.IO" });
            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g.cs";


            var buildCommand = new BuildCommand(testAsset);
            buildCommand
                .Execute()
                .Should()
                .Pass();

            var outputDirectory = buildCommand.GetIntermediateDirectory(tfm);

            outputDirectory.Should().HaveFile(globalUsingsFileName);

            File.ReadAllText(Path.Combine(outputDirectory.FullName, globalUsingsFileName)).Should().Be(
@"// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
");
        }

        [Fact]
        public void It_can_generate_custom_usings()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.ProjectChanges.Add(projectXml =>
            {
                var ns = projectXml.Root.Name.Namespace;
                var itemGroup = new XElement(ns + "ItemGroup");
                projectXml.Root.Add(XElement.Parse(
@"<ItemGroup>
    <Using Include=""CustomNamespace"" />
    <Using Include=""TestStaticNamespace"" Static=""True"" />
    <Using Include=""System.Biology"" Alias=""AppliedChemistry"" />
</ItemGroup>"));
            });

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g.cs";

            var buildCommand = new BuildCommand(testAsset);
            buildCommand
                .Execute()
                .Should()
                .Fail();

            var outputDirectory = buildCommand.GetIntermediateDirectory(tfm);

            outputDirectory.Should().HaveFile(globalUsingsFileName);

            File.ReadAllText(Path.Combine(outputDirectory.FullName, globalUsingsFileName)).Should().Be(
@"// <auto-generated/>
global using global::CustomNamespace;
global using AppliedChemistry = global::System.Biology;
global using static global::TestStaticNamespace;
");
        }

        [Fact]
        public void It_considers_switches_when_deduping()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.ProjectChanges.Add(projectXml =>
            {
                var ns = projectXml.Root.Name.Namespace;
                var itemGroup = new XElement(ns + "ItemGroup");
                projectXml.Root.Add(XElement.Parse(
@"<ItemGroup>
    <Using Include=""CustomNamespace"" />
    <Using Include=""System.IO.File"" Alias=""FileIO"" />
    <Using Include=""TestStaticNamespace"" Static=""True"" />
    <Using Include=""TestStaticNamespace"" />
    <Using Include=""TestStaticNamespace"" Static=""True"" />
    <Using Include=""CustomNamespace"" />
    <Using Include=""System.IO.File"" Alias=""Disk"" />
    <Using Include=""System.IO.File"" Alias=""FileIO"" />
</ItemGroup>"));
            });

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g.cs";

            var buildCommand = new BuildCommand(testAsset);
            buildCommand
                .Execute()
                .Should()
                .Fail();

            var outputDirectory = buildCommand.GetIntermediateDirectory(tfm);

            outputDirectory.Should().HaveFile(globalUsingsFileName);

            File.ReadAllText(Path.Combine(outputDirectory.FullName, globalUsingsFileName)).Should().Be(
@"// <auto-generated/>
global using global::CustomNamespace;
global using global::TestStaticNamespace;
global using Disk = global::System.IO.File;
global using FileIO = global::System.IO.File;
global using static global::TestStaticNamespace;
");
        }

        [RequiresMSBuildVersionFact("17.0.0.32901")]
        public void It_can_persist_generatedfile_between_cleans()
        {
            // Regression test for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1405579
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.AdditionalProperties["ImplicitUsings"] = "enable";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g.cs";

            var buildCommand = new BuildCommand(testAsset);
            buildCommand
                .Execute()
                .Should()
                .Pass();

            var outputDirectory = buildCommand.GetIntermediateDirectory(tfm);

            outputDirectory.Should().HaveFile(globalUsingsFileName);

            var cleanCommand = new CleanCommand(testAsset);
            cleanCommand
                .Execute()
                .Should()
                .Pass();

            // Verify the GlobalUsings.g.cs does not get removed on clean.
            outputDirectory.Should().HaveFile(globalUsingsFileName);
            File.ReadAllText(Path.Combine(outputDirectory.FullName, globalUsingsFileName)).Should().Be(
@"// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
");
        }

        private TestProject CreateTestProject(string tfm)
        {
            var testProject = new TestProject
            {
                IsExe = true,
                TargetFrameworks = tfm,
                ProjectSdk = "Microsoft.NET.Sdk"
            };
            testProject.SourceFiles["Program.cs"] = @"
namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}
";
            return testProject;
        }
    }
}
