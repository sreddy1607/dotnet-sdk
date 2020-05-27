﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToBuildAWindowsRuntimeComponent : SdkTest
    {
        public GivenThatWeWantToBuildAWindowsRuntimeComponent(ITestOutputHelper log) : base(log)
        {
        }

        [Fact]
        public void It_fails_from_producing_winmds_for_net5_0()
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("WindowsRuntimeComponent")
                .WithSource();

            var buildCommand = new BuildCommand(Log, testAsset.TestRoot);
            buildCommand
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("NETSDK1131: ");
        }

        [Fact]
        public void It_fails_from_referencing_winmds_for_net5_0()
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("WinMDClasslibrary")
                .WithSource();


            var buildCommand = new BuildCommand(Log, testAsset.TestRoot);
            buildCommand
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("NETSDK1130: ");
        }

        [Theory]
        [InlineData("netcoreapp3.1")]
        [InlineData("net48")]
        public void It_successfully_builds_when_referencing_winmds(string targetFramework)
        {
            var testAsset = _testAssetsManager
                .CopyTestAsset("WinMDClasslibrary")
                .WithSource()
                .WithProjectChanges(project =>
                {
                    var ns = project.Root.Name.Namespace;
                    var propertyGroup = project.Root.Elements(ns + "PropertyGroup").First();
                    propertyGroup.Element("TargetFramework").ReplaceWith(new XElement("TargetFramework", targetFramework));
                });


            var buildCommand = new BuildCommand(Log, testAsset.TestRoot);
            buildCommand
                .Execute()
                .Should()
                .Pass();
        }
    }
}
