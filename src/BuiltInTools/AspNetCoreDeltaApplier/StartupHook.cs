﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Tools;

internal sealed class StartupHook
{
    private static readonly bool LogDeltaClientMessages = Environment.GetEnvironmentVariable("HOTRELOAD_LOG_DELTA_CLIENT_MESSAGES") == "1";

    public static void Initialize()
    {
        var receiveDeltaNotifications = GetAssembliesReceivingDeltas();

        Task.Run(async () =>
        {
            try
            {
                await ReceiveDeltas(receiveDeltaNotifications);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        });
    }

    private static List<Action> GetAssembliesReceivingDeltas()
    {
        var receipients = new List<Action>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var customAttributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
            var deltaReceiverAttribute = customAttributes.FirstOrDefault(f => f.Key == "ReceiveHotReloadDeltaNotification");
            if (deltaReceiverAttribute is AssemblyMetadataAttribute { Value: not null })
            {
                var type = assembly.GetType(deltaReceiverAttribute.Value, throwOnError: false);
                if (type is null)
                {
                    Log($"Could not find delta receiver type {deltaReceiverAttribute.Value} in assembly {assembly}.");
                    continue;
                }

                if (type.GetMethod("DeltaApplied", BindingFlags.Public | BindingFlags.Static) is MethodInfo methodInfo)
                {
                    var action = methodInfo.CreateDelegate<Action>();
                    Action safeAction = () =>
                    {
                        try { action(); } catch (Exception ex) { Log(ex.ToString()); }
                    };

                    receipients.Add(safeAction);
                }
                else
                {
                    Log($"Could not find method 'DeltaApplied' on type {type}.");
                }
            }
        }

        return receipients;
    }

    public static async Task ReceiveDeltas(List<Action> receiveDeltaNotifications)
    {
        Log("Attempting to receive deltas.");

        using var pipeClient = new NamedPipeClientStream(".", "netcore-hot-reload", PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);
        try
        {
            await pipeClient.ConnectAsync(5000);
            Log("Connected.");
        }
        catch (TimeoutException)
        {
            Log("Unable to connect to hot-reload server.");
        }

        while (pipeClient.IsConnected)
        {
            var bytes = new byte[4096];
            var numBytes = await pipeClient.ReadAsync(bytes);

            var update = JsonSerializer.Deserialize<UpdatePayload>(bytes.AsSpan(0, numBytes));
            Log("Attempting to apply deltas.");

            try
            {
                foreach (var item in update.Deltas)
                {
                    var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.Modules.FirstOrDefault() is Module m && m.ModuleVersionId == item.ModuleId);
                    if (assembly is not null)
                    {
                        System.Reflection.Metadata.AssemblyExtensions.ApplyUpdate(assembly, item.MetadataDelta, item.ILDelta, ReadOnlySpan<byte>.Empty);
                    }
                }

                // We want to base this off of mvids, but we'll figure that out eventually.
                var applyResult = update.ChangedFile is string changedFile && changedFile.EndsWith(".razor", StringComparison.Ordinal) ?
                    ApplyResult.Success :
                    ApplyResult.Success_RefreshBrowser;
                pipeClient.WriteByte((byte)applyResult);

                receiveDeltaNotifications.ForEach(r => r.Invoke());

                Log("Deltas applied.");
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }
        Log("Stopped received delta updates. Server is no longer connected.");
    }

    private static void Log(string message)
    {
        if (LogDeltaClientMessages)
        {
            Console.WriteLine(message);
        }
    }
}

