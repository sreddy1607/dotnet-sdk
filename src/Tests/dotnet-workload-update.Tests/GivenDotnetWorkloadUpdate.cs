// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Runtime.CompilerServices;
using ManifestReaderTests;
using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.Workloads.Workload;
using Microsoft.DotNet.Workloads.Workload.Install;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using Microsoft.DotNet.Cli.Workload.Install.Tests;
using Microsoft.DotNet.Workloads.Workload.Update;
using Microsoft.DotNet.Cli.Utils;
using static Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadResolver;
using System.Text.Json;
using Microsoft.DotNet.Cli.Workload.Search.Tests;
using Microsoft.DotNet.Workloads.Workload.History;

namespace Microsoft.DotNet.Cli.Workload.Update.Tests
{
    public class GivenDotnetWorkloadUpdate : SdkTest
    {
        private readonly BufferedReporter _reporter;
        private readonly string _manifestPath;
        private readonly ParseResult _parseResult;

        public GivenDotnetWorkloadUpdate(ITestOutputHelper log) : base(log)
        {
            _reporter = new BufferedReporter();
            _manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "Sample.json");
            _parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update" });
        }

        [Fact]
        public void GivenWorkloadUpdateFromHistory()
        {
            string workloadHistoryRecord = @"{
              ""TimeStarted"": ""2023-11-13T13:25:49.8011987-08:00"",
              ""TimeCompleted"": ""2023-11-13T13:25:52.8522942-08:00"",
              ""CommandName"": ""update"",
              ""WorkloadArguments"": [],
              ""RollbackFileContents"": null,
              ""CommandLineArgs"": [],
              ""Succeeded"": true,
              ""ErrorMessage"": null,
              ""StateBeforeCommand"": {
                ""ManifestVersions"": {
                  ""microsoft.net.sdk.android"": ""34.0.0-rc.1.432/8.0.100-rc.1"",
                  ""microsoft.net.sdk.aspire"": ""8.0.0-alpha.23471.13/8.0.100-rc.1""
                },
                ""InstalledWorkloads"": []
              },
              ""StateAfterCommand"": {
                ""ManifestVersions"": {
                  ""microsoft.net.sdk.android"": ""34.0.0-rc.1.432/8.0.100-rc.1"",
                  ""microsoft.net.sdk.aspire"": ""8.0.0-alpha.23471.13/8.0.100-rc.1""
                },
                ""InstalledWorkloads"": [""maui-android"", ""aspire""]
              }
            }";

            IEnumerable<WorkloadManifestInfo> installedManifests = new List<WorkloadManifestInfo>() {
                                                new WorkloadManifestInfo("microsoft.net.sdk.android", "34.0.0-rc.1", "androidDirectory", "8.0.100-rc.1"),
                                                new WorkloadManifestInfo("microsoft.net.sdk.ios", "16.4.8825", "iosDirectory", "8.0.100-rc.1") };

            IWorkloadResolverFactory mockResolverFactory = new MockWorkloadResolverFactory(
                    Path.Combine(Path.GetTempPath(), "dotnetTestPath"),
                    "7.0.0",
                    new MockWorkloadResolver(new string[] { "maui-android", "maui-ios" }.Select(s => new WorkloadInfo(new WorkloadId(s), null)),
                    installedManifests),
                    "userProfileDir");

            var mauiAndroidPack = new PackInfo(new WorkloadPackId("maui-android-pack"), "34.0", WorkloadPackKind.Sdk, "androidDir", "maui-android-pack");
            var mauiIosPack = new PackInfo(new WorkloadPackId("maui-ios-pack"), "16.4", WorkloadPackKind.Framework, "iosDir", "maui-ios-pack");
            var aspirePack = new PackInfo(new WorkloadPackId("aspire-pack"), "8.0", WorkloadPackKind.Library, "aspireDir", "aspire-pack");

