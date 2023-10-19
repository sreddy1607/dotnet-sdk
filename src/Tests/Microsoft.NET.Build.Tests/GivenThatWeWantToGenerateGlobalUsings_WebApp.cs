// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToGenerateGlobalUsings_WebApp : SdkTest
    {

        public GivenThatWeWantToGenerateGlobalUsings_WebApp(ITestOutputHelper log) : base(log) { }

        [RequiresMSBuildVersionFact("17.8.1.47607")]
        public void It_generates_web_implicit_usings_and_builds_successfully()
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
global using global::Microsoft.AspNetCore.Builder;
global using global::Microsoft.AspNetCore.Hosting;
global using global::Microsoft.AspNetCore.Http;
global using global::Microsoft.AspNetCore.Routing;
global using global::Microsoft.Extensions.Configuration;
global using global::Microsoft.Extensions.DependencyInjection;
global using global::Microsoft.Extensions.Hosting;
global using global::Microsoft.Extensions.Logging;
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Net.Http.Json;
global using global::System.Threading;
global using global::System.Threading.Tasks;
");
        }

        [Fact]
        public void It_can_disable_web_usings()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.AdditionalProperties["ImplicitUsings"] = "disable";
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

        private TestProject CreateTestProject(string tfm)
        {
            var testProject = new TestProject
            {
                IsExe = true,
                TargetFrameworks = tfm,
                ProjectSdk = "Microsoft.NET.Sdk.Web"
            };
            testProject.SourceFiles["Program.cs"] = @"
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet(""/"", (Func<string>)(() => ""Hello World!""));

app.Run();
";
            return testProject;
        }
    }
}
