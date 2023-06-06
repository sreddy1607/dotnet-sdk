﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.DotNet.ApiCompatibility.Logging;
using Microsoft.DotNet.PackageValidation;
using Microsoft.NET.Build.Tasks;

namespace Microsoft.DotNet.ApiCompat.Task
{
    /// <summary>
    /// ApiCompat's ValidatePackage msbuild frontend.
    /// This task provides the functionality to compare package assets based on given inputs.
    /// </summary>
    public class ValidatePackageTask : TaskBase
    {
        // Important: Keep properties exposed in sync with the CLI frontend.

        /// <summary>
        /// The path to the package to inspect.
        /// </summary>
        [Required]
        public string? PackageTargetPath { get; set; }

        /// <summary>
        /// The path to the roslyn assemblies that should be loaded.
        /// </summary>
        [Required]
        public string? RoslynAssembliesPath { get; set; }

        /// <summary>
        /// A runtime graph that can be provided for package asset selection.
        /// </summary>
        public string? RuntimeGraph { get; set; }

        /// <summary>
        /// A NoWarn string contains the error codes that should be ignored.
        /// </summary>
        public string? NoWarn { get; set; }

        /// <summary>
        /// If true, includes both internal and public API.
        /// </summary>
        public bool RespectInternals { get; set; }

        /// <summary>
        /// Enables rule to check that attributes match.
        /// </summary>
        public bool EnableRuleAttributesMustMatch { get; set; }

        /// <summary>
        /// Set of files with types in DocId format of which attributes to exclude.
        /// </summary>
        public string[]? ExcludeAttributesFiles { get; set; }

        /// <summary>
        /// Enables rule to check that the parameter names between public methods do not change.
        /// </summary>
        public bool EnableRuleCannotChangeParameterName { get; set; }

        /// <summary>
        /// If true, performs api compatibility checks on the package assets.
        /// </summary>
        public bool RunApiCompat { get; set; } = true;

        /// <summary>
        /// Enables strict mode api comparison checks enqueued by the compatible tfm validator.
        /// </summary>
        public bool EnableStrictModeForCompatibleTfms { get; set; }

        /// <summary>
        /// Enables strict mode api comparison checks enqueued by the compatible framework in package validator.
        /// </summary>
        public bool EnableStrictModeForCompatibleFrameworksInPackage { get; set; }

        /// <summary>
        /// Enables strict mode api comparison checks enqueued by the baseline package validator.
        /// </summary>
        public bool EnableStrictModeForBaselineValidation { get; set; }

        /// <summary>
        /// The path to the baseline package that acts as the contract to inspect.
        /// </summary>
        public string? BaselinePackageTargetPath { get; set; }

        /// <summary>
        /// If true, generates a suppression file that contains the api compatibility errors.
        /// </summary>
        public bool GenerateSuppressionFile { get; set; }

        /// <summary>
        /// The path to suppression files. If provided, the suppressions are read and stored.
        /// </summary>
        public string[]? SuppressionFiles { get; set; }

        /// <summary>
        /// The path to the suppression output file that is written to, when <see cref="GenerateSuppressionFile"/> is true.
        /// </summary>
        public string? SuppressionOutputFile { get; set; }

        /// <summary>
        /// Assembly references grouped by target framework, for the assets inside the package.
        /// </summary>
        public ITaskItem[]? PackageAssemblyReferences { get; set; }

        /// <summary>
        /// Assembly references grouped by target framework, for the assets inside the baseline package.
        /// </summary>
        public ITaskItem[]? BaselinePackageAssemblyReferences { get; set; }

        public override bool Execute()
        {
            RoslynResolver roslynResolver = RoslynResolver.Register(RoslynAssembliesPath!);
            try
            {
                return base.Execute();
            }
            finally
            {
                roslynResolver.Unregister();
            }
        }

        protected override void ExecuteCore()
        {
            Func<ISuppressionEngine, SuppressableMSBuildLog> logFactory = (suppressionEngine) => new(Log, suppressionEngine);
            ValidatePackage.Run(logFactory,
                GenerateSuppressionFile,
                SuppressionFiles,
                SuppressionOutputFile,
                NoWarn,
                RespectInternals,
                EnableRuleAttributesMustMatch,
                ExcludeAttributesFiles,
                EnableRuleCannotChangeParameterName,
                PackageTargetPath!,
                RunApiCompat,
                EnableStrictModeForCompatibleTfms,
                EnableStrictModeForCompatibleFrameworksInPackage,
                EnableStrictModeForBaselineValidation,
                BaselinePackageTargetPath,
                RuntimeGraph,
                PackageAssemblyReferences is not null ? ParsePackageAssemblyReferences(PackageAssemblyReferences) : null,
                BaselinePackageAssemblyReferences is not null ? ParsePackageAssemblyReferences(BaselinePackageAssemblyReferences) : null);
        }

        private static IEnumerable<PackageAssemblyReferenceCollection> ParsePackageAssemblyReferences(ITaskItem[] packageAssemblyReferences)
        {
            foreach (ITaskItem taskItem in packageAssemblyReferences)
            {
                string? targetFrameworkMoniker = taskItem.GetMetadata("TargetFrameworkMoniker");
                string targetPlatformMoniker = taskItem.GetMetadata("TargetPlatformMoniker");
                string? referencePath = taskItem.GetMetadata("ReferencePath");

                // The TPM is null when the assembly doesn't target a platform.
                if (string.IsNullOrEmpty(targetFrameworkMoniker) || string.IsNullOrEmpty(referencePath))
                    continue;

                yield return new PackageAssemblyReferenceCollection(targetFrameworkMoniker,
                    targetPlatformMoniker,
                    referencePath.Split(','));
            }
        }
    }
}
