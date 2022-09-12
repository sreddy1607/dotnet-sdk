// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using NuGet.ContentModel;

namespace Microsoft.DotNet.PackageValidation
{
    internal class ContentItemEqualityComparer : IEqualityComparer<ContentItem>
    {
        public static readonly ContentItemEqualityComparer Instance = new();

        private ContentItemEqualityComparer()
        {
        }

        public bool Equals(ContentItem? x, ContentItem? y) => string.Equals(x?.Path, y?.Path);

        public int GetHashCode(ContentItem obj) => obj.Path.GetHashCode();
    }
}
