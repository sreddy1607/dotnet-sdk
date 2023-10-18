// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.NET.TestFramework;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.NET.Build.Containers.UnitTests;

[CollectionDefinition("Daemon Tests")]
public class DaemonTestsCollection
{ 
}

[Collection("Daemon Tests")]
public class DockerDaemonTests : IDisposable
{
    private ITestOutputHelper _testOutput;
    private readonly TestLoggerFactory _loggerFactory;

    public DockerDaemonTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
        _loggerFactory = new TestLoggerFactory(testOutput);
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }

    [DockerDaemonAvailableFact]
    public async Task Can_detect_when_no_daemon_is_running()
    {
        ILogger logger = _loggerFactory.CreateLogger(nameof(Can_detect_when_no_daemon_is_running));
        // mimic no daemon running by setting the DOCKER_HOST to a nonexistent socket
        try
        {
            Environment.SetEnvironmentVariable("DOCKER_HOST", "tcp://123.123.123.123:12345");
            bool available = await new LocalDocker(logger).IsAvailableAsync(default).ConfigureAwait(false);
            Assert.False(available, "No daemon should be listening at that port");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOCKER_HOST", null);
        }
    }

    [DockerDaemonAvailableFact]
    public async Task Can_detect_when_daemon_is_running()
    {
        ILogger logger = _loggerFactory.CreateLogger(nameof(Can_detect_when_daemon_is_running));
        bool available = await new LocalDocker(logger).IsAvailableAsync(default).ConfigureAwait(false);
        Assert.True(available, "Should have found a working daemon");
    }
}
