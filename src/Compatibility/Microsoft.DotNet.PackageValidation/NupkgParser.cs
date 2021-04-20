// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace Microsoft.DotNet.PackageValidation
{
    /// <summary>
    /// Creates a package object from the nupkg package.
    /// </summary>
    public class NupkgParser
    {
        /// <summary>
        /// Creates the package object from package path.
        /// </summary>
        /// <param name="packagePath">The path to the package path.</param>
        /// <param name="runtimeGraph">The path to the the runtime graph.</param>
        /// <returns>The package object.</returns>
        public static Package CreatePackage(string packagePath, string runtimeGraph)
        {
            PackageArchiveReader packageReader = new PackageArchiveReader(packagePath);
            Package package = CreatePackage(packageReader, runtimeGraph);
            package.PackagePath = packagePath;
            return package;
        }

        /// <summary>
        /// Creates the package object from package stream.
        /// </summary>
        /// <param name="packageStream">The package stream</param>
        /// <param name="runtimeGraph">The path to the the runtime graph</param>
        /// <returns></returns>
        public static Package CreatePackage(MemoryStream packageStream, string runtimeGraph)
        {
            PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
            Package package = CreatePackage(packageReader, runtimeGraph);
            package.PackageStream = packageStream;
            return package;
        }

        private static Package CreatePackage(PackageArchiveReader packageReader, string runtimeGraph)
        {
            NuspecReader nuspecReader = packageReader.NuspecReader;
            string packageId = nuspecReader.GetId();
            string version = nuspecReader.GetVersion().ToString();
            IEnumerable<PackageDependencyGroup> dependencyGroups = nuspecReader.GetDependencyGroups();
            
            Dictionary<NuGetFramework, IEnumerable<PackageDependency>> packageDependencies = new Dictionary<NuGetFramework, IEnumerable<PackageDependency>>();
            foreach (var item in dependencyGroups)
            {
                packageDependencies.Add(item.TargetFramework, item.Packages);
            }

            return  new Package(packageId, version, packageReader.GetFiles()?.Where(t => t.EndsWith(packageId + ".dll")), packageDependencies, runtimeGraph);
        }
    }
}
