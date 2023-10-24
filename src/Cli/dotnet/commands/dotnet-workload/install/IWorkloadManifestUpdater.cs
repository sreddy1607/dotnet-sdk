// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using static Microsoft.DotNet.Workloads.Workload.Install.WorkloadManifestUpdater;

namespace Microsoft.DotNet.Workloads.Workload.Install
{
    internal interface IWorkloadManifestUpdater
    {
        Task UpdateAdvertisingManifestsAsync(bool includePreviews, DirectoryPath? offlineCache = null, IEnumerable<WorkloadManifestInfo> manifests = null);

        Task BackgroundUpdateAdvertisingManifestsWhenRequiredAsync();

        IEnumerable<ManifestUpdateWithWorkloads> CalculateManifestUpdates();

        IEnumerable<ManifestVersionUpdate>
            CalculateManifestRollbacks(string rollbackDefinitionFilePath, IEnumerable<(ManifestId id, ManifestVersionWithBand versionWithBand)> manifestRollbackContents);

        Task<IEnumerable<WorkloadDownload>> GetManifestPackageDownloadsAsync(bool includePreviews, SdkFeatureBand providedSdkFeatureBand, SdkFeatureBand installedSdkFeatureBand);

        IEnumerable<WorkloadId> GetUpdatableWorkloadsToAdvertise(IEnumerable<WorkloadId> installedWorkloads);

        void DeleteUpdatableWorkloadsFile();

        public ManifestVersionWithBand GetInstalledManifestVersion(ManifestId manifestId);

        IEnumerable<(ManifestId id, ManifestVersionWithBand ManifestWithBand)> ParseRollbackDefinitionFile(string rollbackDefinitionFilePath, SdkFeatureBand featureBand);
    }

    internal record ManifestUpdateWithWorkloads(ManifestVersionUpdate ManifestUpdate, WorkloadCollection Workloads);
}
