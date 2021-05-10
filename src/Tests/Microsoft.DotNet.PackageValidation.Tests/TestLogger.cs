﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.PackageValidation.Tests
{
    public class TestLogger : ILogger
    {
        public List<string> errors = new();

        public void LogError(string message)
        {
            errors.Add(message);
        }
    }
}
