// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.ApiCompatibility;
using NuGet.ContentModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.PackageValidation
{
    /// <summary>
    /// Validates that there are compile time and runtime assets for all the compatible frameworks.
    /// Queues the apicompat between the applicable compile and runtime assemblies for these frameworks.
    /// </summary>
    public class CompatibleTfmValidator
    {
        private string _noWarn;
        private (string, string)[] _ignoredDifferences;
        private bool _runApiCompat;
        private ApiCompatRunner apiCompatRunner;
        private static Dictionary<NuGetFramework, HashSet<NuGetFramework>> s_packageTfmMapping = InitializeTfmMappings();

        public CompatibleTfmValidator(string noWarn, (string, string)[] ignoredDifferences, bool runApiCompat)
        {
            _noWarn = noWarn;
            _ignoredDifferences = ignoredDifferences;
            _runApiCompat = runApiCompat;
            apiCompatRunner = new(_noWarn, _ignoredDifferences);
        }

        /// <summary>
        /// Validates that there are compile time and runtime assets for all the compatible frameworks.
        /// Validates that the surface between compile time and runtime assets is compatible.
        /// </summary>
        /// <param name="package">Nuget Package that needs to be validated.</param>
        /// <returns>The List of Package Validation Diagnostics.</returns>
        public (DiagnosticBag<TargetFrameworkApplicabilityDiagnostics>, IEnumerable<ApiCompatDiagnostics>) Validate(Package package)
        {
            DiagnosticBag<TargetFrameworkApplicabilityDiagnostics> errors = new DiagnosticBag<TargetFrameworkApplicabilityDiagnostics>(_noWarn, _ignoredDifferences);

            HashSet<NuGetFramework> compatibleTargetFrameworks = new();
            foreach (NuGetFramework item in package.FrameworksInPackage)
            {
                if (s_packageTfmMapping.ContainsKey(item))
                {
                    compatibleTargetFrameworks.UnionWith(s_packageTfmMapping[item]);
                    compatibleTargetFrameworks.Add(item);
                }
            }

            foreach (NuGetFramework framework in compatibleTargetFrameworks)
            {
                ContentItem compileTimeAsset = package.FindBestCompileAssetForFramework(framework);

                if (compileTimeAsset == null)
                {
                    // modify the message here
                    errors.Add(new TargetFrameworkApplicabilityDiagnostics(DiagnosticIds.CompatibleRuntimeRidLessAsset,
                    framework.ToString(),
                    string.Format(Resources.NoCompatibleRuntimeAsset, framework.ToString())));
                    break;
                }

                ContentItem runtimeAsset = package.FindBestRuntimeAssetForFramework(framework);

                if (runtimeAsset == null)
                {
                    errors.Add(new TargetFrameworkApplicabilityDiagnostics(DiagnosticIds.CompatibleRuntimeRidLessAsset,
                        framework.ToString(),
                        string.Format(Resources.NoCompatibleRuntimeAsset, framework.ToString())));
                }
                else
                {
                    if (_runApiCompat)
                    {
                        apiCompatRunner.QueueApiCompat(Helpers.GetFileStreamFromPackage(package.PackagePath, compileTimeAsset.Path),
                            Helpers.GetFileStreamFromPackage(package.PackagePath, runtimeAsset.Path),
                            Path.GetFileName(package.PackagePath),
                             Resources.CompatibleTfmValidatorHeader,
                             string.Format(Resources.MissingApisForFramework, framework.ToString()));
                    }
                }
 
                foreach (string rid in package.Rids)
                {
                    runtimeAsset = package.FindBestRuntimeAssetForFrameworkAndRuntime(framework, rid);
                    if (runtimeAsset == null)
                    {
                        errors.Add(new TargetFrameworkApplicabilityDiagnostics(DiagnosticIds.CompatibleRuntimeRidSpecificAsset,
                            $"{framework}-" + rid,
                            string.Format(Resources.NoCompatibleRidSpecificRuntimeAsset, framework.ToString(), rid)));
                    }
                    else
                    {
                        if (_runApiCompat)
                        {
                            apiCompatRunner.QueueApiCompat(Helpers.GetFileStreamFromPackage(package.PackagePath, compileTimeAsset.Path),
                                Helpers.GetFileStreamFromPackage(package.PackagePath, runtimeAsset.Path),
                                Path.GetFileName(package.PackagePath),
                                Resources.CompatibleTfmValidatorHeader,
                                string.Format(Resources.MissingApisForFrameworkAndRid, framework.ToString(), rid));
                        }
                    }
                }
            }

            return (errors, apiCompatRunner.RunApiCompat());
        }

        private static Dictionary<NuGetFramework, HashSet<NuGetFramework>> InitializeTfmMappings()
        {
            Dictionary<NuGetFramework, HashSet<NuGetFramework>> packageTfmMapping = new();
            // creating a map framework in package => frameworks to test based on default compatibilty mapping.
            foreach (var item in DefaultFrameworkMappings.Instance.CompatibilityMappings)
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
