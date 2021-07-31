﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks;
using System.Linq;

namespace Microsoft.NET.Build.Tasks
{
    public class GenerateImplicitNamespaceImportsFile : TaskBase
    {
        [Required]
        public ITaskItem[] UniqueImport { get; set; }

        [Required]
        public ITaskItem File { get; set; }

        [Required]
        public bool Overwrite { get; set; }

        [Required]
        public bool WriteOnlyWhenDifferent { get; set; }

        protected override void ExecuteCore()
        {
            List<TaskItem> items = new List<TaskItem>();
            items.Add(new TaskItem("// <autogenerated />"));
            foreach (ITaskItem uniqueImportItem in UniqueImport)
            {
                string itemSpec = uniqueImportItem.ItemSpec;
                if (FilterImportCore(ref items, itemSpec))
                {
                    continue;
                }

                items.Add(new TaskItem($"global using global::{itemSpec};"));
            }

            // a hack to execute the WriteLinesToFile task manually.
            WriteLinesToFile writer = new WriteLinesToFile();
            writer.Lines = items.ToArray();
            writer.File = File;
            writer.Overwrite = Overwrite;
            writer.WriteOnlyWhenDifferent = WriteOnlyWhenDifferent;
            writer.Execute();
        }

        private bool FilterImportCore(ref List<TaskItem> items, string itemSpec)
        {
            if (!itemSpec.StartsWith("static "))
            {
                return FilterAliasImport(ref items, itemSpec);
            }

            string[] items2 = itemSpec.Split(' ');
            string str = items2[Array.IndexOf(items2, "static") + 1];
            items2[Array.IndexOf(items2, "static") + 1] = str.Insert(0, "global::");
            items.Add(new TaskItem($"global using {string.Join(" ", items2)};"));
            return true;
        }

        private bool FilterAliasImport(ref List<TaskItem> items, string itemSpec)
        {
            if (!itemSpec.Contains("="))
            {
                return false;
            }

            string[] items2 = itemSpec.Split(' ');
            items2[2] = items2[2].Insert(0, "global::");
            items.Add(new TaskItem($"global using {string.Join(" ", items2)};"));
            return true;
        }
    }
}
