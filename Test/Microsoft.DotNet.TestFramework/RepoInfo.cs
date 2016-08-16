// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.TestFramework
{
    public class RepoInfo
    {
        private static string s_repoRoot;

        private static string s_configuration;

        public static string RepoRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(s_repoRoot))
                {
                    return s_repoRoot;
                }

                string directory = GetBaseDirectory();

                while (!Directory.Exists(Path.Combine(directory, ".git")) && directory != null)
                {
                    directory = Directory.GetParent(directory).FullName;
                }

                if (directory == null)
                {
                    throw new Exception("Cannot find the git repository root");
                }

                s_repoRoot = directory;
                return s_repoRoot;
            }
        }

        public static string Configuration
        {
            get
            {
                if (string.IsNullOrEmpty(s_configuration))
                {
                    s_configuration = FindConfigurationInBasePath();
                }

                return s_configuration;
            }
        }

        public static string Bin
        {
            get
            {
                return Path.Combine(RepoRoot, "bin");
            }
        }

        private static string FindConfigurationInBasePath()
        {
            string baseDirectory = GetBaseDirectory();

            var configuration = StripBinPathPrefixFromDirectory(baseDirectory);
            configuration = StripTestPathSuffixFromDirectory(configuration);

            return configuration;
        }

        private static string StripBinPathPrefixFromDirectory(string directory)
        {
            return directory.Remove(0, Bin.Length + 1);
        }

        private static string StripTestPathSuffixFromDirectory(string directory)
        {
            return directory.Remove(directory.IndexOf("Tests") - 1);
        }

        private static string GetBaseDirectory()
        {
#if NET451
            string directory = AppDomain.CurrentDomain.BaseDirectory;
#else
            string directory = AppContext.BaseDirectory;
#endif

            return directory;
        }
    }
}