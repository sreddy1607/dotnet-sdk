// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Security;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Internal
{
    internal sealed class PollingFileWatcher : IFileSystemWatcher
    {
        // The minimum interval to rerun the scan
        private static readonly TimeSpan s_minRunInternal = TimeSpan.FromSeconds(.5);

        private readonly DirectoryInfo _watchedDirectory;
        private readonly Thread _pollingThread;

        private bool _raiseEvents;
        private volatile bool _disposed;

        public event EventHandler<(string filePath, bool newFile)>? OnFileChange;

        public PollingFileWatcher(string watchedDirectory)
        {
            Ensure.NotNullOrEmpty(watchedDirectory, nameof(watchedDirectory));

            _watchedDirectory = new DirectoryInfo(watchedDirectory);

            _pollingThread = new Thread(new ThreadStart(PollingLoop))
            {
                IsBackground = true,
                Name = nameof(PollingFileWatcher)
            };

            _pollingThread.Start();
        }

        public event EventHandler<Exception>? OnError { add { } remove { } }

        public string BasePath
            => _watchedDirectory.FullName;

        public bool EnableRaisingEvents
        {
            get => _raiseEvents;
            set
            {
                EnsureNotDisposed();
                _raiseEvents = value;
            }
        }

        private void PollingLoop()
        {
            var currentSnapshot = CreateDirectorySnapshot(_watchedDirectory);
            var changes = new HashSet<string>();

            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            while (!_disposed)
            {
                if (stopwatch.Elapsed < s_minRunInternal)
                {
                    // Don't run too often
                    // The min wait time here can be double
                    // the value of the variable (FYI)
                    Thread.Sleep(s_minRunInternal);
                }

                stopwatch.Reset();

                if (!_raiseEvents)
                {
                    continue;
                }

                var newSnapshot = CreateDirectorySnapshot(_watchedDirectory);
                RecordChanges(changes, currentSnapshot, newSnapshot);

                NotifyChanges(changes);
                changes.Clear();

                currentSnapshot = newSnapshot;
            }

            stopwatch.Stop();
        }

        private void RecordChanges(
            HashSet<string> changes,
            ImmutableDictionary<string, FileSystemInfo> oldSnapshot,
            ImmutableDictionary<string, FileSystemInfo> newSnapshot)
        {
            foreach (var (fullPath, newInfo) in newSnapshot)
            {
                if (oldSnapshot.TryGetValue(fullPath, out var oldInfo))
                {
                    try
                    {
                        if (oldInfo.LastWriteTimeUtc != newInfo.LastWriteTimeUtc)
                        {
                            // File changed
                            RecordChange(newInfo);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // File deleted
                        RecordChange(oldInfo);
                    }
                }
                else
                {
                    // File added:
                    RecordChange(newInfo);
                }
            }

            // deleted files:
            foreach (var (fullPath, oldInfo) in oldSnapshot)
            {
                if (newSnapshot.ContainsKey(fullPath))
                {
                    RecordChange(oldInfo);
                }
            }

            void RecordChange(FileSystemInfo info)
            {
                if (info.FullName == _watchedDirectory.FullName)
                {
                    return;
                }

                if (!changes.Add(info.FullName))
                {
                    return;
                }

                FileSystemInfo? parentInfo;
                try
                {
                    parentInfo = info switch
                    {
                        FileInfo { Directory: { } containingDirectory } => containingDirectory,
                        DirectoryInfo { Parent: { } parentDirectory } => parentDirectory,
                        _ => null
                    };
                }
                catch
                {
                    parentInfo = null;
                }

                if (parentInfo != null)
                {
                    RecordChange(parentInfo);
                }
            }
        }

        private static ImmutableDictionary<string, FileSystemInfo> CreateDirectorySnapshot(DirectoryInfo watchedDirectory)
        {
            var snapshot = ImmutableDictionary.CreateBuilder<string, FileSystemInfo>();

            try
            {
                if (watchedDirectory.Exists)
                {
                    foreach (var info in watchedDirectory.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        // initialize last write timestamp:
                        try
                        {
                            _ = info.LastWriteTimeUtc;

                            // the enumerator does not guarantee unique paths
                            snapshot.TryAdd(info.FullName, info);
                        }
                        catch (IOException)
                        {
                            // entry doesn't exist anymore
                        }
                    }
                }
            }
            catch (Exception e) when (e is DirectoryNotFoundException or SecurityException or PlatformNotSupportedException)
            {
            }

            return snapshot.ToImmutable();
        }

        private void NotifyChanges(IEnumerable<string> fullPaths)
        {
            foreach (var fullPath in fullPaths)
            {
                if (_disposed || !_raiseEvents)
                {
                    break;
                }

                OnFileChange?.Invoke(this, (fullPath, newFile: false));
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PollingFileWatcher));
            }
        }

        public void Dispose()
        {
            EnableRaisingEvents = false;
            _disposed = true;
        }
    }
}
