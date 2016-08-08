﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Core.Build.Tasks
{
    internal static class MetadataKeys
    {
        // General Metadata
        internal const string Name = "Name";
        internal const string Type = "Type";
        internal const string Version = "Version";
        internal const string FileGroup = "FileGroup";
        internal const string Path = "Path";
        internal const string ResolvedPath = "ResolvedPath";

        // Target Metadata
        internal const string RuntimeIdentifier = "RuntimeIdentifier";
        internal const string TargetFrameworkMoniker = "TargetFrameworkMoniker";
        internal const string FrameworkName = "FrameworkName";
        internal const string FrameworkVersion = "FrameworkVersion";

        // Foreign Keys
        internal const string ParentTarget = "ParentTarget";
        internal const string ParentTargetLibrary = "ParentTargetLibrary";
        internal const string ParentPackage = "ParentPackage";

        // Tags
        internal const string Analyzer = "Analyzer";
        internal const string AnalyzerLanguage = "AnalyzerLanguage";
    }
}
