﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.ConvertTypeOfToNameOf;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    internal sealed class HotReloadFileSetWatcher(IReadOnlyDictionary<string, FileItem> fileSet, DateTime buildCompletionTime, IReporter reporter) : IDisposable
    {
        private static readonly TimeSpan s_debounceInterval = TimeSpan.FromMilliseconds(50);

        private readonly FileWatcher _fileWatcher = new(fileSet, reporter);
        private readonly object _changedFilesLock = new();
        private readonly ConcurrentDictionary<string, FileItem> _changedFiles = new(StringComparer.Ordinal);

        private TaskCompletionSource<FileItem[]?>? _tcs;
        private bool _initialized;
        private bool _disposed;

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            _fileWatcher.StartWatching();
            _fileWatcher.OnFileChange += FileChangedCallback;

            reporter.Report(MessageDescriptor.WaitingForChanges);

            Task.Factory.StartNew(async () =>
            {
                // Debounce / polling loop
                while (!_disposed)
                {
                    await Task.Delay(s_debounceInterval);
                    if (_changedFiles.IsEmpty)
                    {
                        continue;
                    }

                    var tcs = Interlocked.Exchange(ref _tcs, null!);
                    if (tcs is null)
                    {
                        continue;
                    }

                    FileItem[] changedFiles;
                    lock (_changedFilesLock)
                    {
                        changedFiles = _changedFiles.Values.ToArray();
                        _changedFiles.Clear();
                    }

                    if (changedFiles is [])
                    {
                        continue;
                    }

                    tcs.TrySetResult(changedFiles);
                }

            }, default, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            void FileChangedCallback(string path, bool newFile)
            {
                try
                {
                    // Do not report changes to files that happened during build:
                    if (Math.Max(File.GetCreationTimeUtc(path).Ticks, File.GetLastWriteTimeUtc(path).Ticks) < buildCompletionTime.Ticks)
                    {
                        reporter.Verbose($"Ignoring file updated during build: '{path}'.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    reporter.Verbose($"Ignoring file '{path}' due to access error: {e.Message}.");
                    return;
                }

                if (newFile)
                {
                    lock (_changedFilesLock)
                    {
                        _changedFiles.TryAdd(path, new FileItem { FilePath = path, IsNewFile = newFile });
                    }
                }
                else if (fileSet.TryGetValue(path, out var fileItem))
                {
                    lock (_changedFilesLock)
                    {
                        _changedFiles.TryAdd(path, fileItem);
                    }
                }
            }
        }

        public Task<FileItem[]?> GetChangedFilesAsync(CancellationToken cancellationToken, bool forceWaitForNewUpdate = false)
        {
            EnsureInitialized();

            var tcs = _tcs;
            if (!forceWaitForNewUpdate && tcs is not null)
            {
                return tcs.Task;
            }

            _tcs = tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.TrySetResult(null));
            return tcs.Task;
        }

        public void Dispose()
        {
            _disposed = true;
            _fileWatcher.Dispose();
        }
    }
}
