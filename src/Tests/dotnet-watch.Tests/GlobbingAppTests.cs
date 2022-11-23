﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.NET.TestFramework;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class GlobbingAppTests
    {
        private const string AppName = "WatchGlobbingApp";
        private readonly TestAssetsManager _testAssetsManager;
        private readonly ITestOutputHelper _logger;

        public GlobbingAppTests(ITestOutputHelper logger)
        {
            _testAssetsManager = new TestAssetsManager(logger);
            _logger = logger;
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ChangeCompiledFile(bool usePollingWatcher)
        {
            var testAsset = _testAssetsManager.CopyTestAsset(AppName, identifier: usePollingWatcher.ToString())
               .WithSource()
               .Path;

            using var app = new WatchableApp(testAsset, _logger);

            app.UsePollingWatcher = usePollingWatcher;
            await app.StartWatcherAsync();

            await AssertCompiledAppDefinedTypes(app, expected: 2);

            var fileToChange = Path.Combine(app.SourceDirectory, "include", "Foo.cs");
            var programCs = File.ReadAllText(fileToChange);
            File.WriteAllText(fileToChange, programCs);

            await app.AssertFileChanged();
            await app.AssertRestarted();
            await AssertCompiledAppDefinedTypes(app, expected: 2);
        }

        [Fact]
        public async Task DeleteCompiledFile()
        {
            var testAsset = _testAssetsManager.CopyTestAsset(AppName)
               .WithSource()
               .Path;

            using var app = new WatchableApp(testAsset, _logger);

            await app.StartWatcherAsync();

            await AssertCompiledAppDefinedTypes(app, expected: 2);

            var fileToChange = Path.Combine(app.SourceDirectory, "include", "Foo.cs");
            File.Delete(fileToChange);

            await app.AssertRestarted();
            await AssertCompiledAppDefinedTypes(app, expected: 1);
        }

        [Fact]
        public async Task DeleteSourceFolder()
        {
            var testAsset = _testAssetsManager.CopyTestAsset(AppName)
               .WithSource()
               .Path;

            using var app = new WatchableApp(testAsset, _logger);

            await app.StartWatcherAsync();

            await AssertCompiledAppDefinedTypes(app, expected: 2);

            var folderToDelete = Path.Combine(app.SourceDirectory, "include");
            Directory.Delete(folderToDelete, recursive: true);

            await app.AssertRestarted();
            await AssertCompiledAppDefinedTypes(app, expected: 1);
        }

        [Fact]
        public async Task RenameCompiledFile()
        {
            var testAsset = _testAssetsManager.CopyTestAsset(AppName)
               .WithSource()
               .Path;

            using var app = new WatchableApp(testAsset, _logger);

            await app.StartWatcherAsync();

            var oldFile = Path.Combine(app.SourceDirectory, "include", "Foo.cs");
            var newFile = Path.Combine(app.SourceDirectory, "include", "Foo_new.cs");
            File.Move(oldFile, newFile);

            await app.AssertRestarted();
        }

        [Fact]
        public async Task ChangeExcludedFile()
        {
            var testAsset = _testAssetsManager.CopyTestAsset(AppName)
               .WithSource()
               .Path;

            using var app = new WatchableApp(testAsset, _logger);

            await app.StartWatcherAsync();

            var changedFile = Path.Combine(app.SourceDirectory, "exclude", "Baz.cs");
            File.WriteAllText(changedFile, "");

            var fileChanged = app.AssertFileChanged();
            var finished = await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(5)), fileChanged);
            Assert.NotSame(fileChanged, finished);
        }

        [Fact]
        public async Task ListsFiles()
        {
            var testAsset = _testAssetsManager.CopyTestAsset(AppName)
               .WithSource()
               .Path;

            using var app = new WatchableApp(testAsset, _logger);

            app.Prepare();
            app.Start(new[] { "--list" });
            var lines = await app.Process.GetAllOutputLinesAsync(CancellationToken.None);
            var files = lines.Where(l => !l.StartsWith("watch :"));

            AssertEx.EqualFileList(
                testAsset,
                new[]
                {
                    "Program.cs",
                    "include/Foo.cs",
                    "WatchGlobbingApp.csproj",
                },
                files);
        }

        private async Task AssertCompiledAppDefinedTypes(WatchableApp app, int expected)
        {
            var prefix = "Defined types = ";

            var line = await app.AssertOutputLineStartsWith(prefix);
            Assert.Equal(expected, int.Parse(line));
        }
    }
}
