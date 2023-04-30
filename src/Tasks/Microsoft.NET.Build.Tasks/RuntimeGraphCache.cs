﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using NuGet.RuntimeModel;

namespace Microsoft.NET.Build.Tasks
{
    internal class RuntimeGraphCache
    {
        private IBuildEngine4 _buildEngine;
        private Logger _log;

        public RuntimeGraphCache(TaskBase task)
        {
            _buildEngine = task.BuildEngine4;
            _log = task.Log;
        }

        public RuntimeGraph GetRuntimeGraph(string runtimeJsonPath)
        {
            if (string.IsNullOrEmpty(runtimeJsonPath))
            {
                throw new ArgumentNullException(nameof(runtimeJsonPath));
            }
            if (!Path.IsPathRooted(runtimeJsonPath))
            {
                throw new BuildErrorException("Path not rooted: {0}", runtimeJsonPath);
            }

            string key = GetTaskObjectKey(runtimeJsonPath);

            RuntimeGraph result;
            object existingRuntimeGraphTaskObject = _buildEngine.GetRegisteredTaskObject(key, RegisteredTaskObjectLifetime.AppDomain);
            if (existingRuntimeGraphTaskObject == null)
            {
                result = JsonRuntimeFormat.ReadRuntimeGraph(runtimeJsonPath);

                _buildEngine.RegisterTaskObject(key, result, RegisteredTaskObjectLifetime.AppDomain, true);
            }
            else
            {
                result = (RuntimeGraph)existingRuntimeGraphTaskObject;
            }

            return result;
        }

        private static string GetTaskObjectKey(string runtimeJsonPath)
        {
            return $"{nameof(RuntimeGraphCache)}:{runtimeJsonPath}";
        }
    }
}
