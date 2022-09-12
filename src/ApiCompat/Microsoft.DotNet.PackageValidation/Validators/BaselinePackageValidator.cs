// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ApiCompatibility.Logging;
using Microsoft.DotNet.ApiCompatibility.Runner;
using NuGet.ContentModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.PackageValidation.Validators
{
    /// <summary>
    /// Validates that no target framework / rid support is dropped in the latest package.
    /// Reports all the breaking changes in the latest package.
    /// </summary>
    public class BaselinePackageValidator : IPackageValidator
    {
        private readonly ICompatibilityLogger _log;
        private readonly IApiCompatRunner _apiCompatRunner;

        public BaselinePackageValidator(ICompatibilityLogger log,
            IApiCompatRunner apiCompatRunner)
        {
            _log = log;
            _apiCompatRunner = apiCompatRunner;
        }

        /// <summary>
        /// Validates the latest nuget package doesnot drop any target framework/rid and does not introduce any breaking changes.
        /// </summary>
        /// <param name="package">Nuget Package that needs to be validated.</param>
        public void Validate(PackageValidatorOption options)
        {
            if (options.BaselinePackage is null)
                throw new ArgumentNullException(nameof(options.BaselinePackage));

            ApiCompatRunnerOptions apiCompatOptions = new(options.EnableStrictMode, isBaselineComparison: true);

            // Iterate over all target frameworks in the package.
            foreach (NuGetFramework baselineTargetFramework in options.BaselinePackage.FrameworksInPackage)
            {
                // Retrieve the compile time assets from the baseline package
                IReadOnlyList<ContentItem>? baselineCompileAssets = options.BaselinePackage.FindBestCompileAssetForFramework(baselineTargetFramework);
                if (baselineCompileAssets != null)
                {
                    // Search for compatible compile time assets in the latest package.
                    IReadOnlyList<ContentItem>? latestCompileAssets = options.Package.FindBestCompileAssetForFramework(baselineTargetFramework);
                    // Emit an error if
                    // - No latest compile time asset is available or
                    // - The latest compile time asset is a placeholder but the baseline compile time asset isn't.
                    if (latestCompileAssets == null ||
                        (latestCompileAssets.IsPlaceholderFile() && !baselineCompileAssets.IsPlaceholderFile()))
                    {
                        _log.LogError(
                            new Suppression(DiagnosticIds.TargetFrameworkDropped) { Target = baselineTargetFramework.ToString() },
                            DiagnosticIds.TargetFrameworkDropped,
                            Resources.MissingTargetFramework,
                            baselineTargetFramework.ToString());
                    }
                    else if (baselineCompileAssets.IsPlaceholderFile() && !latestCompileAssets.IsPlaceholderFile())
                    {
                        // Ignore the newly added compile time asset in the latest package.
                    }
                    else if (options.EnqueueApiCompatWorkItems)
                    {
                        _apiCompatRunner.QueueApiCompatFromContentItem(_log,
                            baselineCompileAssets,
                            latestCompileAssets,
                            apiCompatOptions,
                            options.BaselinePackage,
                            options.Package);
                    }
                }

                // Retrieve runtime baseline assets and searches for compatible runtime assets in the latest package.
                IReadOnlyList<ContentItem>? baselineRuntimeAssets = options.BaselinePackage.FindBestRuntimeAssetForFramework(baselineTargetFramework);
                if (baselineRuntimeAssets != null)
                {
                    // Search for compatible runtime assets in the latest package.
                    IReadOnlyList<ContentItem>? latestRuntimeAssets = options.Package.FindBestRuntimeAssetForFramework(baselineTargetFramework);
                    // Emit an error if
                    // - No latest runtime asset is available or
                    // - The latest runtime asset is a placeholder but the baseline runtime asset isn't.
                    if (latestRuntimeAssets == null ||
                        (latestRuntimeAssets.IsPlaceholderFile() && !baselineRuntimeAssets.IsPlaceholderFile()))
                    {
                        _log.LogError(
                            new Suppression(DiagnosticIds.TargetFrameworkDropped) { Target = baselineTargetFramework.ToString() },
                            DiagnosticIds.TargetFrameworkDropped,
                            Resources.MissingTargetFramework,
                            baselineTargetFramework.ToString());
                    }
                    else if (baselineRuntimeAssets.IsPlaceholderFile() && !latestRuntimeAssets.IsPlaceholderFile())
                    {
                        // Ignore the newly added run time asset in the latest package.
                    }
                    else if (options.EnqueueApiCompatWorkItems)
                    {
                        _apiCompatRunner.QueueApiCompatFromContentItem(_log,
                            baselineRuntimeAssets,
                            latestRuntimeAssets,
                            apiCompatOptions,
                            options.BaselinePackage,
                            options.Package);
                    }
                }

                // Retrieve runtime specific baseline assets and searches for compatible runtime specific assets in the latest package.
                IReadOnlyList<ContentItem>? baselineRuntimeSpecificAssets = options.BaselinePackage.FindBestRuntimeSpecificAssetForFramework(baselineTargetFramework);
                if (baselineRuntimeSpecificAssets != null && baselineRuntimeSpecificAssets.Count > 0)
                {
                    IEnumerable<IGrouping<string, ContentItem>> baselineRuntimeSpecificAssetsRidGroupedPerRid = baselineRuntimeSpecificAssets
                        .Where(t => t.Path.StartsWith("runtimes"))
                        .GroupBy(t => (string)t.Properties["rid"]);

                    foreach (IGrouping<string, ContentItem> baselineRuntimeSpecificAssetsRidGroup in baselineRuntimeSpecificAssetsRidGroupedPerRid)
                    {
                        IReadOnlyList<ContentItem> baselineRuntimeSpecificAssetsForRid = baselineRuntimeSpecificAssetsRidGroup.ToArray();
                        IReadOnlyList<ContentItem>? latestRuntimeSpecificAssets = options.Package.FindBestRuntimeAssetForFrameworkAndRuntime(baselineTargetFramework, baselineRuntimeSpecificAssetsRidGroup.Key);
                        // Emit an error if
                        // - No latest runtime specific asset is available or
                        // - The latest runtime specific asset is a placeholder but the baseline runtime specific asset isn't.
                        if (latestRuntimeSpecificAssets == null ||
                            (latestRuntimeSpecificAssets.IsPlaceholderFile() && !baselineRuntimeSpecificAssetsForRid.IsPlaceholderFile()))
                        {
                            _log.LogError(
                                new Suppression(DiagnosticIds.TargetFrameworkAndRidPairDropped) { Target = baselineTargetFramework.ToString() + "-" + baselineRuntimeSpecificAssetsRidGroup.Key },
                                DiagnosticIds.TargetFrameworkAndRidPairDropped,
                                Resources.MissingTargetFrameworkAndRid,
                                baselineTargetFramework.ToString(),
                                baselineRuntimeSpecificAssetsRidGroup.Key);
                        }
                        else if (baselineRuntimeSpecificAssetsForRid.IsPlaceholderFile() && !latestRuntimeSpecificAssets.IsPlaceholderFile())
                        {
                            // Ignore the newly added runtime specific asset in the latest package.
                        }
                        else if (options.EnqueueApiCompatWorkItems)
                        {
                            _apiCompatRunner.QueueApiCompatFromContentItem(_log,
                                baselineRuntimeSpecificAssetsForRid,
                                latestRuntimeSpecificAssets,
                                apiCompatOptions,
                                options.BaselinePackage,
                                options.Package);
                        }
                    }
                }
            }

            if (options.ExecuteApiCompatWorkItems)
                _apiCompatRunner.ExecuteWorkItems();
        }
    }
}
