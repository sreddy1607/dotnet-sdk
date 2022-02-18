﻿// Copyright(c) .NET Foundation and contributors.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli;
using System.CommandLine.Parsing;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.DotNet.Tools.Test;

namespace Microsoft.DotNet.Tools.VSTest
{
    public class VSTestCommand
    {
        public static int Run(ParseResult parseResult)
        {
            parseResult.HandleDebugSwitch();

            // We use also current process id for the correlation id for possible future usage in case we need to know the parent process
            // from the VSTest side.
            string testSessionCorrelationId = $"{Environment.ProcessId}_{Guid.NewGuid()}";

            var args = new List<string>();
            args.AddRange(GetArgs(parseResult));

            if (FeatureFlag.Default.IsEnabled(FeatureFlag.ARTIFACTS_POSTPROCESSING))
            {
                // Add artifacts processing mode and test session id for the artifact post-processing
                args.Add("--artifactsProcessingMode-collect");
                args.Add($"--testSessionCorrelationId:{testSessionCorrelationId}");
            }

            VSTestForwardingApp vsTestforwardingApp = new(args);

            int exitCode = vsTestforwardingApp.Execute();

            // We run post processing also if execution is failed for possible partial successful result to post process.
            TestCommand.RunArtifactPostProcessingIfNeeded(testSessionCorrelationId, parseResult, FeatureFlag.Default);

            return exitCode;
        }

        private static string[] GetArgs(ParseResult parseResult)
        {
            IEnumerable<string> args = parseResult.GetArguments();

            if (parseResult.HasOption(CommonOptions.TestLoggerOption))
            {
                // System command line might have mutated the options, reformat test logger option so vstest recognizes it
                string loggerValue = parseResult.GetValueForOption(CommonOptions.TestLoggerOption);
                args = args.Where(a => !a.Equals(loggerValue) && !CommonOptions.TestLoggerOption.Aliases.Contains(a));
                args = args.Prepend($"{CommonOptions.TestLoggerOption.Aliases.First()}:{loggerValue}");
            }

            return args.ToArray();
        }
    }
}
