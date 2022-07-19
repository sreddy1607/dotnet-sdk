﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.NET.TestFramework.ProjectConstruction;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Tests
{
    public class GivenThatWeWantToUseBinaryFormatter : SdkTest
    {
        public GivenThatWeWantToUseBinaryFormatter(ITestOutputHelper log) : base(log)
        {
        }

        private const string SourceWithPragmaSuppressions = @"
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable SYSLIB0011

namespace BinaryFormatterTests
{
    public class TestClass
    {
        public static void Main(string[] args)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            var deserializedObj = formatter.Deserialize(stream);
        }
    }
}";

        private const string SourceWithoutPragmaSuppressions = @"
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BinaryFormatterTests
{
    public class TestClass
    {
        public static void Main(string[] args)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            var deserializedObj = formatter.Deserialize(stream);
        }
    }
}";

        [Theory]
        [InlineData("netcoreapp3.1")]
        [InlineData("netstandard2.0")]
        [InlineData("net472")]
        public void It_does_not_warn_when_targeting_downlevel_frameworks(string targetFramework)
        {
            var testProject = new TestProject()
            {
                Name = "BinaryFormatterTests",
                TargetFrameworks = targetFramework,
                IsExe = true
            };

            testProject.SourceFiles.Add("TestClass.cs", SourceWithoutPragmaSuppressions);

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var buildCommand = new BuildCommand(testAsset, "BinaryFormatterTests");

            buildCommand
                .Execute()
                .Should()
                .Pass()
                .And
                .NotHaveStdOutContaining("SYSLIB0011");
        }

        [Theory]
        [InlineData("netcoreapp3.1")]
        [InlineData("netstandard2.0")]
        [InlineData("net472")]
        [InlineData("net5.0")]
        [InlineData("net6.0")]
        [InlineData("net7.0")]
        public void It_does_not_warn_on_any_framework_when_using_pragma_suppressions(string targetFramework)
        {
            var testProject = new TestProject()
            {
                Name = "BinaryFormatterTests",
                TargetFrameworks = targetFramework,
                IsExe = true
            };

            testProject.SourceFiles.Add("TestClass.cs", SourceWithPragmaSuppressions);

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var buildCommand = new BuildCommand(testAsset, "BinaryFormatterTests");

            buildCommand
                .Execute()
                .Should()
                .Pass()
                .And
                .NotHaveStdOutContaining("SYSLIB0011");
        }

        [Theory]
        [InlineData("net5.0")]
        [InlineData("net6.0")]
        public void It_warns_when_targeting_certain_frameworks_and_not_using_pragma_suppressions(string targetFramework)
        {
            var testProject = new TestProject()
            {
                Name = "BinaryFormatterTests",
                TargetFrameworks = targetFramework,
                IsExe = true
            };

            testProject.SourceFiles.Add("TestClass.cs", SourceWithoutPragmaSuppressions);

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var buildCommand = new BuildCommand(testAsset, "BinaryFormatterTests");

            buildCommand
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("SYSLIB0011");
        }

        [Theory]
        [InlineData("net7.0")]
        public void It_errors_when_targeting_certain_frameworks_and_not_using_pragma_suppressions(string targetFramework)
        {
            var testProject = new TestProject()
            {
                Name = "BinaryFormatterTests",
                TargetFrameworks = targetFramework,
                IsExe = true
            };

            testProject.SourceFiles.Add("TestClass.cs", SourceWithoutPragmaSuppressions);

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var buildCommand = new BuildCommand(testAsset, "BinaryFormatterTests");

            buildCommand
                .Execute()
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("SYSLIB0011");
        }

        [Theory]
        [InlineData("net7.0")]
        public void It_allows_downgrading_errors_to_warnings_via_project_config(string targetFramework)
        {
            var testProject = new TestProject()
            {
                Name = "BinaryFormatterTests",
                TargetFrameworks = targetFramework,
                IsExe = true
            };

            testProject.SourceFiles.Add("TestClass.cs", SourceWithoutPragmaSuppressions);
            testProject.AdditionalProperties["EnableUnsafeBinaryFormatterSerialization"] = "true";

            var testAsset = _testAssetsManager.CreateTestProject(testProject);
            var buildCommand = new BuildCommand(testAsset, "BinaryFormatterTests");

            buildCommand
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("SYSLIB0011");
        }
    }
}
