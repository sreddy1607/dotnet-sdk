// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Workloads.Workload.Install;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace Microsoft.DotNet.Workloads.Workload.Update
{
    internal class WorkloadUpdateCommand : InstallingWorkloadCommand
    {
        private readonly bool _adManifestOnlyOption;
        private readonly bool _printRollbackDefinitionOnly;
        private readonly bool _fromPreviousSdk;
        private readonly string _workloadSetMode;

        public WorkloadUpdateCommand(
            ParseResult parseResult,
            IReporter reporter = null,
            IWorkloadResolverFactory workloadResolverFactory = null,
            IInstaller workloadInstaller = null,
            INuGetPackageDownloader nugetPackageDownloader = null,
            IWorkloadManifestUpdater workloadManifestUpdater = null,
            string tempDirPath = null)
            : base(parseResult, reporter: reporter, workloadResolverFactory: workloadResolverFactory, workloadInstaller: workloadInstaller,
                  nugetPackageDownloader: nugetPackageDownloader, workloadManifestUpdater: workloadManifestUpdater,
                  tempDirPath: tempDirPath)

        {
            _workloadSetVersion = parseResult.GetValue(InstallingWorkloadCommandParser.WorkloadSetVersionOption);
            _fromPreviousSdk = parseResult.GetValue(WorkloadUpdateCommandParser.FromPreviousSdkOption);
            _adManifestOnlyOption = parseResult.GetValue(WorkloadUpdateCommandParser.AdManifestOnlyOption);
            _printRollbackDefinitionOnly = parseResult.GetValue(WorkloadUpdateCommandParser.PrintRollbackOption);
            _workloadSetMode = parseResult.GetValue(InstallingWorkloadCommandParser.WorkloadSetMode);

            _workloadInstaller = _workloadInstallerFromConstructor ?? WorkloadInstallerFactory.GetWorkloadInstaller(Reporter,
                                _sdkFeatureBand, _workloadResolver, Verbosity, _userProfileDir, VerifySignatures, PackageDownloader,
                                _dotnetPath, TempDirectoryPath, packageSourceLocation: _packageSourceLocation, RestoreActionConfiguration,
                                elevationRequired: !_printDownloadLinkOnly && !_printRollbackDefinitionOnly && string.IsNullOrWhiteSpace(_downloadToCacheOption));

            _workloadManifestUpdater = _workloadManifestUpdaterFromConstructor ?? new WorkloadManifestUpdater(Reporter, _workloadResolver, PackageDownloader, _userProfileDir,
                _workloadInstaller.GetWorkloadInstallationRecordRepository(), _workloadInstaller, _packageSourceLocation, sdkFeatureBand: _sdkFeatureBand);
        }

        public override int Execute()
        {
            if (!string.IsNullOrWhiteSpace(_downloadToCacheOption))
            {
                try
                {
                    DownloadToOfflineCacheAsync(new DirectoryPath(_downloadToCacheOption), _includePreviews).Wait();
                }
                catch (Exception e)
                {
                    throw new GracefulException(string.Format(LocalizableStrings.WorkloadCacheDownloadFailed, e.Message), e, isUserError: false);
                }
            }
            else if (_printDownloadLinkOnly)
            {
                var packageUrls = GetUpdatablePackageUrlsAsync(_includePreviews).GetAwaiter().GetResult();
                Reporter.WriteLine("==allPackageLinksJsonOutputStart==");
                Reporter.WriteLine(JsonSerializer.Serialize(packageUrls, new JsonSerializerOptions() { WriteIndented = true }));
                Reporter.WriteLine("==allPackageLinksJsonOutputEnd==");
            }
            else if (_adManifestOnlyOption)
            {
                _workloadManifestUpdater.UpdateAdvertisingManifestsAsync(
                    _includePreviews,
                    ShouldUseWorkloadSetMode(_sdkFeatureBand, _dotnetPath),
                    string.IsNullOrWhiteSpace(_fromCacheOption) ?
                        null :
                        new DirectoryPath(_fromCacheOption))
                    .Wait();
                Reporter.WriteLine();
                Reporter.WriteLine(LocalizableStrings.WorkloadUpdateAdManifestsSucceeded);
            }
            else if (_printRollbackDefinitionOnly)
            {
                var workloadSet = WorkloadSet.FromManifests(_workloadResolver.GetInstalledManifests());

                Reporter.WriteLine("==workloadRollbackDefinitionJsonOutputStart==");
                Reporter.WriteLine(workloadSet.ToJson());
                Reporter.WriteLine("==workloadRollbackDefinitionJsonOutputEnd==");
            }
            else if (!string.IsNullOrWhiteSpace(_workloadSetMode))
            {
                if (_workloadSetMode.Equals("workloadset", StringComparison.OrdinalIgnoreCase))
                {
                    _workloadInstaller.UpdateInstallMode(_sdkFeatureBand, true);
                }
                else if (_workloadSetMode.Equals("loosemanifest", StringComparison.OrdinalIgnoreCase) ||
                         _workloadSetMode.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    _workloadInstaller.UpdateInstallMode(_sdkFeatureBand, false);
                }
                else
                {
                    throw new GracefulException(string.Format(LocalizableStrings.WorkloadSetModeTakesWorkloadSetLooseManifestOrAuto, _workloadSetMode), isUserError: true);
                }
            }
            else
            {
                try
                {
                    var transaction = new CliTransaction();
                    transaction.RollbackStarted = () =>
                    {
                        Reporter.WriteLine(LocalizableStrings.RollingBackInstall);
                    };
                    // Don't hide the original error if roll back fails, but do log the rollback failure
                    transaction.RollbackFailed = ex =>
                    {
                        Reporter.WriteLine(string.Format(LocalizableStrings.RollBackFailedMessage, ex.Message));
                    };

                    transaction.Run(
                        action: context =>
                        {
                            DirectoryPath? offlineCache = string.IsNullOrWhiteSpace(_fromCacheOption) ? null : new DirectoryPath(_fromCacheOption);
                            if (string.IsNullOrWhiteSpace(_workloadSetVersion))
                            {
                                UpdateWorkloads(_includePreviews, offlineCache, context);
                            }
                            else
                            {
                                var workloadSetLocation = HandleWorkloadUpdateFromVersion(context, offlineCache);
                                CalculateManifestUpdatesAndUpdateWorkloads(false, true, workloadSetLocation, offlineCache, context);
                            }
                        });
                }
                catch (Exception e)
                {
                    // Don't show entire stack trace
                    throw new GracefulException(string.Format(LocalizableStrings.WorkloadUpdateFailed, e.Message), e, isUserError: false);
                }
            }

            _workloadInstaller.Shutdown();
            return _workloadInstaller.ExitCode;
        }

        public void UpdateWorkloads(bool includePreviews = false, DirectoryPath? offlineCache = null, ITransactionContext context = null)
        {
            Reporter.WriteLine();

            var useRollback = !string.IsNullOrWhiteSpace(_fromRollbackDefinition);
            var useWorkloadSets = ShouldUseWorkloadSetMode(_sdkFeatureBand, _dotnetPath);

            if (useRollback && useWorkloadSets)
            {
                // Rollback files are only for loose manifests. Update the mode to be loose manifests.
                Reporter.WriteLine(LocalizableStrings.UpdateFromRollbackSwitchesModeToLooseManifests);
                _workloadInstaller.UpdateInstallMode(_sdkFeatureBand, false);
                useWorkloadSets = false;
            }

            _workloadManifestUpdater.UpdateAdvertisingManifestsAsync(includePreviews, useWorkloadSets, offlineCache).Wait();

            string workloadSetLocation = null;
            if (useWorkloadSets)
            {
                workloadSetLocation = InstallWorkloadSet(context);
            }

            CalculateManifestUpdatesAndUpdateWorkloads(useRollback, useWorkloadSets, workloadSetLocation, offlineCache, context);
        }

        private void CalculateManifestUpdatesAndUpdateWorkloads(bool useRollback, bool useWorkloadSets, string workloadSetLocation, DirectoryPath? offlineCache, ITransactionContext context)
        {
            var workloadIds = GetUpdatableWorkloads();
            var manifestsToUpdate = useRollback ? _workloadManifestUpdater.CalculateManifestRollbacks(_fromRollbackDefinition) :
                useWorkloadSets ? _workloadManifestUpdater.CalculateManifestRollbacks(workloadSetLocation) :
                _workloadManifestUpdater.CalculateManifestUpdates().Select(m => m.ManifestUpdate);

            var workloadSetVersion = workloadSetLocation is null ? null : Path.GetFileName(Path.GetDirectoryName(workloadSetLocation));

            UpdateWorkloadsWithInstallRecord(_sdkFeatureBand, manifestsToUpdate, workloadSetVersion, useRollback, context, offlineCache);

            WorkloadInstallCommand.TryRunGarbageCollection(_workloadInstaller, Reporter, Verbosity, workloadSetVersion => _workloadResolverFactory.CreateForWorkloadSet(_dotnetPath, _sdkVersion.ToString(), _userProfileDir, workloadSetVersion), offlineCache);

            _workloadManifestUpdater.DeleteUpdatableWorkloadsFile();

            Reporter.WriteLine();
            Reporter.WriteLine(string.Format(LocalizableStrings.UpdateSucceeded, string.Join(" ", workloadIds)));
            Reporter.WriteLine();
        }

        private void UpdateWorkloadsWithInstallRecord(
            SdkFeatureBand sdkFeatureBand,
            IEnumerable<ManifestVersionUpdate> manifestsToUpdate,
            string workloadSetVersion,
            bool useRollback,
            ITransactionContext context,
            DirectoryPath? offlineCache = null)
        {
            context.Run(
                action: () =>
                {
                    foreach (var manifestUpdate in manifestsToUpdate)
                    {
                        _workloadInstaller.InstallWorkloadManifest(manifestUpdate, context, offlineCache, useRollback);
                    }

                    if (useRollback)
                    {
                        _workloadInstaller.SaveInstallStateManifestVersions(sdkFeatureBand, GetInstallStateContents(manifestsToUpdate));
                    }
                    else
                    {
                        _workloadInstaller.RemoveManifestsFromInstallState(sdkFeatureBand);
                    }

                    _workloadInstaller.AdjustWorkloadSetInInstallState(sdkFeatureBand, string.IsNullOrWhiteSpace(_workloadSetVersion) ? null : workloadSetVersion);

                    _workloadResolver.RefreshWorkloadManifests();

                    var workloads = GetUpdatableWorkloads();

                    _workloadInstaller.InstallWorkloads(workloads, sdkFeatureBand, context, offlineCache);
                },
                rollback: () =>
                {
                    //  Nothing to roll back at this level, InstallWorkloadManifest and InstallWorkloadPacks handle the transaction rollback
                    //  We will refresh the workload manifests to make sure that the resolver has the updated state after the rollback
                    _workloadResolver.RefreshWorkloadManifests();
                });
        }

        private async Task DownloadToOfflineCacheAsync(DirectoryPath offlineCache, bool includePreviews)
        {
            await GetDownloads(GetUpdatableWorkloads(), skipManifestUpdate: false, includePreviews, offlineCache.Value);
        }

        private async Task<IEnumerable<string>> GetUpdatablePackageUrlsAsync(bool includePreview)
        {
            var downloads = await GetDownloads(GetUpdatableWorkloads(), skipManifestUpdate: false, includePreview);

            var urls = new List<string>();
            foreach (var download in downloads)
            {
                urls.Add(await PackageDownloader.GetPackageUrl(new PackageId(download.NuGetPackageId), new NuGetVersion(download.NuGetPackageVersion), _packageSourceLocation));
            }

            return urls;
        }

        private IEnumerable<WorkloadId> GetUpdatableWorkloads()
        {
            var workloads = GetInstalledWorkloads(_fromPreviousSdk);

            if (workloads == null || !workloads.Any())
            {
                Reporter.WriteLine(LocalizableStrings.NoWorkloadsToUpdate);
            }

            return workloads;
        }
    }
}
