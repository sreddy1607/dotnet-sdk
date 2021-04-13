// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Workloads.Workload.Install
{
    internal interface IWorkloadManifestUpdater
    {
        void UpdateAdvertisingManifests(SdkFeatureBand featureBand);

        IEnumerable<(ManifestId manifestId, ManifestVersion existingVersion, ManifestVersion newVersion)> CalculateManifestUpdates(SdkFeatureBand featureBand);
    }
}
