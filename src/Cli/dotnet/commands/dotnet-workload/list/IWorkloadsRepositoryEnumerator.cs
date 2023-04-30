// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.NET.Sdk.WorkloadManifestReader;

namespace Microsoft.DotNet.Workloads.Workload.List
{
    internal interface IWorkloadsRepositoryEnumerator
    {
        IEnumerable<WorkloadId> InstalledSdkWorkloadIds { get; }
        InstalledWorkloadsCollection AddInstalledVsWorkloads(IEnumerable<WorkloadId> sdkWorkloadIds);

        /// <summary>
        /// Gets deduplicated enumeration of transitive closure of 'extends' relation of installed workloads.
        /// </summary>
        /// <returns>Deduplicated enumeration of workload infos.</returns>
        IEnumerable<WorkloadResolver.WorkloadInfo> InstalledAndExtendedWorkloads { get; }
    }
}
