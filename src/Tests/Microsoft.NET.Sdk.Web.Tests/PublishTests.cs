﻿using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.NET.TestFramework.ProjectConstruction;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Sdk.Web.Tests
{
    public class PublishTests : SdkTest
    {
        public PublishTests(ITestOutputHelper log) : base(log)
        {
        }

        [RequiresMSBuildVersionTheory("17.0.0.32901")]
        [MemberData(nameof(SupportedTfms))]
        public void TrimmingOptions_are_defaulted_correctly_on_trimmed_apps(string targetFramework)
        {
            var projectName = "HelloWorld";
            var rid = EnvironmentInfo.GetCompatibleRid(targetFramework);

            var testProject = CreateTestProjectForILLinkTesting(targetFramework, projectName);
            testProject.AdditionalProperties["PublishTrimmed"] = "true";

            var testAsset = _testAssetsManager.CreateTestProject(testProject, identifier: projectName + targetFramework);

            var publishCommand = new PublishCommand(Log, Path.Combine(testAsset.TestRoot, testProject.Name));
            publishCommand.Execute($"/p:RuntimeIdentifier={rid}").Should().Pass();

            string outputDirectory = publishCommand.GetOutputDirectory(targetFramework: targetFramework, runtimeIdentifier: rid).FullName;
            string runtimeConfigFile = Path.Combine(outputDirectory, $"{projectName}.runtimeconfig.json");
            string runtimeConfigContents = File.ReadAllText(runtimeConfigFile);

            JsonNode runtimeConfig = JsonNode.Parse(runtimeConfigContents);
            JsonNode configProperties = runtimeConfig["runtimeOptions"]["configProperties"];

            configProperties["Microsoft.AspNetCore.EnsureJsonTrimmability"].GetValue<bool>()
                    .Should().BeTrue();
        }

        [RequiresMSBuildVersionTheory("17.0.0.32901")]
        [MemberData(nameof(SupportedTfms))]
        public void TrimmingOptions_are_defaulted_correctly_on_aot_apps(string targetFramework)
        {
            var projectName = "HelloWorld";
            var rid = EnvironmentInfo.GetCompatibleRid(targetFramework);

            var testProject = CreateTestProjectForILLinkTesting(targetFramework, projectName);
            testProject.AdditionalProperties["PublishAOT"] = "true";

            var testAsset = _testAssetsManager.CreateTestProject(testProject, identifier: projectName + targetFramework);
            var publishCommand = new PublishCommand(Log, Path.Combine(testAsset.TestRoot, testProject.Name));
            publishCommand.Execute($"/p:RuntimeIdentifier={rid}").Should().Pass();

            string outputDirectory = publishCommand.GetIntermediateDirectory(targetFramework: targetFramework, runtimeIdentifier: rid).FullName;
            string responseFile = Path.Combine(outputDirectory, "native", $"{projectName}.ilc.rsp");
            var responseFileContents = File.ReadLines(responseFile);

            responseFileContents.Should().Contain("--feature:Microsoft.AspNetCore.EnsureJsonTrimmability=true");
        }

        public static IEnumerable<object[]> SupportedTfms { get; } = new List<object[]>
        {
#if NET8_0
            new object[] { ToolsetInfo.CurrentTargetFramework },
            new object[] { ToolsetInfo.NextTargetFramework }
#else
#error If building for a newer TFM, please update the values above
#endif
        };

        private TestProject CreateTestProjectForILLinkTesting(
            string targetFramework,
            string projectName)
        {
            var testProject = new TestProject()
            {
                Name = projectName,
                TargetFrameworks = targetFramework,
                IsExe = true,
                ProjectSdk = "Microsoft.NET.Sdk.Web"
            };

            testProject.SourceFiles[$"Program.cs"] = """
                using Microsoft.AspNetCore.Builder;
                using Microsoft.Extensions.Hosting;

                var builder = WebApplication.CreateBuilder();
                var app = builder.Build();
                app.Start();
                """;

            return testProject;
        }
    }
}
