// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToGenerateGlobalUsings_Worker : SdkTest
    {
        public GivenThatWeWantToGenerateGlobalUsings_Worker(ITestOutputHelper log) : base(log) { }

        [RequiresMSBuildVersionFact("17.0.0.32901")]
        public void It_generates_worker_implicit_usings_and_builds_successfully()
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
global using global::Microsoft.Extensions.Configuration;
global using global::Microsoft.Extensions.DependencyInjection;
global using global::Microsoft.Extensions.Hosting;
global using global::Microsoft.Extensions.Logging;
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
        public void It_can_disable_worker_usings()
        {
            var tfm = ToolsetInfo.CurrentTargetFramework;
            var testProject = CreateTestProject(tfm);
            testProject.AdditionalProperties["ImplicitUsings"] = "disable";
            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var globalUsingsFileName = $"{testAsset.TestProject.Name}.GlobalUsings.g..cs";

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
                ProjectSdk = "Microsoft.NET.Sdk.Worker"
            };
            testProject.PackageReferences.Add(new TestPackageReference("Microsoft.Extensions.Hosting", "6.0.0-preview.5.21301.5"));

            testProject.SourceFiles["Program.cs"] = @"
namespace WorkerApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
    }
}
";
            testProject.SourceFiles["Worker.cs"] = @"
namespace WorkerApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(""Worker running at: {time}"", DateTimeOffset.Now);
                try
                {
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
";
            return testProject;
        }
    }
}
