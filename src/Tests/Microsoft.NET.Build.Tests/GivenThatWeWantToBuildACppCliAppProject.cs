﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using Microsoft.NET.Build.Tasks;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToBuildACppCliAppProject : SdkTest
    {
        public GivenThatWeWantToBuildACppCliAppProject(ITestOutputHelper log) : base(log)
        {
        }

        [FullMSBuildOnlyFact]
        public void It_should_fail_with_error_message()
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("NETCoreCppClApp")
                .WithSource();

            new BuildCommand(Log, Path.Combine(testAsset.TestRoot, "NETCoreCppCliTest.sln"))
                .Execute("/restore")
                .Should()
                .Fail()
                .And.HaveStdOutContaining(Strings.NoSupportCppExeDotnetCore);
        }
    }
}
