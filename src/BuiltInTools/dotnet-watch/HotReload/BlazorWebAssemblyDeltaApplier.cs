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

        private static readonly TimeSpan VerifyDeltaTimeout = TimeSpan.FromSeconds(5);

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

            return await VerifyDeltaApplied(context, cancellationToken).WaitAsync(VerifyDeltaTimeout, cancellationToken);
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

        private async Task<bool> VerifyDeltaApplied(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            var _receiveBuffer = new byte[1];
            try
            {
                // We want to give the client some time to ACK the deltas being applied. VerifyDeltaApplied is limited by a
                // 5 second wait timeout enforced using a WaitAsync. However, WaitAsync only works reliably if the calling
                // function is async. If BrowserRefreshServer.ReceiveAsync finishes synchronously, the WaitAsync would
                // never have an opportunity to execute. Consequently, we'll give it some reasonable number of opportunities
                // to loop before we decide that applying deltas failed.
                for (var i = 0; i < 100; i++)
                {
                    var result = await context.BrowserRefreshServer.ReceiveAsync(_receiveBuffer, cancellationToken);
                    if (result is null)
                    {
                        // A null result indicates no clients are connected. No deltas could have been applied in this state.
                        _reporter.Verbose("No client is connected to ack deltas");
                        return false;
                    }

                    if (IsDeltaReceivedMessage(result.Value))
                    {
                        // 1 indicates success.
                        return _receiveBuffer[0] == 1;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _reporter.Verbose("Timed out while waiting to verify delta was applied.");
            }

            return false;

            bool IsDeltaReceivedMessage(ValueWebSocketReceiveResult result)
            {
                _reporter.Verbose($"Received {_receiveBuffer[0]} from browser in [Count: {result.Count}, MessageType: {result.MessageType}, EndOfMessage: {result.EndOfMessage}].");
                return result.Count == 1 // Should have received 1 byte on the socket for the acknowledgement
                    && result.MessageType is WebSocketMessageType.Binary
                    && result.EndOfMessage;
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
