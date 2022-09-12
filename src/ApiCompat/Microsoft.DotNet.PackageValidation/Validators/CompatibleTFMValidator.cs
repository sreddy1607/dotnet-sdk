// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ApiCompatibility.Logging;
using Microsoft.DotNet.ApiCompatibility.Runner;
using NuGet.ContentModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.PackageValidation.Validators
{
    /// <summary>
    /// Validates that there are compile time and runtime assets for all the compatible frameworks.
    /// Queues the apicompat between the applicable compile and runtime assemblies for these frameworks.
    /// </summary>
    public class CompatibleTfmValidator : IPackageValidator
    {
        private static readonly Dictionary<NuGetFramework, HashSet<NuGetFramework>> s_packageTfmMapping = InitializeTfmMappings();
        private readonly ICompatibilityLogger _log;
        private readonly IApiCompatRunner _apiCompatRunner;

        public CompatibleTfmValidator(ICompatibilityLogger log,
            IApiCompatRunner apiCompatRunner)
        {
            _log = log;
            _apiCompatRunner = apiCompatRunner;
        }

        /// <summary>
        /// Validates that there are compile time and runtime assets for all the compatible frameworks.
        /// Validates that the surface between compile time and runtime assets is compatible.
        /// </summary>
        /// <param name="package">Nuget Package that needs to be validated.</param>
        public void Validate(PackageValidatorOption options)
        {
            ApiCompatRunnerOptions apiCompatOptions = new(options.EnableStrictMode);

            HashSet<NuGetFramework> compatibleTargetFrameworks = new();
            foreach (NuGetFramework item in options.Package.FrameworksInPackage)
            {
                compatibleTargetFrameworks.Add(item);
                if (s_packageTfmMapping.ContainsKey(item))
                {
                    compatibleTargetFrameworks.UnionWith(s_packageTfmMapping[item]);
                }
            }

            foreach (NuGetFramework framework in compatibleTargetFrameworks)
            {
                IReadOnlyList<ContentItem>? compileTimeAsset = options.Package.FindBestCompileAssetForFramework(framework);
                if (compileTimeAsset == null)
                {
                    _log.LogError(
                        new Suppression(DiagnosticIds.ApplicableCompileTimeAsset) { Target = framework.ToString() },
                        DiagnosticIds.ApplicableCompileTimeAsset,
                        Resources.NoCompatibleCompileTimeAsset,
                        framework.ToString());
                    continue;
                }

                IReadOnlyList<ContentItem>? runtimeAsset = options.Package.FindBestRuntimeAssetForFramework(framework);
                // Emit an error if
                // - No runtime asset is available or
                // - The runtime asset is a placeholder but the compile time asset isn't.
                if (runtimeAsset == null ||
                    (runtimeAsset.IsPlaceholderFile() && !compileTimeAsset.IsPlaceholderFile()))
                {
                    _log.LogError(
                        new Suppression(DiagnosticIds.CompatibleRuntimeRidLessAsset) { Target = framework.ToString() },
                        DiagnosticIds.CompatibleRuntimeRidLessAsset,
                        Resources.NoCompatibleRuntimeAsset,
                        framework.ToString());
                }
                // Ignore the additional runtime asset when performing in non-strict mode, otherwise emit a missing
                // compile time asset error.
                else if (compileTimeAsset.IsPlaceholderFile() && !runtimeAsset.IsPlaceholderFile())
                {
                    if (options.EnableStrictMode)
                    {
                        _log.LogError(
                            new Suppression(DiagnosticIds.ApplicableCompileTimeAsset) { Target = framework.ToString() },
                            DiagnosticIds.ApplicableCompileTimeAsset,
                            Resources.NoCompatibleCompileTimeAsset,
                            framework.ToString());
                    }
                }
                // Invoke ApiCompat to compare the compile time asset with the runtime asset.
                else if (options.EnqueueApiCompatWorkItems)
                {
                    _apiCompatRunner.QueueApiCompatFromContentItem(_log,
                        compileTimeAsset,
                        runtimeAsset,
                        apiCompatOptions,
                        options.Package);
                }

                foreach (string rid in options.Package.Rids.Where(rid => framework.SupportsRuntimeIdentifier(rid)))
                {
                    IReadOnlyList<ContentItem>? runtimeRidSpecificAsset = options.Package.FindBestRuntimeAssetForFrameworkAndRuntime(framework, rid);
                    // Emit an error if
                    // - No runtime specific asset is available or
                    // - The runtime specific asset is a placeholder but the compile time asset isn't.
                    if (runtimeRidSpecificAsset == null ||
                        (runtimeRidSpecificAsset.IsPlaceholderFile() && !compileTimeAsset.IsPlaceholderFile()))
                    {
                        _log.LogError(
                            new Suppression(DiagnosticIds.CompatibleRuntimeRidSpecificAsset) { Target = framework.ToString() + "-" + rid },
                            DiagnosticIds.CompatibleRuntimeRidSpecificAsset,
                            Resources.NoCompatibleRidSpecificRuntimeAsset,
                            framework.ToString(),
                            rid);
                    }
                    // Ignore the additional runtime specific asset when performing in non-strict mode, otherwise emit a
                    // missing compile time asset error.
                    else if (compileTimeAsset.IsPlaceholderFile() && !runtimeRidSpecificAsset.IsPlaceholderFile())
                    {
                        if (options.EnableStrictMode)
                        {
                            _log.LogError(
                                new Suppression(DiagnosticIds.ApplicableCompileTimeAsset) { Target = framework.ToString() },
                                DiagnosticIds.ApplicableCompileTimeAsset,
                                Resources.NoCompatibleCompileTimeAsset,
                                framework.ToString());
                        }
                    }
                    // Invoke ApiCompat to compare the compile asset with the runtime specific asset.
                    else if (options.EnqueueApiCompatWorkItems)
                    {
                        _apiCompatRunner.QueueApiCompatFromContentItem(_log,
                            compileTimeAsset,
                            runtimeRidSpecificAsset,
                            apiCompatOptions,
                            options.Package);
                    }
                }
            }

            if (options.ExecuteApiCompatWorkItems)
                _apiCompatRunner.ExecuteWorkItems();
        }

        private static Dictionary<NuGetFramework, HashSet<NuGetFramework>> InitializeTfmMappings()
        {
            Dictionary<NuGetFramework, HashSet<NuGetFramework>> packageTfmMapping = new();

            // creating a map framework in package => frameworks to test based on default compatibilty mapping.
            foreach (OneWayCompatibilityMappingEntry item in DefaultFrameworkMappings.Instance.CompatibilityMappings)
            {
                NuGetFramework forwardTfm = item.SupportedFrameworkRange.Max;
                NuGetFramework reverseTfm = item.TargetFrameworkRange.Min;
                if (packageTfmMapping.ContainsKey(forwardTfm))
                {
                    packageTfmMapping[forwardTfm].Add(reverseTfm);
                }
                else
                {
                    packageTfmMapping.Add(forwardTfm, new HashSet<NuGetFramework> { reverseTfm });
                }
            }

            return packageTfmMapping;
        }
    }
}
