﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.NETCore.Build.Tasks
{
    using PathAndPropertiesTuple = Tuple<string, IDictionary<string, string>>;

    /// <summary>
    /// Values for File Group Metadata corresponding to the groups in a target library
    /// </summary>
    public enum FileGroup
    {
        CompileTimeAssembly,
        RuntimeAssembly,
        ContentFile,
        NativeLibrary,
        ResourceAssembly,
        RuntimeTarget,
        FrameworkAssembly
    }

    public static class FileGroupExtensions
    {
        private static readonly IDictionary<string, string> _emptyProperties = new Dictionary<string, string>();

        /// <summary>
        /// Return Type metadata that should be applied to files in the target library group 
        /// </summary>
        public static string GetTypeMetadata(this FileGroup fileGroup)
        {
            switch (fileGroup)
            {
                case FileGroup.CompileTimeAssembly:
                case FileGroup.RuntimeAssembly:
                case FileGroup.NativeLibrary:
                case FileGroup.ResourceAssembly:
                case FileGroup.RuntimeTarget:
                    return "assembly";

                case FileGroup.FrameworkAssembly:
                    return "frameworkAssembly";

                case FileGroup.ContentFile:
                    return "content";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Return a list of file paths from the corresponding group in the target library
        /// </summary>
        public static IEnumerable<PathAndPropertiesTuple> GetFilePathAndProperies(
            this FileGroup fileGroup, LockFileTargetLibrary package)
        {
            switch (fileGroup)
            {
                case FileGroup.CompileTimeAssembly:
                    return SelectPath(package.CompileTimeAssemblies);

                case FileGroup.RuntimeAssembly:
                    return SelectPath(package.RuntimeAssemblies);

                case FileGroup.ContentFile:
                    return SelectPath(package.ContentFiles);

                case FileGroup.NativeLibrary:
                    return SelectPath(package.NativeLibraries);

                case FileGroup.ResourceAssembly:
                    return SelectPath(package.ResourceAssemblies);

                case FileGroup.RuntimeTarget:
                    return SelectPath(package.RuntimeTargets);

                case FileGroup.FrameworkAssembly:
                    return package.FrameworkAssemblies.Select(c => Tuple.Create(c, _emptyProperties));

                default:
                    throw new Exception($"Unexpected file group in project.lock.json target library {package.Name}");
            }
        }

        private static IEnumerable<PathAndPropertiesTuple> SelectPath<T>(IList<T> fileItemList) 
            where T : LockFileItem
            => fileItemList.Select(c => Tuple.Create(c.Path, c.Properties));
    }
}
