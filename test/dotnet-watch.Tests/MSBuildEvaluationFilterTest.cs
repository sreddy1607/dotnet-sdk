// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Tools.Internal;
using Moq;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class MSBuildEvaluationFilterTest
    {
        private static readonly FileSet s_emptyFileSet = new(projectInfo: null!, Array.Empty<FileItem>());

        private readonly IFileSetFactory _fileSetFactory = Mock.Of<IFileSetFactory>(
            f => f.CreateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(s_emptyFileSet));

        [Fact]
        public async Task ProcessAsync_EvaluatesFileSetIfProjFileChanges()
        {
            var context = new DotNetWatchContext
            {
                HotReloadEnabled = false,
                Reporter = NullReporter.Singleton,
                LaunchSettingsProfile = new()
            };

            var filter = new MSBuildEvaluationFilter(context, _fileSetFactory);

            var state = new WatchState()
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec()
            };

            await filter.ProcessAsync(state, CancellationToken.None);

            state.Iteration++;
            state.ChangedFile = new FileItem { FilePath = "Test.csproj" };
            state.RequiresMSBuildRevaluation = false;

            await filter.ProcessAsync(state, CancellationToken.None);

            Assert.True(state.RequiresMSBuildRevaluation);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotEvaluateFileSetIfNonProjFileChanges()
        {
            var context = new DotNetWatchContext
            {
                HotReloadEnabled = false,
                Reporter = NullReporter.Singleton,
                LaunchSettingsProfile = new()
            };

            var filter = new MSBuildEvaluationFilter(context, _fileSetFactory);

            var state = new WatchState()
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec()
            };

            await filter.ProcessAsync(state, CancellationToken.None);

            state.Iteration++;
            state.ChangedFile = new FileItem { FilePath = "Controller.cs" };
            state.RequiresMSBuildRevaluation = false;

            await filter.ProcessAsync(state, CancellationToken.None);

            Assert.False(state.RequiresMSBuildRevaluation);
            Mock.Get(_fileSetFactory).Verify(v => v.CreateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task ProcessAsync_EvaluateFileSetOnEveryChangeIfOptimizationIsSuppressed()
        {
            var context = new DotNetWatchContext
            {
                HotReloadEnabled = false,
                SuppressMSBuildIncrementalism = true,
                Reporter = NullReporter.Singleton,
                LaunchSettingsProfile = new()
            };

            var filter = new MSBuildEvaluationFilter(context, _fileSetFactory);

            var state = new WatchState()
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec()
            };

            await filter.ProcessAsync(state, CancellationToken.None);

            state.Iteration++;
            state.ChangedFile = new FileItem { FilePath = "Controller.cs" };
            state.RequiresMSBuildRevaluation = false;

            await filter.ProcessAsync(state, CancellationToken.None);

            Assert.True(state.RequiresMSBuildRevaluation);
            Mock.Get(_fileSetFactory).Verify(v => v.CreateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessAsync_SetsEvaluationRequired_IfMSBuildFileChanges_ButIsNotChangedFile()
        {
            // There's a chance that the watcher does not correctly report edits to msbuild files on
            // concurrent edits. MSBuildEvaluationFilter uses timestamps to additionally track changes to these files.

            var fileSet = new FileSet(null, new[] { new FileItem { FilePath = "Controlller.cs" }, new FileItem { FilePath = "Proj.csproj" } });
            var fileSetFactory = Mock.Of<IFileSetFactory>(f => f.CreateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(fileSet));

            var context = new DotNetWatchContext
            {
                HotReloadEnabled = false,
                Reporter = NullReporter.Singleton,
                LaunchSettingsProfile = new()
            };

            var filter = new TestableMSBuildEvaluationFilter(context, fileSetFactory)
            {
                Timestamps =
                {
                    ["Controller.cs"] = new DateTime(1000),
                    ["Proj.csproj"] = new DateTime(1000),
                }
            };

            var state = new WatchState()
            {
                Iteration = 0,
                ProcessSpec = new ProcessSpec()
            };

            await filter.ProcessAsync(state, CancellationToken.None);
            state.RequiresMSBuildRevaluation = false;
            state.ChangedFile = new FileItem { FilePath = "Controller.cs" };
            state.Iteration++;
            filter.Timestamps["Proj.csproj"] = new DateTime(1007);

            await filter.ProcessAsync(state, CancellationToken.None);

            Assert.True(state.RequiresMSBuildRevaluation);
        }

        private class TestableMSBuildEvaluationFilter(DotNetWatchContext context, IFileSetFactory factory)
            : MSBuildEvaluationFilter(context, factory)
        {
            public Dictionary<string, DateTime> Timestamps { get; } = [];
            private protected override DateTime GetLastWriteTimeUtcSafely(string file) => Timestamps[file];
        }
    }
}
