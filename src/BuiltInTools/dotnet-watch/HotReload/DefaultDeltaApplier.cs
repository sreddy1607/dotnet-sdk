﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Watch.Api;
using Microsoft.Extensions.HotReload;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    internal class DefaultDeltaApplier : IDeltaApplier
    {
        private static readonly string _namedPipeName = Guid.NewGuid().ToString();
        private readonly IReporter _reporter;
        private Task<ImmutableArray<string>>? _capabilitiesTask;
        private NamedPipeServerStream? _pipe;

        public DefaultDeltaApplier(IReporter reporter)
        {
            _reporter = reporter;
        }

        internal bool SuppressNamedPipeForTests { get; set; }

        public ValueTask InitializeAsync(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            if (!SuppressNamedPipeForTests)
            {
                _pipe = new NamedPipeServerStream(_namedPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                _capabilitiesTask = Task.Run(async () =>
                {
                    _reporter.Verbose($"Connecting to the application.");

                    await _pipe.WaitForConnectionAsync(cancellationToken);

                    // When the client connects, the first payload it sends is the initialization payload which includes the apply capabilities.

                    var capabilities = ClientInitializationPayload.Read(_pipe).Capabilities;
                    return capabilities.Split(' ').ToImmutableArray();
                });
            }

            if (context.Iteration == 0)
            {
                var deltaApplier = Path.Combine(AppContext.BaseDirectory, "hotreload", "Microsoft.Extensions.DotNetDeltaApplier.dll");
                context.ProcessSpec.EnvironmentVariables.DotNetStartupHooks.Add(deltaApplier);

                // Configure the app for EnC
                context.ProcessSpec.EnvironmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
                context.ProcessSpec.EnvironmentVariables["DOTNET_HOTRELOAD_NAMEDPIPE_NAME"] = _namedPipeName;
            }
            return default;
        }

        public Task<ImmutableArray<string>> GetApplyUpdateCapabilitiesAsync(DotNetWatchContext context, CancellationToken cancellationToken)
            => _capabilitiesTask ?? Task.FromResult(ImmutableArray<string>.Empty);

        public async ValueTask<bool> Apply(DotNetWatchContext context, ImmutableArray<WatchHotReloadService.Update> solutionUpdate, CancellationToken cancellationToken)
        {
            if (_capabilitiesTask is null || !_capabilitiesTask.IsCompletedSuccessfully || _pipe is null || !_pipe.IsConnected)
            {
                // The client isn't listening
                _reporter.Verbose("No client connected to receive delta updates.");
                return false;
            }

            var payload = new UpdatePayload
            {
                Deltas = ImmutableArray.CreateRange(solutionUpdate, c => new UpdateDelta
                {
                    ModuleId = c.ModuleId,
                    ILDelta = c.ILDelta.ToArray(),
                    MetadataDelta = c.MetadataDelta.ToArray(),
                    UpdatedTypes = c.UpdatedTypes.ToArray(),
                }),
            };

            await payload.WriteAsync(_pipe, cancellationToken);
            await _pipe.FlushAsync(cancellationToken);

            var result = ApplyResult.Failed;
            var bytes = ArrayPool<byte>.Shared.Rent(1);
            try
            {
                var numBytes = await _pipe.ReadAsync(bytes, cancellationToken);

                if (numBytes == 1)
                {
                    result = (ApplyResult)bytes[0];
                }
            }
            catch (Exception ex)
            {
                // Log it, but we'll treat this as a failed apply.
                _reporter.Verbose(ex.Message);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }

            if (result == ApplyResult.Failed)
            {
                return false;
            }

            if (context.BrowserRefreshServer is not null)
            {
                // BrowserRefreshServer will be null in non web projects or if we failed to establish a websocket connection
                await context.BrowserRefreshServer.SendJsonSerlialized(new AspNetCoreHotReloadApplied(), cancellationToken);
            }

            return true;
        }

        public async ValueTask ReportDiagnosticsAsync(DotNetWatchContext context, IEnumerable<string> diagnostics, CancellationToken cancellationToken)
        {
            if (context.BrowserRefreshServer != null)
            {
                var message = new HotReloadDiagnostics
                {
                    Diagnostics = diagnostics
                };

                await context.BrowserRefreshServer.SendJsonSerlialized(message, cancellationToken);
            }
        }

        public void Dispose()
        {
            _pipe?.Dispose();
        }

        public readonly struct HotReloadDiagnostics
        {
            public string Type => "HotReloadDiagnosticsv1";

            public IEnumerable<string> Diagnostics { get; init; }
        }

        public readonly struct AspNetCoreHotReloadApplied
        {
            public string Type => "AspNetCoreHotReloadApplied";
        }
    }
}
