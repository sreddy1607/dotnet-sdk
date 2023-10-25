﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.ApiCompatibility.Runner
{
    /// <summary>
    /// Work item for the api compat runner
    /// </summary>
    /// <remarks>
    /// Creates a work item with a single left set, options and a single right set.
    /// </remarks>
    public readonly struct ApiCompatRunnerWorkItem(IReadOnlyList<MetadataInformation> left,
        ApiCompatRunnerOptions options,
        IReadOnlyList<MetadataInformation> right) : IEquatable<ApiCompatRunnerWorkItem>
    {
        /// <summary>
        /// The metadata information of the left assemblies to compare with the rights.
        /// </summary>
        public readonly IReadOnlyList<MetadataInformation> Left = left;

        /// <summary>
        /// The api compat options to configure the comparison checks.
        /// </summary>
        public readonly ApiCompatRunnerOptions Options = options;

        /// <summary>
        /// The metadata information of the right assemblies that are compared against the lefts.
        /// </summary>
        public IList<IReadOnlyList<MetadataInformation>> Right { get; } = new List<IReadOnlyList<MetadataInformation>>(new IReadOnlyList<MetadataInformation>[] { right });

        /// <summary>
        /// Creates a work item with a single left, options and a single right.
        /// </summary>
        public ApiCompatRunnerWorkItem(MetadataInformation left,
            ApiCompatRunnerOptions options,
            MetadataInformation right)
            : this(new MetadataInformation[] { left },
                  options,
                  new MetadataInformation[] { right })
        {
        }

        /// <inheritdoc />
        public bool Equals(ApiCompatRunnerWorkItem other) => other.Left.SequenceEqual(Left) && other.Options.Equals(Options);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ApiCompatRunnerWorkItem item && Equals(item);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 19 + Options.GetHashCode();
                foreach (MetadataInformation left in Left)
                {
                    hash = hash * 31 + left.GetHashCode();
                }
                return hash;
            }
        }

        /// <inheritdoc />
        public static bool operator ==(ApiCompatRunnerWorkItem workItem1, ApiCompatRunnerWorkItem workItem2) => workItem1.Equals(workItem2);

        /// <inheritdoc />
        public static bool operator !=(ApiCompatRunnerWorkItem workItem1, ApiCompatRunnerWorkItem workItem2) => !(workItem1 == workItem2);

        /// <inheritdoc />
        public override string ToString() => $"{Left.Select(l => l.AssemblyId).Aggregate((l1, l2) => l1 + ", " + l2)}: {Options}";
    }
}
