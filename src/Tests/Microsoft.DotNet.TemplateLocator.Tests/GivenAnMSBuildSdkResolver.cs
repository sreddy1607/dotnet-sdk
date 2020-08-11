// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using NuGet.Common;
using NuGet.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.TemplateLocator.Tests
{
    public class GivenAnTemplateLocator : SdkTest
    {

        public GivenAnTemplateLocator(ITestOutputHelper logger) : base(logger)
        {
        }

        [Fact]
        public void ItShouldReturnListOfTemplates()
        {
            var resolver = new TemplateLocator();
            var stage2Dotnet = new DirectoryInfo(TestContext.Current.ToolsetUnderTest.DotNetHostPath);
            var stage2Templates = stage2Dotnet.Parent.GetDirectories("templates")[0].EnumerateDirectories().First();
            resolver.SetDotnetSdkTemplatesLocation(stage2Templates);

            var result = resolver.GetDotnetSdkTemplatePackages("any");

            result.Should().NotBeEmpty();
        }
    }
}
