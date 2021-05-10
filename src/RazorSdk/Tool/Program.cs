﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.Razor.Tool
{
    internal static class Program
    {
        static Program()
        {
            // To minimize the size of the SDK, we resolve all Rosyln-related assemblies
            // from the `Roslyn/bincore` location in the SDK. The `RegisterAssemblyResolutionEvents`
            // method registers the event that will handle loading the assemblies from the correct
            // path. Note: since assembly resolution starts immediately when the `Main` method is
            // invoked, so we register the event listener here to ensure they are registered before
            // we `Main` is invoked.
            RegisterAssemblyResolutionEvents();
        }
        public static int Main(string[] args)
        {
            DebugMode.HandleDebugSwitch(ref args);

            var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { cancel.Cancel(); };

            var outputWriter = new StringWriter();
            var errorWriter = new StringWriter();

            // Prevent shadow copying.
            var loader = new DefaultExtensionAssemblyLoader(baseDirectory: null);
            var checker = new DefaultExtensionDependencyChecker(loader, outputWriter, errorWriter);

            var application = new Application(
                cancel.Token,
                loader,
                checker,
                (path, properties) => MetadataReference.CreateFromFile(path, properties),
                outputWriter,
                errorWriter);

            var result = application.Execute(args);

            var output = outputWriter.ToString();
            var error = errorWriter.ToString();

            outputWriter.Dispose();
            errorWriter.Dispose();

            Console.Write(output);
            Console.Error.Write(error);

            // This will no-op if server logging is not enabled.
            ServerLogger.Log(output);
            ServerLogger.Log(error);

            return result;
        }

        private static void RegisterAssemblyResolutionEvents()
        {
            var roslynPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Roslyn", "bincore");

            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
            {
                if (assembly.Name is "Microsoft.CodeAnalysis" or "Microsoft.CodeAnalysis.CSharp")
                {
                    return context.LoadFromAssemblyPath(Path.Combine(roslynPath, assembly.Name + ".dll"));
                }
                return null;
            };
        }
    }
}
