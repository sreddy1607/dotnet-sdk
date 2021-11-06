// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using ManifestReaderTests;
using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.Workloads.Workload;
using Microsoft.DotNet.Workloads.Workload.Install;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Utilities;
using Xunit;
using Xunit.Abstractions;
using Microsoft.DotNet.Workloads.Workload.Install.InstallRecord;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.DotNet.Cli.Utils;
using System.Text.Json;

namespace Microsoft.DotNet.Cli.Workload.Install.Tests
{
    public class GivenDotnetWorkloadInstall : SdkTest
    {
        private readonly BufferedReporter _reporter;
        private readonly string _manifestPath;

        public GivenDotnetWorkloadInstall(ITestOutputHelper log) : base(log)
        {
            _reporter = new BufferedReporter();
            _manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "Sample.json");
        }

        [Fact]
        public void GivenWorkloadInstallItErrorsOnFakeWorkloadName()
        {
            var command = new DotnetCommand(Log);
            command
                .WithEnvironmentVariable("DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR", string.Empty)
                .WithEnvironmentVariable("PATH", "fake")
                .Execute("workload", "install", "fake", "--skip-manifest-update")
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(Workloads.Workload.Install.LocalizableStrings.WorkloadNotRecognized, "fake"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItCanInstallPacks(bool userLocal)
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "--skip-manifest-update" });
            (_, var installManager, var installer, _, _, _) = GetTestInstallers(parseResult, userLocal);

            installManager.InstallWorkloads(mockWorkloadIds, true);

