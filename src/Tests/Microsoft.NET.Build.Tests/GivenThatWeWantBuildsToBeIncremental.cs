// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantBuildsToBeIncremental : SdkTest
    {
        public GivenThatWeWantBuildsToBeIncremental(ITestOutputHelper log) : base(log)
        {
        }

        [Fact(Skip="todebug")]
        public void GenerateBuildRuntimeConfigurationFiles_runs_incrementally()
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld")
                .WithSource()
                .Restore(Log);

            var buildCommand = new BuildCommand(Log, testAsset.TestRoot);
            var outputDirectory = buildCommand.GetOutputDirectory("netcoreapp1.1").FullName;
            var runtimeConfigDevJsonPath = Path.Combine(outputDirectory, "HelloWorld.runtimeconfig.dev.json");

            buildCommand.Execute().Should().Pass();
            DateTime runtimeConfigDevJsonFirstModifiedTime = File.GetLastWriteTimeUtc(runtimeConfigDevJsonPath);

            buildCommand.Execute().Should().Pass();
            DateTime runtimeConfigDevJsonSecondModifiedTime = File.GetLastWriteTimeUtc(runtimeConfigDevJsonPath);

            runtimeConfigDevJsonSecondModifiedTime.Should().Be(runtimeConfigDevJsonFirstModifiedTime);
        }

        [Fact(Skip="todebug")]
        public void ResolvePackageAssets_runs_incrementally()
        { 
            var testAsset = _testAssetsManager
                .CopyTestAsset("HelloWorld")
                .WithSource()
                .Restore(Log);

            var targetFramework = "netcoreapp1.1";
            var buildCommand = new BuildCommand(Log, testAsset.TestRoot);
            var outputDirectory = buildCommand.GetOutputDirectory(targetFramework).FullName;
            var baseIntermediateOutputDirectory = buildCommand.GetBaseIntermediateDirectory().FullName;
            var intermediateDirectory = buildCommand.GetIntermediateDirectory(targetFramework).FullName;

            var assetsJsonPath = Path.Combine(baseIntermediateOutputDirectory, "project.assets.json");
            var assetsCachePath = Path.Combine(intermediateDirectory, "HelloWorld.assets.cache");

            // initial build
            buildCommand.Execute().Should().Pass();
            var cacheWriteTime1 = File.GetLastWriteTimeUtc(assetsCachePath);

            // build with no change to project.assets.json
            WaitForUtcNowToAdvance();
            buildCommand.Execute().Should().Pass();
            var cacheWriteTime2 = File.GetLastWriteTimeUtc(assetsCachePath);
            cacheWriteTime2.Should().Be(cacheWriteTime1);

            // build with modified project
            WaitForUtcNowToAdvance();
            File.SetLastWriteTimeUtc(assetsJsonPath, DateTime.UtcNow);
            buildCommand.Execute().Should().Pass();
            var cacheWriteTime3 = File.GetLastWriteTimeUtc(assetsCachePath);
            cacheWriteTime3.Should().NotBe(cacheWriteTime2);

            // build with modified settings
            WaitForUtcNowToAdvance();
            buildCommand.Execute("/p:DisableLockFileFrameworks=true").Should().Pass();
            var cacheWriteTime4 = File.GetLastWriteTimeUtc(assetsCachePath);
            cacheWriteTime4.Should().NotBe(cacheWriteTime3);
        }
    }
}
