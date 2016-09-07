﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System;
using System.Linq;

namespace Microsoft.NETCore.Build.Tasks.UnitTests
{
    internal static class LockFileSnippets
    {
        #region LockFile String Snippet Creation Utilities

        public static string CreateLockFileSnippet(
            string[] targets,
            string[] libraries,
            string[] projectFileDependencyGroups)
        {
            return $@"{{
              ""locked"": false,
              ""version"": 2,
              ""targets"": {{{string.Join(",", targets)}}},
              ""libraries"": {{{string.Join(",", libraries)}}},
              ""projectFileDependencyGroups"": {{{string.Join(",", projectFileDependencyGroups)}}}
            }}";
        }

        public static string CreateLibrary(string nameVer, string type = "package", params string[] members)
        {
            return $@" ""{nameVer}"": {{
                ""sha512"": ""abcde=="",
                ""type"": ""{type}"",
                ""path"": ""{nameVer}"",
                ""files"": [{ToStringList(members)}]
            }}";
        }

        public static string CreateProjectLibrary(string nameVer, string path, string msbuildProject)
        {
            return $@" ""{nameVer}"": {{
                ""type"": ""project"",
                ""path"": ""{path}"",
                ""msbuildProject"": ""{msbuildProject}""
            }}";
        }

        public static string CreateTarget(string tfm, params string[] targetLibs)
        {
            return $"\"{tfm}\": {{{string.Join(",", targetLibs)}}}";
        }

        public static string CreateTargetLibrary(
            string nameVer,
            string type = "package",
            string[] dependencies = null,
            string[] frameworkAssemblies = null,
            string[] compile = null,
            string[] runtime = null,
            string[] native = null,
            string[] resource = null,
            string[] runtimeTargets = null,
            string[] contentFiles = null)
        {
            List<string> parts = new List<string>();
            parts.Add($"\"type\": \"{type}\"");

            if (frameworkAssemblies != null)
            {
                parts.Add($"\"frameworkAssemblies\": [{ToStringList(frameworkAssemblies)}]");
            }

            Action<string, string[]> addListIfPresent = (label, list) =>
            {
                if (list != null) parts.Add($"\"{label}\": {{{string.Join(",", list)}}}");
            };

            addListIfPresent("dependencies", dependencies);
            addListIfPresent("compile", compile);
            addListIfPresent("runtime", runtime);
            addListIfPresent("native", native);
            addListIfPresent("resource", resource);
            addListIfPresent("runtimeTargets", runtimeTargets);
            addListIfPresent("contentFiles", contentFiles);

            return $@" ""{nameVer}"": {{
                {string.Join(",", parts)}
            }}";
        }

        public static string CreateFileItem(string path, Dictionary<string, string> metadata = null)
        {
            var metadataString = metadata == null
                ? string.Empty
                : string.Join(",", metadata.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value}\""));

            return $"\"{path}\":{{{metadataString}}}";
        }

        public static string CreateProjectFileDependencyGroup(string tfm, params string[] dependencies)
        {
            return $"\"{tfm}\": [{ToStringList(dependencies)}]";
        }

        private static string ToStringList(params string[] members)
        {
            return members == null ? string.Empty : string.Join(",", members.Select(m => $"\"{m}\""));
        }

        #endregion

        #region Default Lock File String Snippets

        public static readonly string ProjectGroup =
            CreateProjectFileDependencyGroup("", "LibA >= 1.2.3");

        public static readonly string NETCoreGroup =
            CreateProjectFileDependencyGroup(".NETCoreApp,Version=v1.0");

        public static readonly string NETCoreOsxGroup =
            CreateProjectFileDependencyGroup(".NETCoreApp,Version=v1.0/osx.10.11-x64");

        public static readonly string LibADefn =
            CreateLibrary("LibA/1.2.3", "package", "lib/file/A.dll", "lib/file/B.dll", "lib/file/C.dll");

        public static readonly string LibBDefn =
            CreateLibrary("LibB/1.2.3", "package", "lib/file/D.dll", "lib/file/E.dll", "lib/file/F.dll");

        public static readonly string LibCDefn =
            CreateLibrary("LibC/1.2.3", "package", "lib/file/G.dll", "lib/file/H.dll", "lib/file/I.dll");

        public static readonly string TargetLibA = CreateTargetLibrary("LibA/1.2.3", "package",
            dependencies: new string[] { "\"LibB\": \"1.2.3\"" },
            frameworkAssemblies: new string[] { "System.Some.Lib" },
            compile: new string[] { CreateFileItem("lib/file/A.dll"), CreateFileItem("lib/file/B.dll") },
            runtime: new string[] { CreateFileItem("lib/file/A.dll"), CreateFileItem("lib/file/B.dll") }
            );

        public static readonly string TargetLibB = CreateTargetLibrary("LibB/1.2.3", "package",
            dependencies: new string[] { "\"LibC\": \"1.2.3\"" },
            frameworkAssemblies: new string[] { "System.Some.Lib" },
            compile: new string[] { CreateFileItem("lib/file/D.dll"), CreateFileItem("lib/file/E.dll") },
            runtime: new string[] { CreateFileItem("lib/file/D.dll"), CreateFileItem("lib/file/E.dll") }
            );

        public static readonly string TargetLibC = CreateTargetLibrary("LibC/1.2.3", "package",
            compile: new string[] { CreateFileItem("lib/file/G.dll"), CreateFileItem("lib/file/H.dll") },
            runtime: new string[] { CreateFileItem("lib/file/G.dll"), CreateFileItem("lib/file/H.dll") }
            );

        public static readonly string TargetLibBAllAssets = CreateTargetLibrary("LibB/1.2.3", "package",
            frameworkAssemblies: new string[] { "System.Some.Lib" },
            dependencies: new string[] { "\"LibC\": \"1.2.3\"" },
            compile: new string[] { CreateFileItem("lib/file/C1.dll") },
            runtime: new string[] { CreateFileItem("lib/file/R1.dll") },
            native: new string[] { CreateFileItem("lib/file/N1.dll") },
            resource: new string[] {
                    CreateFileItem(
                        "lib/file/R2.resources.dll",
                        metadata: new Dictionary<string, string>() { {"locale", "de"} })
            },
            runtimeTargets: new string[] {
                    CreateFileItem(
                        "runtimes/osx/native/R3.dylib",
                        metadata: new Dictionary<string, string>() {
                            { "assetType", "native"}, { "rid", "osx" }
                        })
            },
            contentFiles: new string[] {
                    CreateFileItem(
                        "contentFiles/any/images/C2.png",
                        metadata: new Dictionary<string, string>() {
                            { "buildAction", "EmbeddedResource" },
                            { "codeLanguage", "any" },
                            { "copyToOutput", "false" },
                        })
            }
            );

        #endregion
    }
}