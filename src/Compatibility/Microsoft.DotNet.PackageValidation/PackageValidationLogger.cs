﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;
using Microsoft.DotNet.ValidationSuppression;
using Microsoft.NET.Build.Tasks;

namespace Microsoft.DotNet.PackageValidation
{
    internal class PackageValidationLogger : IPackageLogger
    {
        private readonly Logger _log;
        private readonly SuppressionEngine _suppressionEngine;
        private readonly bool _baselineAllErrors;

        public PackageValidationLogger(Logger log, string baselineFile)
            : this(log, baselineFile, false) {}

        public PackageValidationLogger(Logger log, string baselineFile, bool baselineAllErrors)
        {
            _log = log;
            _suppressionEngine = SuppressionEngine.CreateFromSuppressionFile(baselineFile);
            _baselineAllErrors = baselineAllErrors;
        }

        public void LogError(Suppression suppression, string code, string format, params string[] args)
        {
            if (!_suppressionEngine.IsErrorSuppressed(suppression))
            {
                if (_baselineAllErrors)
                {
                    _suppressionEngine.AddSuppression(suppression);
                }

                _log.LogNonSdkError(code, format, args);
            }
        }

        public void LogMessage(MessageImportance importance, string format, params string[] args) => _log.LogMessage(importance, format, args);

        public void LogErrorHeader(string message) => _log.LogNonSdkError(null, message);

        public void BaselineAllSuppressionsToFile(string baselineFile)
        {
            _suppressionEngine.WriteSuppressionsToFile(baselineFile);
        }
    }
}
