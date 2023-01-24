﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.ApiCompatibility.Logging;
using Microsoft.DotNet.ApiSymbolExtensions.Logging;

namespace Microsoft.DotNet.ApiCompatibility.Tests
{
    internal class SuppressableTestLog : ISuppressableLog
    {
        public List<string> errors = new();
        public List<string> warnings = new();

        public bool HasLoggedErrors => errors.Count != 0;
        public bool SuppressionWasLogged => HasLoggedErrors;

        public bool LogError(Suppression suppression, string code, string format, params string[] args)
        {
            errors.Add($"{code} {string.Format(format, args)}");
            return true;
        }
        public void LogError(string format, params string[] args) => throw new NotImplementedException();
        public void LogError(string code, string format, params string[] args) => throw new NotImplementedException();
        
        public bool LogWarning(Suppression suppression, string code, string format, params string[] args)
        {
            errors.Add($"{code} {string.Format(format, args)}");
            return true;
        }
        public void LogWarning(string format, params string[] args) => throw new NotImplementedException();
        public void LogWarning(string code, string format, params string[] args) => throw new NotImplementedException();

        public void LogMessage(string format, params string[] args) => throw new NotImplementedException();
        public void LogMessage(MessageImportance importance, string format, params string[] args) => throw new NotImplementedException();
    }
}
