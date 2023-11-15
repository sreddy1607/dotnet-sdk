// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.NET.Sdk.WorkloadManifestReader;

namespace Microsoft.DotNet.Workloads.Workload.Install
{
    internal interface IWorkloadManifestUpdater
    {
        Task UpdateAdvertisingManifestsAsync(bool includePreviews, DirectoryPath? offlineCache = null, IEnumerable<WorkloadManifestInfo> manifests = null);

        Task BackgroundUpdateAdvertisingManifestsWhenRequiredAsync();

        IEnumerable<ManifestUpdateWithWorkloads> CalculateManifestUpdates();

        IEnumerable<ManifestVersionUpdate> CalculateManifestRollbacks(string rollbackDefinitionFilePath, WorkloadHistoryRecorder recorder);

        Task<IEnumerable<WorkloadDownload>> GetManifestPackageDownloadsAsync(bool includePreviews, SdkFeatureBand providedSdkFeatureBand, SdkFeatureBand installedSdkFeatureBand);

        IEnumerable<WorkloadId> GetUpdatableWorkloadsToAdvertise(IEnumerable<WorkloadId> installedWorkloads);

        void DeleteUpdatableWorkloadsFile();

        ManifestVersionWithBand GetInstalledManifestVersion(ManifestId manifestId, bool throwIfNotFound = true);
    }

    internal record ManifestUpdateWithWorkloads(ManifestVersionUpdate ManifestUpdate, WorkloadCollection Workloads);

    internal record ManifestVersionWithBand(ManifestVersion Version, SdkFeatureBand Band);
}