            installer.GarbageCollectionCalled.Should().BeTrue();
            installer.CachePath.Should().BeNull();
            installer.InstallationRecordRepository.WorkloadInstallRecord.Should().BeEquivalentTo(mockWorkloadIds);
            installer.InstalledPacks.Count.Should().Be(8);
            installer.InstalledPacks.Where(pack => pack.Id.ToString().Contains("Android")).Count().Should().Be(8);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItCanRollBackPackInstallation(bool userLocal)
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android"), new WorkloadId("xamarin-android-build") };
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "xamarin-android-build", "--skip-manifest-update" });
            (_, var installManager, var installer, var workloadResolver, _, _) = GetTestInstallers(parseResult, userLocal, failingWorkload: "xamarin-android-build");

            var exceptionThrown = Assert.Throws<Exception>(() => installManager.InstallWorkloads(mockWorkloadIds, true));
            exceptionThrown.Message.Should().Be("Failing workload: xamarin-android-build");
            var expectedPacks = mockWorkloadIds
                .SelectMany(workloadId => workloadResolver.GetPacksInWorkload(workloadId))
                .Distinct()
                .Select(packId => workloadResolver.TryGetPackInfo(packId))
                .Where(pack => pack != null);
            installer.RolledBackPacks.ShouldBeEquivalentTo(expectedPacks);
            installer.InstallationRecordRepository.WorkloadInstallRecord.Should().BeEmpty();
        }

        [Fact]
        public void GivenWorkloadInstallOnFailingRollbackItDisplaysTopLevelError()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android"), new WorkloadId("xamarin-android-build") };
            var testDirectory = _testAssetsManager.CreateTestDirectory().Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var installer = new MockPackWorkloadInstaller(failingWorkload: "xamarin-android-build", failingRollback: true);
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { _manifestPath }), dotnetRoot);
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "xamarin-android-build", "--skip-manifest-update" });
            var installManager = new WorkloadInstallCommand(parseResult, reporter: _reporter, workloadResolver: workloadResolver, workloadInstaller: installer, version: "6.0.100");

            var exceptionThrown = Assert.Throws<Exception>(() => installManager.InstallWorkloads(mockWorkloadIds, true));
            exceptionThrown.Message.Should().Be("Failing workload: xamarin-android-build");
            string.Join(" ", _reporter.Lines).Should().Contain("Rollback failure");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItCanUpdateAdvertisingManifests(bool userLocal)
        {
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android" });
            (_, var installManager, var installer, _, var manifestUpdater, _) = GetTestInstallers(parseResult, userLocal);

            installManager.InstallWorkloads(new List<WorkloadId>(), false); // Don't actually do any installs, just update manifests

            installer.InstalledManifests.Should().BeEmpty(); // Didn't try to alter any installed manifests
            manifestUpdater.CalculateManifestUpdatesCallCount.Should().Be(1);
            manifestUpdater.UpdateAdvertisingManifestsCallCount.Should().Be(1);
        }

        [Fact]
        public void GivenWorkloadInstallItWarnsOnGarbageCollectionFailure()
        {
            _reporter.Clear();
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android"), new WorkloadId("xamarin-android-build") };
            var testDirectory = _testAssetsManager.CreateTestDirectory().Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var installer = new MockPackWorkloadInstaller(failingGarbageCollection: true);
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { _manifestPath }), dotnetRoot);
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "xamarin-android-build", "--skip-manifest-update" });
            var installManager = new WorkloadInstallCommand(parseResult, reporter: _reporter, workloadResolver: workloadResolver, workloadInstaller: installer, version: "6.0.100");

            installManager.InstallWorkloads(mockWorkloadIds, true);
            string.Join(" ", _reporter.Lines).Should().Contain("Failing garbage collection");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItCanUpdateInstalledManifests(bool userLocal)
        {
            var parseResult =
                Parser.Instance.Parse(new string[] {"dotnet", "workload", "install", "xamarin-android"});
            var manifestsToUpdate =
                new (ManifestId, ManifestVersion, ManifestVersion, Dictionary<WorkloadId, WorkloadDefinition>
                    Workloads)[]
                    {
                        (new ManifestId("mock-manifest"), new ManifestVersion("1.0.0"), new ManifestVersion("2.0.0"),
                            null),
                    };
            (_, var installManager, var installer, _, _, _) =
                GetTestInstallers(parseResult, userLocal, manifestUpdates: manifestsToUpdate);

            installManager.InstallWorkloads(new List<WorkloadId>(), false); // Don't actually do any installs, just update manifests

            installer.InstalledManifests[0].manifestId.Should().Be(manifestsToUpdate[0].Item1);
            installer.InstalledManifests[0].manifestVersion.Should().Be(manifestsToUpdate[0].Item3);
            installer.InstalledManifests[0].sdkFeatureBand.Should().Be(new SdkFeatureBand("6.0.100"));
            installer.InstalledManifests[0].offlineCache.Should().Be(null);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallFromCacheItInstallsCachedManifest(bool userLocal)
        {
            var manifestsToUpdate =
                new (ManifestId, ManifestVersion, ManifestVersion, Dictionary<WorkloadId, WorkloadDefinition>
                    Workloads)[]
                    {
                        (new ManifestId("mock-manifest"), new ManifestVersion("1.0.0"), new ManifestVersion("2.0.0"),
                            null)
                    };
            var cachePath = Path.Combine(_testAssetsManager.CreateTestDirectory(identifier: AppendForUserLocal("mockCache_", userLocal)).Path,
                "mockCachePath");
            var parseResult = Parser.Instance.Parse(new string[]
            {
                "dotnet", "workload", "install", "xamarin-android", "--from-cache", cachePath
            });
            (_, var installManager, var installer, _, _, _) = GetTestInstallers(parseResult, userLocal,
                tempDirManifestPath: _manifestPath, manifestUpdates: manifestsToUpdate);

            installManager.Execute();

            installer.InstalledManifests[0].manifestId.Should().Be(manifestsToUpdate[0].Item1);
            installer.InstalledManifests[0].manifestVersion.Should().Be(manifestsToUpdate[0].Item3);
            installer.InstalledManifests[0].sdkFeatureBand.Should().Be(new SdkFeatureBand("6.0.100"));
            installer.InstalledManifests[0].offlineCache.Should().Be(new DirectoryPath(cachePath));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItCanDownloadToOfflineCache(bool userLocal)
        {
            var cachePath = Path.Combine(_testAssetsManager.CreateTestDirectory(identifier: AppendForUserLocal("mockCache_", userLocal)).Path, "mockCachePath");
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "--download-to-cache", cachePath });
            (_, var installManager, var installer, _, var manifestUpdater, _) = GetTestInstallers(parseResult, userLocal, tempDirManifestPath: _manifestPath);

            installManager.Execute();

            // Manifest packages should have been 'downloaded' and used for pack resolution
            manifestUpdater.DownloadManifestPackagesCallCount.Should().Be(1);
            manifestUpdater.ExtractManifestPackagesToTempDirCallCount.Should().Be(1);
            // 8 android pack packages
            installer.CachedPacks.Count.Should().Be(8);
            installer.CachePath.Should().Be(cachePath);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItCanInstallFromOfflineCache(bool userLocal)
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            var cachePath = "mockCachePath";
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "--from-cache", cachePath });
            (_, var installManager, var installer, _, _, var nugetDownloader) = GetTestInstallers(parseResult, userLocal);

            installManager.Execute();

            installer.GarbageCollectionCalled.Should().BeTrue();
            installer.CachePath.Should().Contain(cachePath);
            installer.InstallationRecordRepository.WorkloadInstallRecord.Should().BeEquivalentTo(mockWorkloadIds);
            installer.InstalledPacks.Count.Should().Be(8);
            installer.InstalledPacks.Where(pack => pack.Id.ToString().Contains("Android")).Count().Should().Be(8);
            nugetDownloader.DownloadCallParams.Count().Should().Be(0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
		public void GivenWorkloadInstallItPrintsDownloadUrls(bool userLocal)
        {
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", "xamarin-android", "--print-download-link-only" });
            (_, var installManager, _, _, _, _) = GetTestInstallers(parseResult, userLocal, tempDirManifestPath: _manifestPath);

            installManager.Execute();

            _reporter.Lines.Should().Contain("==allPackageLinksJsonOutputStart==");
            string.Join(" ", _reporter.Lines).Should().Contain("mock-url-xamarin.android.sdk");
            string.Join(" ", _reporter.Lines).Should().Contain("mock-manifest-url");
        }

        [Fact]
        public void GivenWorkloadInstallItErrorsOnUnsupportedPlatform()
        {
            var mockWorkloadId = "unsupported";
            var manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "UnsupportedPlatform.json");
            var testDirectory = _testAssetsManager.CreateTestDirectory().Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var installer = new MockPackWorkloadInstaller();
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { manifestPath }), dotnetRoot);
            var nugetDownloader = new MockNuGetPackageDownloader(dotnetRoot);
            var manifestUpdater = new MockWorkloadManifestUpdater();
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", mockWorkloadId });

            var exceptionThrown = Assert.Throws<GracefulException>(() => new WorkloadInstallCommand(parseResult, reporter: _reporter, workloadResolver: workloadResolver, workloadInstaller: installer,
                nugetPackageDownloader: nugetDownloader, workloadManifestUpdater: manifestUpdater, userProfileDir: testDirectory, dotnetDir: dotnetRoot, version: "6.0.100"));
            exceptionThrown.Message.Should().Be(String.Format(Workloads.Workload.Install.LocalizableStrings.WorkloadNotSupportedOnPlatform, mockWorkloadId));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadInstallItDoesNotRemoveOldInstallsOnRollback(bool userLocal)
        {
            var testDirectory = _testAssetsManager.CreateTestDirectory(identifier: userLocal ? "userlocal" : "default").Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            var tmpDir = Path.Combine(testDirectory, "tmp");
            var manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "MockWorkloadsSample.json");
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { manifestPath }), dotnetRoot, userLocal, userProfileDir);
            var nugetDownloader = new FailingNuGetPackageDownloader(tmpDir);
            var manifestUpdater = new MockWorkloadManifestUpdater();
            var sdkFeatureVersion = "6.0.100";
            var existingWorkload = "mock-1";
            var installingWorkload = "mock-2";

            if (userLocal)
            {
                WorkloadFileBasedInstall.SetUserLocal(dotnetRoot, sdkFeatureVersion);
            }

            // Successfully install a workload
            var installParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", existingWorkload });
            var installCommand = new WorkloadInstallCommand(installParseResult, reporter: _reporter, workloadResolver: workloadResolver, nugetPackageDownloader: new MockNuGetPackageDownloader(tmpDir),
                workloadManifestUpdater: manifestUpdater, userProfileDir: userProfileDir, version: sdkFeatureVersion, dotnetDir: dotnetRoot, tempDirPath: testDirectory);
            installCommand.Execute();

            // Install a workload with a mocked nuget failure
            installParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", installingWorkload });
            installCommand = new WorkloadInstallCommand(installParseResult, reporter: _reporter, workloadResolver: workloadResolver, nugetPackageDownloader: nugetDownloader,
                workloadManifestUpdater: manifestUpdater, userProfileDir: userProfileDir, version: sdkFeatureVersion, dotnetDir: dotnetRoot, tempDirPath: testDirectory);
            var exceptionThrown = Assert.Throws<GracefulException>(() => installCommand.Execute());
            exceptionThrown.Message.Should().Contain("Test Failure");

            // Existing installation is still present
            string installRoot = userLocal ? userProfileDir : dotnetRoot;
            var installRecordPath = Path.Combine(installRoot, "metadata", "workloads", sdkFeatureVersion, "InstalledWorkloads");
            Directory.GetFiles(installRecordPath).Count().Should().Be(1);
            File.Exists(Path.Combine(installRecordPath, existingWorkload))
                .Should().BeTrue();
            var packRecordDirs = Directory.GetDirectories(Path.Combine(installRoot, "metadata", "workloads", "InstalledPacks", "v1"));
            packRecordDirs.Count().Should().Be(3);
            var installPacks = Directory.GetDirectories(Path.Combine(installRoot, "packs"));
            installPacks.Count().Should().Be(3);
        }

        private (string, WorkloadInstallCommand, MockPackWorkloadInstaller, IWorkloadResolver, MockWorkloadManifestUpdater, MockNuGetPackageDownloader) GetTestInstallers(
                ParseResult parseResult,
                bool userLocal,
                [CallerMemberName] string testName = "",
                string failingWorkload = null,
                IEnumerable<(ManifestId, ManifestVersion, ManifestVersion, Dictionary<WorkloadId, WorkloadDefinition> Workloads)> manifestUpdates = null,
                string tempDirManifestPath = null)
        {
            _reporter.Clear();
            var testDirectory = _testAssetsManager.CreateTestDirectory(testName: testName, identifier: userLocal ? "userlocal" : "default").Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            var installer = new MockPackWorkloadInstaller(failingWorkload);
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { _manifestPath }), dotnetRoot);
            var nugetDownloader = new MockNuGetPackageDownloader(dotnetRoot);
            var manifestUpdater = new MockWorkloadManifestUpdater(manifestUpdates, tempDirManifestPath);
            string sdkVersion = "6.0.100";
            if (userLocal)
            {
                WorkloadFileBasedInstall.SetUserLocal(dotnetRoot, sdkVersion);
            }
            var installManager = new WorkloadInstallCommand(
                parseResult,
                reporter: _reporter,
                workloadResolver: workloadResolver,
                workloadInstaller: installer,
                nugetPackageDownloader: nugetDownloader,
                workloadManifestUpdater: manifestUpdater,
                userProfileDir: userProfileDir,
                dotnetDir: dotnetRoot,
                version: "6.0.100");

            return (testDirectory, installManager, installer, workloadResolver, manifestUpdater, nugetDownloader);
        }

        [Fact]
        public void GivenWorkloadInstallItErrorsOnInvalidWorkloadRollbackFile()
        {
            _reporter.Clear();
            var testDirectory = _testAssetsManager.CreateTestDirectory().Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            var tmpDir = Path.Combine(testDirectory, "tmp");
            var manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "MockWorkloadsSample.json");
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { manifestPath }), dotnetRoot);
            var sdkFeatureVersion = "6.0.100";
            var workload = "mock-1";
            var mockRollbackFileContent = @"{""fake.manifest.name"":""1.0.0""}";
            var rollbackFilePath = Path.Combine(testDirectory, "rollback.json");
            File.WriteAllText(rollbackFilePath, mockRollbackFileContent);

            var installParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", workload, "--from-rollback-file", rollbackFilePath });
            var installCommand = new WorkloadInstallCommand(installParseResult, reporter: _reporter, workloadResolver: workloadResolver, nugetPackageDownloader: new MockNuGetPackageDownloader(tmpDir),
                userProfileDir: userProfileDir, version: sdkFeatureVersion, dotnetDir: dotnetRoot, tempDirPath: testDirectory);
            
            Assert.Throws<GracefulException>(() => installCommand.Execute());
            string.Join(" ", _reporter.Lines).Should().Contain("Invalid rollback definition");
        }

        private string AppendForUserLocal(string identifier, bool userLocal)
        {
            if (!userLocal)
            {
                return identifier;
            }

            return $"{identifier}_userlocal";
        }
    }
}
