﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.NET.TestFramework
{
    public static class NuGetConfigWriter
    {
        public static readonly string DotnetCoreBlobFeed = "https://dotnetfeed.blob.core.windows.net/dotnet-core/index.json";
        public static readonly string AspNetCoreDevFeed = "https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json";

        public static void Write(string folder, params string[] nugetSources)
        {
            Write(folder, nugetSources.ToList());
        }
        public static void Write(string folder, List<string> nugetSources)
        {
            string configFilePath = Path.Combine(folder, "NuGet.Config");
            var root = new XElement("configuration");

            var packageSources = new XElement("packageSources");
            root.Add(packageSources);

            for (int i=0;i<nugetSources.Count;i++)
            {
                packageSources.Add(new XElement("add",
                    new XAttribute("key", Guid.NewGuid().ToString()),
                    new XAttribute("value", nugetSources[i])
                    ));
            }

            File.WriteAllText(configFilePath, root.ToString());
        }
    }
}
