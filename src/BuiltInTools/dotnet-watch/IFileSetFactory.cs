﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Watcher
{
    internal interface IFileSetFactory
    {
        Task<FileSet> CreateAsync(CancellationToken cancellationToken);
    }
}
