// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Build.Graph;

namespace Microsoft.DotNet.Watcher.Tools
{
    internal sealed class BrowserRefreshFilter(DotNetWatchContext context) : IAsyncDisposable
    {
        // This needs to be in sync with the version BrowserRefreshMiddleware is compiled against.
        private static readonly Version s_minimumSupportedVersion = new(6, 0);

        public BrowserRefreshServer? Server { get; private set; }

        public async ValueTask ProcessAsync(WatchState state, CancellationToken cancellationToken)
        {
            if (context.EnvironmentOptions.SuppressBrowserRefresh)
            {
                return;
            }

            if (state.Iteration == 0)
            {
                if (context.ProjectGraph is null)
                {
                    context.Reporter.Verbose("Unable to determine if this project is a webapp.");
                    return;
                }

                if (!IsSupportedVersion(context.ProjectGraph))
                {
                    context.Reporter.Warn(
                        "Skipping configuring browser-refresh middleware since the target framework version is not supported." +
                        " For more information see 'https://aka.ms/dotnet/watch/unsupported-tfm'.");
                    return;
                }

                if (!IsWebApp(context.ProjectGraph))
                {
                    context.Reporter.Verbose("Skipping configuring browser-refresh middleware since this is not a webapp.");
                    return;
                }

                context.Reporter.Verbose("Configuring the app to use browser-refresh middleware.");

                Server = new BrowserRefreshServer(context.EnvironmentOptions, context.Reporter);
                state.BrowserRefreshServer = Server;
                var serverUrls = string.Join(',', await Server.StartAsync(cancellationToken));
                context.Reporter.Verbose($"Refresh server running at {serverUrls}.");
                state.ProcessSpec.EnvironmentVariables["ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT"] = serverUrls;
                state.ProcessSpec.EnvironmentVariables["ASPNETCORE_AUTO_RELOAD_WS_KEY"] = Server.ServerKey;

                var pathToMiddleware = Path.Combine(AppContext.BaseDirectory, "middleware", "Microsoft.AspNetCore.Watch.BrowserRefresh.dll");
                state.ProcessSpec.EnvironmentVariables.DotNetStartupHooks.Add(pathToMiddleware);
                state.ProcessSpec.EnvironmentVariables.AspNetCoreHostingStartupAssemblies.Add("Microsoft.AspNetCore.Watch.BrowserRefresh");
            }
            else if (Server != null)
            {
                // We've detected a change. Notify the browser.
                await Server.SendWaitMessageAsync(cancellationToken);
            }
        }

        private static bool IsSupportedVersion(ProjectGraph context)
        {
            if (context.GraphRoots.FirstOrDefault() is not { } projectNode)
            {
                return false;
            }
            if (projectNode.ProjectInstance.GetPropertyValue("_TargetFrameworkVersionWithoutV") is not string targetFrameworkVersion)
            {
                return false;
            }
            if(!Version.TryParse(targetFrameworkVersion, out var version))
            {
                return false;
            }

            return version >= s_minimumSupportedVersion;
        }

        private static bool IsWebApp(ProjectGraph projectGraph)
        {
            // We only want to enable browser refreshes if this is a WebApp (ASP.NET Core / Blazor app).
            return projectGraph.GraphRoots.FirstOrDefault() is { } projectNode &&
                projectNode.ProjectInstance.GetItems("ProjectCapability").Any(p => p.EvaluatedInclude is "AspNetCore" or "WebAssembly");
        }

        public async ValueTask DisposeAsync()
        {
            if (Server != null)
            {
                await Server.DisposeAsync();
            }
        }
    }
}
