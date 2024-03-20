// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Workloads.Workload.History;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.NET.Sdk.WorkloadManifestReader;

namespace Microsoft.DotNet.Workloads.Workload.Install
{
    internal interface IWorkloadManifestUpdater
    {
        Task UpdateAdvertisingManifestsAsync(bool includePreviews, bool useWorkloadSets = false, DirectoryPath? offlineCache = null);

        Task BackgroundUpdateAdvertisingManifestsWhenRequiredAsync();

        IEnumerable<ManifestUpdateWithWorkloads> CalculateManifestUpdates();

        IEnumerable<ManifestVersionUpdate> CalculateManifestRollbacks(string rollbackDefinitionFilePath, WorkloadHistoryRecorder recorder);

        IEnumerable<ManifestVersionUpdate> CalculateManifestUpdatesFromHistory(WorkloadHistoryRecord record);
        IEnumerable<ManifestVersionUpdate> ParseRollbackDefinitionFiles(IEnumerable<string> files, WorkloadHistoryRecorder recorder = null);

        Task<IEnumerable<WorkloadDownload>> GetManifestPackageDownloadsAsync(bool includePreviews, SdkFeatureBand providedSdkFeatureBand, SdkFeatureBand installedSdkFeatureBand);

        IEnumerable<WorkloadId> GetUpdatableWorkloadsToAdvertise(IEnumerable<WorkloadId> installedWorkloads);

        void DeleteUpdatableWorkloadsFile();

        ManifestVersionWithBand GetInstalledManifestVersion(ManifestId manifestId, bool throwIfNotFound = true);
        void DownloadWorkloadSet(string version, DirectoryPath? offlineCache);
    }

    internal record ManifestUpdateWithWorkloads(ManifestVersionUpdate ManifestUpdate, WorkloadCollection Workloads);

    internal record ManifestVersionWithBand(ManifestVersion Version, SdkFeatureBand Band);
}
