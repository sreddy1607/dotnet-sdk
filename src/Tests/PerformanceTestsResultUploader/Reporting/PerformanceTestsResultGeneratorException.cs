﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace Reporting
{
    [Serializable]
    internal class PerformanceTestsResultUploaderException : Exception
    {
        public PerformanceTestsResultUploaderException()
        {
        }

        public PerformanceTestsResultUploaderException(string message) : base(message)
        {
        }

        public PerformanceTestsResultUploaderException(string message, Exception innerException) : base(message,
            innerException)
        {
        }

        protected PerformanceTestsResultUploaderException(SerializationInfo info, StreamingContext context) : base(
            info, context)
        {
        }
    }
}
