﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Commands;
using NuGet.Packaging;
using Xunit;
using Xunit.Abstractions;
using Microsoft.NET.TestFramework.Assertions;
using System;
using Reporting;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using PerformanceTestsResultGenerator;

namespace Microsoft.NET.ToolPack.Tests
{
    public class GivenPerformanceTestsResultGenerator : SdkTest
    {
        public GivenPerformanceTestsResultGenerator(ITestOutputHelper log) : base(log)
        {
        }

        [Fact]
        public void Given_stream_It_can_convert_xmls_to_TestObject()
        {
            StreamReader reader = new StreamReader(Path.Combine("PerformanceResultSample", "20190618003138-WPF hello world.xml"));
            var test = XunitPerformanceResultConverter.GenerateTestsFromXml(reader);
            AssertTestsObjectEqualToExpectedJson(test, File.ReadAllText(Path.Combine("PerformanceResultSample", "expectTestResult.json")));
        }

        [Fact]
        public void Given_perfWorkingDirectory_it_can_combine_xml_results()
        {
            List<Test> tests = XunitPerformanceResultConverter.BatchGenerateTests(new DirectoryInfo("PerformanceResultSample"));
            AssertTestsObjectEqualToExpectedJson(tests, File.ReadAllText(Path.Combine("PerformanceResultSample", "expectBatchTestResult.json")));
        }

        [Fact]
        public void It_can_get_commit_timestamp()
        {
            Reporting.Reporter.GetCommitTimestamp("29bf0a82d9d9e20b6da067b3bf2cb05bf0504e47", TestContext.GetRepoRoot()).Should().Be(DateTime.Parse("2019-04-23T13:59:53+00:00"));
        }

        private static void AssertTestsObjectEqualToExpectedJson(List<Test> testObject, string expectedJson)
        {

            var settings = new JsonSerializerSettings();
            var resolver = new DefaultContractResolver();
            resolver.NamingStrategy = new CamelCaseNamingStrategy() { ProcessDictionaryKeys = false };
            settings.ContractResolver = resolver;

            string generatedJson = JsonConvert.SerializeObject(testObject, Formatting.Indented, settings);
            var generatedJObject = JsonConvert.DeserializeObject<JArray>(generatedJson);
            var expectedJObject = JsonConvert.DeserializeObject<JArray>(expectedJson);

            JToken.DeepEquals(generatedJObject, expectedJObject).
                Should()
                .BeTrue();
        }
    }
}
