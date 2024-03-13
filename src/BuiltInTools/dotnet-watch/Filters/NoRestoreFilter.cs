// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    internal sealed class NoRestoreFilter(DotNetWatchContext context)
    {
        private bool _canUseNoRestore;
        private string[]? _noRestoreArguments;

        public void Process(WatchState state)
        {
            Debug.Assert(!context.HotReloadEnabled);

            if (context.EnvironmentOptions.SuppressMSBuildIncrementalism)
            {
                return;
            }

            if (state.Iteration == 0)
            {
                var arguments = state.ProcessSpec.Arguments ?? Array.Empty<string>();
                _canUseNoRestore = CanUseNoRestore(arguments, context.Reporter);
                if (_canUseNoRestore)
                {
                    // Create run --no-restore <other args>
                    _noRestoreArguments = arguments.Take(1).Append("--no-restore").Concat(arguments.Skip(1)).ToArray();
                    context.Reporter.Verbose($"No restore arguments: {string.Join(" ", _noRestoreArguments)}");
                }
            }
            else if (_canUseNoRestore)
            {
                if (state.RequiresMSBuildRevaluation)
                {
                    context.Reporter.Verbose("Cannot use --no-restore since msbuild project files have changed.");
                }
                else
                {
                    context.Reporter.Verbose("Modifying command to use --no-restore");
                    state.ProcessSpec.Arguments = _noRestoreArguments;
                }
            }
        }

        private static bool CanUseNoRestore(IEnumerable<string> arguments, IReporter reporter)
        {
            // For some well-known dotnet commands, we can pass in the --no-restore switch to avoid unnecessary restores between iterations.
            // For now we'll support the "run" and "test" commands.
            if (arguments.Any(a => string.Equals(a, "--no-restore", StringComparison.Ordinal)))
            {
                // Did the user already configure a --no-restore?
                return false;
            }

            var dotnetCommand = arguments.FirstOrDefault();
            if (string.Equals(dotnetCommand, "run", StringComparison.Ordinal) || string.Equals(dotnetCommand, "test", StringComparison.Ordinal))
            {
                reporter.Verbose("Watch command can be configured to use --no-restore.");
                return true;
            }
            else
            {
                reporter.Verbose($"Watch command will not use --no-restore. Unsupport dotnet-command '{dotnetCommand}'.");
                return false;
            }
        }
    }
}
