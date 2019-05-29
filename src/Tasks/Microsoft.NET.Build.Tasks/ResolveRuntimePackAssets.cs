﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    public class ResolveRuntimePackAssets : TaskBase
    {
        public ITaskItem[] ResolvedRuntimePacks { get; set; }

        public ITaskItem[] FrameworkReferences { get; set; } = Array.Empty<ITaskItem>();

        public ITaskItem[] UnavailableRuntimePacks { get; set; } = Array.Empty<ITaskItem>();

        public ITaskItem[] SatelliteResourceLanguages { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public ITaskItem[] RuntimePackAssets { get; set; }

        protected override void ExecuteCore()
        {
            var runtimePackAssets = new List<ITaskItem>();

            HashSet<string> frameworkReferenceNames = new HashSet<string>(FrameworkReferences.Select(item => item.ItemSpec), StringComparer.OrdinalIgnoreCase);

            foreach (var unavailableRuntimePack in UnavailableRuntimePacks)
            {
                if (frameworkReferenceNames.Contains(unavailableRuntimePack.ItemSpec))
                {
                    //  This is a runtime pack that should be used, but wasn't available for the specified RuntimeIdentifier
                    //  NETSDK1082: There was no runtime pack for {0} available for the specified RuntimeIdentifier '{1}'.
                    Log.LogError(Strings.NoRuntimePackAvailable, unavailableRuntimePack.ItemSpec,
                        unavailableRuntimePack.GetMetadata(MetadataKeys.RuntimeIdentifier));
                }
            }

            HashSet<string> processedRuntimePackRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var runtimePack in ResolvedRuntimePacks)
            {
                if (!frameworkReferenceNames.Contains(runtimePack.GetMetadata(MetadataKeys.FrameworkName)))
                {
                    //  This is a runtime pack for a shared framework that ultimately wasn't referenced, so don't include its assets
                    continue;
                }

                string runtimePackRoot = runtimePack.GetMetadata(MetadataKeys.PackageDirectory);

                if (string.IsNullOrEmpty(runtimePackRoot) || !Directory.Exists(runtimePackRoot))
                {
                    //  If we do the work in https://github.com/dotnet/cli/issues/10528,
                    //  then we should add a new error message here indicating that the runtime pack hasn't
                    //  been downloaded, and that restore should be run with that runtime identifier.
                    Log.LogError(Strings.NoRuntimePackAvailable, runtimePack.ItemSpec,
                        runtimePack.GetMetadata(MetadataKeys.RuntimeIdentifier));
                }

                if (!processedRuntimePackRoots.Add(runtimePackRoot))
                {
                    //  We already added assets from this runtime pack (which can happen with FrameworkReferences to different
                    //  profiles of the same shared framework)
                    continue;
                }

                string runtimeIdentifier = runtimePack.GetMetadata(MetadataKeys.RuntimeIdentifier);

                //  These hard-coded paths are temporary until we have "real" runtime packs, which will likely have a flattened
                //  folder structure and a manifest indicating how the files should be used: https://github.com/dotnet/cli/issues/10442
                string runtimeAssetsPath = Path.Combine(runtimePackRoot, "runtimes", runtimeIdentifier, "lib", "netcoreapp3.0");
                string nativeAssetsPath = Path.Combine(runtimePackRoot, "runtimes", runtimeIdentifier, "native");

                var runtimeAssets = Directory.Exists(runtimeAssetsPath) ? Directory.GetFiles(runtimeAssetsPath) : Array.Empty<string>();
                var nativeAssets = Directory.Exists(nativeAssetsPath) ? Directory.GetFiles(nativeAssetsPath) : Array.Empty<string>();

                void AddAsset(string assetPath, string assetType)
                {
                    if (assetPath.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith("._", StringComparison.Ordinal))
                    {
                        //  Don't add assets for these files (shouldn't be necessary if/once we have a manifest in the runtime pack
                        //  https://github.com/dotnet/cli/issues/10442
                        return;
                    }

                    var assetItem = new TaskItem(assetPath);

                    assetItem.SetMetadata(MetadataKeys.CopyLocal, "true");
                    assetItem.SetMetadata(MetadataKeys.DestinationSubPath, Path.GetFileName(assetPath));
                    assetItem.SetMetadata(MetadataKeys.AssetType, assetType);
                    assetItem.SetMetadata(MetadataKeys.PackageName, runtimePack.GetMetadata(MetadataKeys.PackageName));
                    assetItem.SetMetadata(MetadataKeys.PackageVersion, runtimePack.GetMetadata(MetadataKeys.PackageVersion));
                    assetItem.SetMetadata(MetadataKeys.RuntimeIdentifier, runtimeIdentifier);
                    assetItem.SetMetadata(MetadataKeys.IsTrimmable, runtimePack.GetMetadata(MetadataKeys.IsTrimmable));

                    runtimePackAssets.Add(assetItem);
                }

                foreach (var asset in runtimeAssets)
                {
                    AddAsset(asset, "runtime");
                }
                foreach (var asset in nativeAssets)
                {
                    AddAsset(asset, "native");
                }

                runtimePackAssets.AddRange(EnumerateResourceAssets(runtimePackRoot, runtimeIdentifier, runtimePack));
            }

            RuntimePackAssets = runtimePackAssets.ToArray();
        }

        private IEnumerable<ITaskItem> EnumerateResourceAssets(string runtimePackRoot, string runtimeIdentifier, ITaskItem runtimePack)
        {
            //  These hard-coded paths are temporary until we have "real" runtime packs, which will likely have a flattened structure
            var directory = Path.Combine(runtimePackRoot, "runtimes", runtimeIdentifier, "lib", "netcoreapp3.0");
            if (!Directory.Exists(directory))
            {
                yield break;
            }

            foreach (var subdir in Directory.EnumerateDirectories(directory))
            {
                foreach (var asset in EnumerateCultureAssets(subdir, runtimeIdentifier, runtimePack))
                {
                    yield return asset;
                }
            }
        }

        private IEnumerable<ITaskItem> EnumerateCultureAssets(string cultureDirectory, string runtimeIdentifier, ITaskItem runtimePack)
        {
            var culture = Path.GetFileName(cultureDirectory);

            if (this.SatelliteResourceLanguages.Length > 1 &&
                !this.SatelliteResourceLanguages.Any(lang => string.Equals(lang.ItemSpec, culture, StringComparison.OrdinalIgnoreCase)))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(cultureDirectory, "*.resources.dll"))
            {
                var item = new TaskItem(file);

                item.SetMetadata(MetadataKeys.CopyLocal, "true");
                item.SetMetadata(MetadataKeys.DestinationSubDirectory, culture + Path.DirectorySeparatorChar);
                item.SetMetadata(MetadataKeys.DestinationSubPath, Path.Combine(culture, Path.GetFileName(file)));
                item.SetMetadata(MetadataKeys.AssetType, "resources");
                item.SetMetadata(MetadataKeys.PackageName, runtimePack.GetMetadata(MetadataKeys.PackageName));
                item.SetMetadata(MetadataKeys.PackageVersion, runtimePack.GetMetadata(MetadataKeys.PackageVersion));
                item.SetMetadata(MetadataKeys.RuntimeIdentifier, runtimeIdentifier);
                item.SetMetadata(MetadataKeys.Culture, culture);

                yield return item;
            }
        }
    }
}