            MockPackWorkloadInstaller mockInstaller = new MockPackWorkloadInstaller(
                    installedWorkloads: new List<WorkloadId>() { new WorkloadId("maui-android"), new WorkloadId("maui-ios"), },
                    installedPacks: new List<PackInfo>() { mauiAndroidPack, mauiIosPack },
                    records: new List<WorkloadHistoryRecord>() { JsonSerializer.Deserialize<WorkloadHistoryRecord>(workloadHistoryRecord) })
            {
                WorkloadResolver = new MockWorkloadResolver(
                                        new string[] { "maui-android", "maui-ios", "aspire" }.Select(s => new WorkloadInfo(new WorkloadId(s), null)),
                                        installedManifests,
                                        id => new List<WorkloadPackId>() { new WorkloadPackId(id.ToString() + "-pack") },
                                        id => id.ToString().Contains("android") ? mauiAndroidPack :
                                              id.ToString().Contains("ios") ? mauiIosPack :
                                              id.ToString().Contains("aspire") ? aspirePack :
                                              null)
                                };

        IWorkloadManifestUpdater mockUpdater = new MockWorkloadManifestUpdater(new ManifestUpdateWithWorkloads[] {
                    new ManifestUpdateWithWorkloads(
                        new ManifestVersionUpdate(new ManifestId("microsoft.net.sdk.android"), new ManifestVersion("34.0.0-rc.1"), "8.0.100-rc.1", null, null), null),
                    new ManifestUpdateWithWorkloads(
                        new ManifestVersionUpdate(new ManifestId("microsoft.net.sdk.ios"), new ManifestVersion("16.4.8825"), "8.0.100-rc.1", null, null), null) });

            WorkloadUpdateCommand update = new(
                Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--from-history", "1" }),
                Reporter.Output,
                mockResolverFactory,
                mockInstaller,
                new MockNuGetPackageDownloader(),
                mockUpdater);

