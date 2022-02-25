// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.NET.TestFramework.ProjectConstruction;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.NET.Publish.Tests.PublishTestUtils;

namespace Microsoft.NET.Publish.Tests
{
    public class GivenThatWeWantToPublishAnAotApp : SdkTest
    {
        private readonly string RuntimeIdentifier = $"/p:RuntimeIdentifier={RuntimeInformation.RuntimeIdentifier}";

        public GivenThatWeWantToPublishAnAotApp(ITestOutputHelper log) : base(log)
        {
        }

        [RequiresMSBuildVersionTheory("17.0.0.32901")]
        [InlineData(ToolsetInfo.CurrentTargetFramework)]
        public void ILLink_analyzer_warnings_are_produced(string targetFramework)
        {
            var projectName = "ILLinkAnalyzerWarningsApp";
            var testProject = CreateTestProjectWithAnalyzerWarnings(targetFramework, projectName, true);
            testProject.AdditionalProperties["EnableAotAnalyzer"] = "true";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);

            var publishCommand = new PublishCommand(Log, Path.Combine(testAsset.TestRoot, testProject.Name));
            publishCommand
                .Execute(RuntimeIdentifier)
                .Should().Pass()
                .And.HaveStdOutContaining("(8,9): warning IL3050")
                .And.HaveStdOutContaining("(18,12): warning IL3052");
        }

        [RequiresMSBuildVersionTheory("17.0.0.32901")]
        [InlineData(ToolsetInfo.CurrentTargetFramework)]
        public void ILLink_linker_analyzer_warnings_are_not_produced(string targetFramework)
        {
            var projectName = "ILLinkAnalyzerWarningsApp";
            var testProject = CreateTestProjectWithAnalyzerWarnings(targetFramework, projectName, true);
            // Inactive linker settings should have no effect on the aot analyzer,
            // unless PublishTrimmed is also set.
            testProject.AdditionalProperties["EnableAotAnalyzer"] = "true";
            testProject.AdditionalProperties["SuppressTrimAnalysisWarnings"] = "false";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);

            var publishCommand = new PublishCommand(Log, Path.Combine(testAsset.TestRoot, testProject.Name));
            publishCommand
                .Execute(RuntimeIdentifier)
                .Should().Pass()
                .And.NotHaveStdOutContaining("IL2026");
        }

        [RequiresMSBuildVersionTheory("17.0.0.32901")]
        [InlineData(ToolsetInfo.CurrentTargetFramework)]
        public void ILLink_analyzer_can_treat_warnings_as_errors(string targetFramework)
        {
            var projectName = "ILLinkAnalyzerWarningsApp";
            var testProject = CreateTestProjectWithAnalyzerWarnings(targetFramework, projectName, true);
            testProject.AdditionalProperties["EnableAotAnalyzer"] = "true";
            testProject.AdditionalProperties["WarningsAsErrors"] = "IL3050";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);

            var publishCommand = new PublishCommand(Log, Path.Combine(testAsset.TestRoot, testProject.Name));
            publishCommand
                .Execute(RuntimeIdentifier)
                .Should().Fail()
                .And.HaveStdOutContaining("(8,9): error IL3050")
                .And.HaveStdOutContaining("(18,12): warning IL3052");
        }

        [RequiresMSBuildVersionTheory("17.0.0.32901")]
        [InlineData(ToolsetInfo.CurrentTargetFramework)]
        public void ILLink_analyzer_can_ignore_warnings(string targetFramework)
        {
            var projectName = "ILLinkAnalyzerWarningsApp";
            var testProject = CreateTestProjectWithAnalyzerWarnings(targetFramework, projectName, true);
            testProject.AdditionalProperties["EnableAotAnalyzer"] = "true";
            testProject.AdditionalProperties["WarningsAsErrors"] = "IL3050";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);

            var publishCommand = new PublishCommand(Log, Path.Combine(testAsset.TestRoot, testProject.Name));
            publishCommand
                .Execute(RuntimeIdentifier, "/p:NoWarn=IL3050", "/p:WarnAsError=IL3050")
                .Should().Pass()
                .And.HaveStdOutContaining("(18,12): warning IL3052");
        }

        private TestProject CreateTestProjectWithAnalyzerWarnings(string targetFramework, string projectName, bool isExecutable)
        {
            var testProject = new TestProject()
            {
                Name = projectName,
                TargetFrameworks = targetFramework,
                IsExe = isExecutable
            };

            testProject.SourceFiles[$"{projectName}.cs"] = @"
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
class C
{
    static void Main()
    {
        ProduceAotAnalysisWarning();
        ProduceLinkerAnalysisWarning();
    }

    [RequiresDynamicCode(""Aot analysis warning"")]
    static void ProduceAotAnalysisWarning()
    {
    }

    [RequiresDynamicCode(""Aot analysis warning"")]
    static C()
    {
    }

    [RequiresUnreferencedCode(""Linker analysis warning"")]
    static void ProduceLinkerAnalysisWarning()
    {
    }
}";

            return testProject;
        }
    }
}
