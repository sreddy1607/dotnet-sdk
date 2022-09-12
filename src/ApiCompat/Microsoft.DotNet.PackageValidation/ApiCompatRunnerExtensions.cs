// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using Microsoft.DotNet.ApiCompatibility.Logging;
using Microsoft.DotNet.ApiCompatibility.Runner;
using NuGet.ContentModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.PackageValidation
{
    /// <summary>
    /// <see cref="ApiCompatRunner"/> extension methods that are specific to package validation
    /// and rely on NuGet API.
    /// </summary>
    internal static class ApiCompatRunnerExtensions
    {
        public static void QueueApiCompatFromContentItem(this IApiCompatRunner apiCompatRunner,
            ICompatibilityLogger log,
            IReadOnlyList<ContentItem> leftContentItems,
            IReadOnlyList<ContentItem> rightContentItems,
            ApiCompatRunnerOptions options,
            Package leftPackage,
            Package? rightPackage = null)
        {
            // Don't enqueue duplicate items (if no right package is supplied and items match)
            if (rightPackage == null && leftContentItems.SequenceEqual(rightContentItems, ContentItemEqualityComparer.Instance))
            {
                return;
            }

            // Don't perform api compatibility checks on placeholder files.
            if (leftContentItems.IsPlaceholderFile() && rightContentItems.IsPlaceholderFile())
            {
                leftContentItems[0].Properties.TryGetValue("tfm", out object? tfmRaw);
                string tfm = tfmRaw?.ToString() ?? string.Empty;

                log.LogMessage(MessageImportance.Normal, Resources.SkipApiCompatForPlaceholderFiles, tfm);
            }

            // Make sure placeholder package files aren't enqueued as api comparison check work items.
            Debug.Assert(!leftContentItems.IsPlaceholderFile() && !rightContentItems.IsPlaceholderFile(),
                "Placeholder files must not be enqueued for api comparison checks.");

            MetadataInformation[] left = new MetadataInformation[leftContentItems.Count];
            for (int leftIndex = 0; leftIndex < leftContentItems.Count; leftIndex++)
            {
                left[leftIndex] = GetMetadataInformation(log,
                    leftPackage,
                    leftContentItems[leftIndex],
                    options.IsBaselineComparison ? Resources.Baseline + " " + leftContentItems[leftIndex].Path : null);
            }

            MetadataInformation[] right = new MetadataInformation[rightContentItems.Count];
            for (int rightIndex = 0; rightIndex < rightContentItems.Count; rightIndex++)
            {
                right[rightIndex] = GetMetadataInformation(log,
                    rightPackage ?? leftPackage,
                    rightContentItems[rightIndex]);
            }

            apiCompatRunner.EnqueueWorkItem(new ApiCompatRunnerWorkItem(left, options, right));
        }

        private static MetadataInformation GetMetadataInformation(ICompatibilityLogger log,
            Package package,
            ContentItem item,
            string? displayString = null)
        {
            displayString ??= item.Path;
            string[]? assemblyReferences = null;

            if (item.Properties.TryGetValue("tfm", out object? tfmObj))
            {
                string targetFramework = ((NuGetFramework)tfmObj).GetShortFolderName();

                if (package.AssemblyReferences != null && !package.AssemblyReferences.TryGetValue(targetFramework, out assemblyReferences))
                {
                    log.LogWarning(
                        new Suppression(DiagnosticIds.SearchDirectoriesNotFoundForTfm)
                        {
                            Target = displayString
                        },
                        DiagnosticIds.SearchDirectoriesNotFoundForTfm,
                        Resources.MissingSearchDirectory,
                        targetFramework,
                        displayString);
                }
            }

            return new MetadataInformation(Path.GetFileName(item.Path), item.Path, package.PackagePath, assemblyReferences, displayString);
        }
    }
}