            mockInstaller.InstallationRecordRepository.WorkloadInstallRecord.Should().BeEquivalentTo(new List<WorkloadId>() { new WorkloadId("maui-android"), new WorkloadId("maui-ios") });
            mockInstaller.GarbageCollectionCalled.Should().BeFalse();
            update.Execute();
            mockInstaller.InstallationRecordRepository.WorkloadInstallRecord.Should().BeEquivalentTo(new List<WorkloadId>() { new WorkloadId("maui-android"), new WorkloadId("aspire") });
            mockInstaller.GarbageCollectionCalled.Should().BeTrue();
            mockInstaller.InstalledManifests.Select(m => m.manifestUpdate.ManifestId.ToString()).Should().BeEquivalentTo(new List<string>() { "microsoft.net.sdk.android", "microsoft.net.sdk.aspire" });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadUpdateItRemovesOldPacksAfterInstall(bool userLocal)
        {
            var testDirectory = _testAssetsManager.CreateTestDirectory(identifier: userLocal ? "userlocal" : "default").Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { _manifestPath }), dotnetRoot, userLocal, userProfileDir);
            var nugetDownloader = new MockNuGetPackageDownloader(dotnetRoot);
            var manifestUpdater = new MockWorkloadManifestUpdater();
            var sdkFeatureVersion = "6.0.100";
            var installingWorkload = "xamarin-android";

            string installRoot = userLocal ? userProfileDir : dotnetRoot;
            if (userLocal)
            {
                WorkloadFileBasedInstall.SetUserLocal(dotnetRoot, sdkFeatureVersion);
            }

            var workloadResolverFactory = new MockWorkloadResolverFactory(dotnetRoot, sdkFeatureVersion, workloadResolver, userProfileDir);

            // Install a workload
            var installParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "install", installingWorkload });
            var installCommand = new WorkloadInstallCommand(installParseResult, reporter: _reporter, workloadResolverFactory, nugetPackageDownloader: nugetDownloader,
                workloadManifestUpdater: manifestUpdater, tempDirPath: testDirectory);
            installCommand.Execute();

            // 7 packs in packs dir, 1 template pack
            var installPacks = Directory.GetDirectories(Path.Combine(installRoot, "packs"));
            installPacks.Count().Should().Be(7);
            foreach (var packDir in installPacks)
            {
                Directory.GetDirectories(packDir).Count().Should().Be(1); // 1 version of each pack installed
            }
            File.Exists(Path.Combine(installRoot, "metadata", "workloads", "InstalledPacks", "v1", "Xamarin.Android.Sdk", "8.4.7", "6.0.100")) // Original pack version is installed
                .Should().BeTrue();
            File.Exists(Path.Combine(installRoot, "template-packs", "xamarin.android.templates.1.0.3.nupkg"))
                .Should().BeTrue();
            // Install records are correct
            File.Exists(Path.Combine(installRoot, "metadata", "workloads", sdkFeatureVersion, "InstalledWorkloads", installingWorkload))
                .Should().BeTrue();
            var packRecordDirs = Directory.GetDirectories(Path.Combine(installRoot, "metadata", "workloads", "InstalledPacks", "v1"));
            packRecordDirs.Count().Should().Be(8);
            foreach (var packRecordDir in packRecordDirs)
            {
                var packVersionRecordDirs = Directory.GetDirectories(packRecordDir);
                packVersionRecordDirs.Count().Should().Be(1); // 1 version of each pack installed
                Directory.GetFiles(packVersionRecordDirs.First()).Count().Should().Be(1); // 1 feature band file for this pack id and version
            }

            // Mock updating the manifest
            workloadResolverFactory.MockResult.WorkloadResolver = WorkloadResolver.CreateForTests(
                new MockManifestProvider(new[] { Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleUpdatedManifest"), "Sample.json") }),
                dotnetRoot, userLocal, userProfileDir);

            // Update workload
            var updateParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update" });
            var updateCommand = new WorkloadUpdateCommand(updateParseResult, reporter: _reporter, workloadResolverFactory, nugetPackageDownloader: nugetDownloader,
            workloadManifestUpdater: manifestUpdater, tempDirPath: testDirectory);
            updateCommand.Execute();

            // 6 packs in packs dir, 1 template pack
            var updatePacks = Directory.GetDirectories(Path.Combine(installRoot, "packs"));
            updatePacks.Count().Should().Be(6);
            foreach (var packDir in updatePacks)
            {
                Directory.GetDirectories(packDir).Count().Should().Be(1); // 1 version of each pack installed
            }
            File.Exists(Path.Combine(installRoot, "metadata", "workloads", "InstalledPacks", "v1", "Xamarin.Android.Sdk", "8.5.7", "6.0.100")) // New pack version is installed
                .Should().BeTrue();
            File.Exists(Path.Combine(installRoot, "template-packs", "xamarin.android.templates.2.1.3.nupkg"))
                .Should().BeTrue();
            // Install records are correct
            File.Exists(Path.Combine(installRoot, "metadata", "workloads", sdkFeatureVersion, "InstalledWorkloads", installingWorkload))
                .Should().BeTrue();
            packRecordDirs = Directory.GetDirectories(Path.Combine(installRoot, "metadata", "workloads", "InstalledPacks", "v1"));
            packRecordDirs.Count().Should().Be(7);
            foreach (var packRecordDir in packRecordDirs)
            {
                var packVersionRecordDirs = Directory.GetDirectories(packRecordDir);
                packVersionRecordDirs.Count().Should().Be(1); // 1 version of each pack installed
                Directory.GetFiles(packVersionRecordDirs.First()).Count().Should().Be(1); // 1 feature band file for this pack id and version
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenWorkloadUpdateAcrossFeatureBandsItUpdatesPacks(bool userLocal)
        {
            var testDirectory = _testAssetsManager.CreateTestDirectory(identifier: userLocal ? "userlocal" : "default").Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            var manifestPath = Path.Combine(_testAssetsManager.GetAndValidateTestProjectDirectory("SampleManifest"), "BasicSample.json");
            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { manifestPath }), dotnetRoot, userLocal, userProfileDir);
            var nugetDownloader = new MockNuGetPackageDownloader(dotnetRoot);
            var manifestUpdater = new MockWorkloadManifestUpdater();
            var sdkFeatureVersion = "6.0.100";
            var installingWorkload = "simple-workload";

            string installRoot = userLocal ? userProfileDir : dotnetRoot;
            if (userLocal)
            {
                WorkloadFileBasedInstall.SetUserLocal(dotnetRoot, sdkFeatureVersion);
            }

            var workloadResolverFactory = new MockWorkloadResolverFactory(dotnetRoot, sdkFeatureVersion, workloadResolver, userProfileDir);

            var workloadPacks = new List<PackInfo>() {
                CreatePackInfo("mock-pack-1", "1.0.0", WorkloadPackKind.Framework, Path.Combine(installRoot, "packs", "mock-pack-1", "1.0.0"), "mock-pack-1"),
                CreatePackInfo("mock-pack-2", "2.0.0", WorkloadPackKind.Framework, Path.Combine(installRoot, "packs", "mock-pack-2", "2.0.0"), "mock-pack-2")
            };

            // Lay out workload installs for a previous feature band
            var oldFeatureBand = "5.0.100";
            var packRecordDir = Path.Combine(installRoot, "metadata", "workloads", "InstalledPacks", "v1");
            foreach (var pack in workloadPacks)
            {
                Directory.CreateDirectory(Path.Combine(packRecordDir, pack.Id, pack.Version));
                File.Create(Path.Combine(packRecordDir, pack.Id, pack.Version, oldFeatureBand));
            }
            Directory.CreateDirectory(Path.Combine(installRoot, "metadata", "workloads", oldFeatureBand, "InstalledWorkloads"));
            Directory.CreateDirectory(Path.Combine(installRoot, "metadata", "workloads", sdkFeatureVersion, "InstalledWorkloads"));
            File.Create(Path.Combine(installRoot, "metadata", "workloads", oldFeatureBand, "InstalledWorkloads", installingWorkload));
            File.Create(Path.Combine(installRoot, "metadata", "workloads", sdkFeatureVersion, "InstalledWorkloads", installingWorkload));

            // Update workload (without installing any workloads to this feature band)
            var updateParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--from-previous-sdk" });
            var updateCommand = new WorkloadUpdateCommand(updateParseResult, reporter: _reporter, workloadResolverFactory, nugetPackageDownloader: nugetDownloader,
                workloadManifestUpdater: manifestUpdater, tempDirPath: testDirectory);
            updateCommand.Execute();

            foreach (var pack in workloadPacks)
            {
                Directory.Exists(pack.Path).Should().BeTrue(because: $"Pack should be installed {testDirectory}");
                File.Exists(Path.Combine(packRecordDir, pack.Id, pack.Version, oldFeatureBand))
                    .Should().BeTrue(because: "Pack install record should still be present for old feature band");
            }
            File.Exists(Path.Combine(installRoot, "metadata", "workloads", oldFeatureBand, "InstalledWorkloads", installingWorkload))
                .Should().BeTrue(because: "Workload install record should still be present for old feature band");
            File.Exists(Path.Combine(installRoot, "metadata", "workloads", sdkFeatureVersion, "InstalledWorkloads", installingWorkload))
                .Should().BeTrue(because: "Workload install record should be present for current feature band");
        }

        static PackInfo CreatePackInfo(string id, string version, WorkloadPackKind kind, string path, string resolvedPackageId) => new(new WorkloadPackId(id), version, kind, path, resolvedPackageId);

        [Fact]
        public void GivenWorkloadUpdateItUpdatesOutOfDatePacks()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            (_, var command, var installer, _, _, _) = GetTestInstallers(_parseResult, installedWorkloads: mockWorkloadIds, installedFeatureBand: "6.0.100");

            command.Execute();

            installer.GarbageCollectionCalled.Should().BeTrue();
            installer.CachePath.Should().BeNull();
            installer.InstalledPacks.Count.Should().Be(8);
            installer.InstalledPacks.Where(pack => pack.Id.ToString().Contains("Android")).Count().Should().Be(8);
        }

        [Fact]
        public void GivenWorkloadUpdateItRollsBackOnFailedUpdate()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android"), new WorkloadId("xamarin-android-build") };
            (_, var command, var installer, var workloadResolver, _, _) = GetTestInstallers(_parseResult, installedWorkloads: mockWorkloadIds, failingPack: "Xamarin.Android.Framework", installedFeatureBand: "6.0.100");


            var exceptionThrown = Assert.Throws<GracefulException>(() => command.Execute());
            exceptionThrown.Message.Should().Contain("Failing pack: Xamarin.Android.Framework");
            var expectedPacks = mockWorkloadIds
                .SelectMany(workloadId => workloadResolver.GetPacksInWorkload(workloadId))
                .Distinct()
                .Select(packId => workloadResolver.TryGetPackInfo(packId))
                .Where(pack => pack != null);
            installer.RolledBackPacks.Should().BeEquivalentTo(expectedPacks);
            installer.InstallationRecordRepository.WorkloadInstallRecord.Should().BeEquivalentTo(mockWorkloadIds);
        }

        [Fact]
        public void GivenWorkloadUpdateItCanDownloadToOfflineCache()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            var cachePath = Path.Combine(_testAssetsManager.CreateTestDirectory(identifier: "cachePath").Path, "mockCachePath");
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--download-to-cache", cachePath });
            (_, var command, _, _, var manifestUpdater, var packageDownloader) = GetTestInstallers(parseResult, installedWorkloads: mockWorkloadIds, includeInstalledPacks: true, installedFeatureBand: "6.0.100");

            command.Execute();

            // Manifest packages should have been 'downloaded' and used for pack resolution
            manifestUpdater.GetManifestPackageDownloadsCallCount.Should().Be(1);
            // 7 android pack packages need to be updated, plus one manifest
            packageDownloader.DownloadCallParams.Count.Should().Be(8);
            foreach (var downloadParams in packageDownloader.DownloadCallParams)
            {
                downloadParams.downloadFolder.Value.Value.Should().Be(cachePath);
                downloadParams.id.ToString().Should().NotBe("xamarin.android.sdk");  // This pack is up to date, doesn't need to be cached
            }
        }

        [Fact]
        public void GivenWorkloadUpdateItCanInstallFromOfflineCache()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            var cachePath = "mockCachePath";
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--from-cache", cachePath });
            (_, var command, var installer, _, _, var nugetDownloader) = GetTestInstallers(parseResult, installedWorkloads: mockWorkloadIds, installedFeatureBand: "6.0.100");

            command.Execute();

            installer.GarbageCollectionCalled.Should().BeTrue();
            installer.CachePath.Should().Contain(cachePath);
            installer.InstalledPacks.Count.Should().Be(8);
            installer.InstalledPacks.Where(pack => pack.Id.ToString().Contains("Android")).Count().Should().Be(8);
            nugetDownloader.DownloadCallParams.Count().Should().Be(0);
        }

        [Fact]
        public void GivenWorkloadUpdateItPrintsDownloadUrls()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--print-download-link-only" });
            (_, var command, _, _, _, _) = GetTestInstallers(parseResult, installedWorkloads: mockWorkloadIds, includeInstalledPacks: true, installedFeatureBand: "6.0.100");

            command.Execute();

            _reporter.Lines.Should().Contain("==allPackageLinksJsonOutputStart==");
            string.Join(" ", _reporter.Lines).Should().Contain("http://mock-url/xamarin.android.templates.1.0.3.nupkg", "New pack urls should be included in output");
            string.Join(" ", _reporter.Lines).Should().Contain("http://mock-url/xamarin.android.framework.8.4.0.nupkg", "Urls for packs with updated versions should be included in output");
            string.Join(" ", _reporter.Lines).Should().NotContain("xamarin.android.sdk", "Urls for packs with the same version should not be included in output");
        }

        [Fact]
        public void GivenWorkloadUpdateItPrintsDownloadUrlsForNewFeatureBand()
        {
            var mockWorkloadIds = new WorkloadId[] { new WorkloadId("xamarin-android") };
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--print-download-link-only", "--sdk-version", "7.0.100" });
            (_, var command, _, _, _, _) = GetTestInstallers(parseResult, installedWorkloads: mockWorkloadIds, includeInstalledPacks: true, sdkVersion: "6.0.400");

            command.Execute();

            _reporter.Lines.Should().Contain("==allPackageLinksJsonOutputStart==");
            string.Join(" ", _reporter.Lines).Should().Contain("http://mock-url/xamarin.android.templates.1.0.3.nupkg", "New pack urls should be included in output");
            string.Join(" ", _reporter.Lines).Should().Contain("http://mock-url/xamarin.android.framework.8.4.0.nupkg", "Urls for packs with updated versions should be included in output");
            string.Join(" ", _reporter.Lines).Should().NotContain("xamarin.android.sdk", "Urls for packs with the same version should not be included in output");
        }

        [Fact]
        public void GivenWorkloadUpdateWithSdkVersionItErrors()
        {
            var testDirectory = _testAssetsManager.CreateTestDirectory().Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            var sdkFeatureVersion = "7.0.100";
            var updateParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--sdk-version", sdkFeatureVersion });

            var workloadResolverFactory = new MockWorkloadResolverFactory(dotnetRoot, sdkFeatureVersion, workloadResolver: null, userProfileDir);

            var exceptionThrown = Assert.Throws<GracefulException>(() => new WorkloadUpdateCommand(updateParseResult, reporter: _reporter, workloadResolverFactory: workloadResolverFactory));
            exceptionThrown.Message.Should().Contain("--sdk-version option is no longer supported");
        }

        [Fact]
        public void GivenOnlyUpdateAdManifestItSucceeds()
        {
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--advertising-manifests-only" });
            (_, var command, _, _, var manifestUpdater, _) = GetTestInstallers(parseResult, installedFeatureBand: "6.0.100");

            command.Execute();
            manifestUpdater.UpdateAdvertisingManifestsCallCount.Should().Be(1);
        }

        [Fact]
        public void GivenPrintRollbackDefinitionItIncludesAllInstalledManifests()
        {
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--print-rollback" });
            (_, var updateCommand, _, _, _, _) = GetTestInstallers(parseResult, installedFeatureBand: "6.0.100");


            updateCommand.Execute();
            _reporter.Lines.Count().Should().Be(3);
            string.Join("", _reporter.Lines).Should().Contain("samplemanifest");
        }

        [Theory]
        [InlineData("6.0.200", "6.0.200")]
        [InlineData("6.0.200", "6.0.100")]
        [InlineData("6.0.100", "6.0.200")]
        [InlineData("5.0.100", "6.0.100")]
        [InlineData("6.0.100", "5.0.100")]
        [InlineData("5.0.100", "6.0.300")]
        [InlineData("6.0.300", "5.0.100")]
        public void ApplyRollbackAcrossFeatureBand(string existingSdkFeatureBand, string newSdkFeatureBand)
        {
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--from-rollback-file", "rollback.json" });
      
            var manifestsToUpdate =
                new ManifestUpdateWithWorkloads[]
                    {
                        new(new ManifestVersionUpdate(new ManifestId("mock-manifest"), new ManifestVersion("1.0.0"), existingSdkFeatureBand, new ManifestVersion("2.0.0"), newSdkFeatureBand), null),
                    };
            (var dotnetPath, var updateCommand, var packInstaller, _, _, _) = GetTestInstallers(parseResult, manifestUpdates: manifestsToUpdate, sdkVersion: "6.0.300", identifier: existingSdkFeatureBand + newSdkFeatureBand, installedFeatureBand: existingSdkFeatureBand);

            updateCommand.UpdateWorkloads();

            packInstaller.InstalledManifests[0].manifestUpdate.ManifestId.Should().Be(manifestsToUpdate[0].ManifestUpdate.ManifestId);
            packInstaller.InstalledManifests[0].manifestUpdate.NewVersion.Should().Be(manifestsToUpdate[0].ManifestUpdate.NewVersion);
            packInstaller.InstalledManifests[0].manifestUpdate.NewFeatureBand.Should().Be(manifestsToUpdate[0].ManifestUpdate.NewFeatureBand);
            packInstaller.InstalledManifests[0].manifestUpdate.ExistingVersion.Should().Be(manifestsToUpdate[0].ManifestUpdate.ExistingVersion);
            packInstaller.InstalledManifests[0].manifestUpdate.ExistingFeatureBand.Should().Be(manifestsToUpdate[0].ManifestUpdate.ExistingFeatureBand);
            packInstaller.InstalledManifests[0].offlineCache.Should().Be(null);

            var defaultJsonPath = Path.Combine(dotnetPath, "metadata", "workloads", "6.0.300", "InstallState", "default.json");
            File.Exists(defaultJsonPath).Should().BeTrue();
            var json = JsonDocument.Parse(new FileStream(defaultJsonPath, FileMode.Open, FileAccess.Read), new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            json.RootElement.Should().NotBeNull();
            json.RootElement.GetProperty("manifests").GetProperty("mock-manifest").GetString().Should().Be("2.0.0/" + newSdkFeatureBand);
        }

        [Fact]
        public void ApplyRollbackWithMultipleManifestsAcrossFeatureBand()
        {
            var parseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--from-rollback-file", "rollback.json" });

            var manifestsToUpdate =
                new ManifestUpdateWithWorkloads[]
                    {
                        new(new ManifestVersionUpdate(new ManifestId("mock-manifest-1"), new ManifestVersion("1.0.0"), "6.0.300", new ManifestVersion("2.0.0"), "6.0.100"), null),
                        new(new ManifestVersionUpdate(new ManifestId("mock-manifest-2"), new ManifestVersion("1.0.0"), "6.0.100", new ManifestVersion("2.0.0"), "6.0.300"), null),
                        new(new ManifestVersionUpdate(new ManifestId("mock-manifest-3"), new ManifestVersion("1.0.0"), "5.0.100", new ManifestVersion("2.0.0"), "6.0.100"), null),
                    };
            (_, var updateCommand, var packInstaller, _, _, _) = GetTestInstallers(parseResult, manifestUpdates: manifestsToUpdate, sdkVersion: "6.0.300", installedFeatureBand: "6.0.300");

            updateCommand.UpdateWorkloads();

            packInstaller.InstalledManifests[0].manifestUpdate.ManifestId.Should().Be(manifestsToUpdate[0].ManifestUpdate.ManifestId);
            packInstaller.InstalledManifests[0].manifestUpdate.NewVersion.Should().Be(manifestsToUpdate[0].ManifestUpdate.NewVersion);
            packInstaller.InstalledManifests[0].manifestUpdate.NewFeatureBand.Should().Be("6.0.100");
            packInstaller.InstalledManifests[1].manifestUpdate.NewFeatureBand.Should().Be("6.0.300");
            packInstaller.InstalledManifests[2].manifestUpdate.NewFeatureBand.Should().Be("6.0.100");
            packInstaller.InstalledManifests[0].manifestUpdate.ExistingFeatureBand.Should().Be("6.0.300");
            packInstaller.InstalledManifests[1].manifestUpdate.ExistingFeatureBand.Should().Be("6.0.100");
            packInstaller.InstalledManifests[2].manifestUpdate.ExistingFeatureBand.Should().Be("5.0.100");
            packInstaller.InstalledManifests[0].offlineCache.Should().Be(null);
        }

        [Fact]
        public void GivenInvalidVersionInRollbackFileItErrors()
        {
            _reporter.Clear();
            
            var testDirectory = _testAssetsManager.CreateTestDirectory().Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var userProfileDir = Path.Combine(testDirectory, "user-profile");
            Directory.CreateDirectory(userProfileDir);

            var mockRollbackFileContent = @"{""mock.workload"":""6.0.0.15/6.0.100""}";
            var rollbackFilePath = Path.Combine(testDirectory, "rollback.json");
            File.WriteAllText(rollbackFilePath, mockRollbackFileContent);
            
            var updateParseResult = Parser.Instance.Parse(new string[] { "dotnet", "workload", "update", "--from-rollback-file", rollbackFilePath });

            string sdkVersion = "6.0.100";

            //  Create a "real" workload resolver with test parameters
            var sdkWorkloadManifestProvider = new SdkDirectoryWorkloadManifestProvider(dotnetRoot, sdkVersion, userProfileDir, globalJsonPath: null);
            var workloadResolver = WorkloadResolver.Create(sdkWorkloadManifestProvider, dotnetRoot, sdkVersion, userProfileDir);
            var workloadResolverFactory = new MockWorkloadResolverFactory(dotnetRoot, "6.0.100", workloadResolver, userProfileDir);

            var updateCommand = new WorkloadUpdateCommand(updateParseResult, reporter: _reporter, workloadResolverFactory: workloadResolverFactory, tempDirPath: testDirectory);

            var exception = Assert.Throws<GracefulException>(() => updateCommand.Execute());
            exception.InnerException.Should().BeOfType<FormatException>();
            exception.InnerException.Message.Should().Contain(string.Format(Workloads.Workload.Install.LocalizableStrings.InvalidVersionForWorkload, "mock.workload", "6.0.0.15"));
        }

        internal (string, WorkloadUpdateCommand, MockPackWorkloadInstaller, IWorkloadResolver, MockWorkloadManifestUpdater, MockNuGetPackageDownloader) GetTestInstallers(
            ParseResult parseResult,
            [CallerMemberName] string testName = "",
            string failingWorkload = null,
            string failingPack = null,
            IEnumerable<ManifestUpdateWithWorkloads> manifestUpdates = null,
            IList<WorkloadId> installedWorkloads = null,
            bool includeInstalledPacks = false,
            string sdkVersion = "6.0.100",
            string identifier = null,
            string installedFeatureBand = null)
        {
            _reporter.Clear();
            var testDirectory = _testAssetsManager.CreateTestDirectory(testName: testName, identifier).Path;
            var dotnetRoot = Path.Combine(testDirectory, "dotnet");
            var installedPacks = new PackInfo[] {
                CreatePackInfo("Xamarin.Android.Sdk", "8.4.7", WorkloadPackKind.Sdk, Path.Combine(dotnetRoot, "packs", "Xamarin.Android.Sdk", "8.4.7"), "Xamarin.Android.Sdk"),
                CreatePackInfo("Xamarin.Android.Framework", "8.2.0", WorkloadPackKind.Framework, Path.Combine(dotnetRoot, "packs", "Xamarin.Android.Framework", "8.2.0"), "Xamarin.Android.Framework")
            };
            var installer = includeInstalledPacks ?
                new MockPackWorkloadInstaller(failingWorkload, failingPack, installedWorkloads: installedWorkloads, installedPacks: installedPacks) :
                new MockPackWorkloadInstaller(failingWorkload, failingPack, installedWorkloads: installedWorkloads);

            var copiedManifestFolder = Path.Combine(dotnetRoot, "sdk-manifests", new SdkFeatureBand(sdkVersion).ToString(), "SampleManifest");
            Directory.CreateDirectory(copiedManifestFolder);
            var copiedManifestFile = Path.Combine(copiedManifestFolder, "WorkloadManifest.json");
            File.Copy(_manifestPath, copiedManifestFile);

            var workloadResolver = WorkloadResolver.CreateForTests(new MockManifestProvider(new[] { copiedManifestFile }), dotnetRoot);
            installer.WorkloadResolver = workloadResolver;
            var nugetDownloader = new MockNuGetPackageDownloader(dotnetRoot);
            var manifestUpdater = new MockWorkloadManifestUpdater(manifestUpdates);

            var workloadResolverFactory = new MockWorkloadResolverFactory(dotnetRoot, sdkVersion, workloadResolver, userProfileDir: testDirectory);

            var installManager = new WorkloadUpdateCommand(
                parseResult,
                reporter: _reporter,
                workloadResolverFactory: workloadResolverFactory,
                workloadInstaller: installer,
                nugetPackageDownloader: nugetDownloader,
                workloadManifestUpdater: manifestUpdater);

            return (dotnetRoot, installManager, installer, workloadResolver, manifestUpdater, nugetDownloader);
        }
    }
}
