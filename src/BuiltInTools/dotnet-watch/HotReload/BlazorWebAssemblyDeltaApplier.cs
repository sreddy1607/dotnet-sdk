﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Watch.Api;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    internal class BlazorWebAssemblyDeltaApplier : IDeltaApplier
    {
        private readonly IReporter _reporter;
        private int _sequenceId;

        public BlazorWebAssemblyDeltaApplier(IReporter reporter)
        {
            _reporter = reporter;
        }

        public ValueTask InitializeAsync(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            // Configure the app for EnC
            context.ProcessSpec.EnvironmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug";
            return default;
        }

        public async ValueTask<bool> Apply(DotNetWatchContext context, string changedFile, ImmutableArray<WatchHotReloadService.Update> solutionUpdate, CancellationToken cancellationToken)
        {
            if (context.BrowserRefreshServer is null)
            {
                _reporter.Verbose("Unable to send deltas because the refresh server is unavailable.");
                return false;
            }

            var payload = new UpdatePayload
            {
                Deltas = solutionUpdate.Select(c => new UpdateDelta
                {
                    SequenceId = _sequenceId++,
                    ModuleId = c.ModuleId,
                    MetadataDelta = c.MetadataDelta.ToArray(),
                    ILDelta = c.ILDelta.ToArray(),
                }),
            };

            await context.BrowserRefreshServer.SendJsonSerlialized(payload, cancellationToken);

            return await VerifyDeltaApplied(context, cancellationToken);
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

        private async ValueTask<bool> VerifyDeltaApplied(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            var _receiveBuffer = new byte[1];
            try 
            {
                var result = await context.BrowserRefreshServer.ReceiveAsync(_receiveBuffer, cancellationToken);
                return IsDeltaApplied(result);
            }
            catch when (!cancellationToken.IsCancellationRequested)
            {
                _reporter.Verbose("Timed out while waiting to verify delta was applied.");
                return false;
            }
            
            bool IsDeltaApplied(ValueWebSocketReceiveResult? result)
            {
                return result.HasValue
                    && result.Value.Count == 1 // Should have received 1 byte on the socket for the acknowledgement
                    && result.Value.MessageType is WebSocketMessageType.Binary 
                    && result.Value.EndOfMessage
                    && _receiveBuffer[0] == 1;
            }
        }

        public void Dispose()
        {
            // Do nothing.
        }

        private readonly struct UpdatePayload
        {
            public string Type => "BlazorHotReloadDeltav1";
            public IEnumerable<UpdateDelta> Deltas { get; init; }
        }

        private readonly struct UpdateDelta
        {
            public int SequenceId { get; init; }
            public Guid ModuleId { get; init; }
            public byte[] MetadataDelta { get; init; }
            public byte[] ILDelta { get; init; }
        }

        public readonly struct HotReloadDiagnostics
        {
            public string Type => "HotReloadDiagnosticsv1";

            public IEnumerable<string> Diagnostics { get; init; }
        }
    }
}
