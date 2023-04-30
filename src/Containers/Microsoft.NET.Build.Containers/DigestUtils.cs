﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.NET.Build.Containers;

internal sealed class DigestUtils
{
    /// <summary>
    /// Gets digest for string <paramref name="str"/>.
    /// </summary>
    internal static string GetDigest(string str) => GetDigestFromSha(GetSha(str));

    /// <summary>
    /// Formats digest based on ready SHA <paramref name="sha"/>.
    /// </summary>
    internal static string GetDigestFromSha(string sha) => $"sha256:{sha}";

    /// <summary>
    /// Gets the SHA of <paramref name="str"/>.
    /// </summary>
    internal static string GetSha(string str)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(str), hash);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
