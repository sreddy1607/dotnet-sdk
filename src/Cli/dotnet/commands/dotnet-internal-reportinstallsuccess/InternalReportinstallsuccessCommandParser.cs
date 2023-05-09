// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Cli
{
    internal static class InternalReportinstallsuccessCommandParser
    {
        public static readonly Argument<string> Argument = new Argument<string>("internal-reportinstallsuccess-arg");

        private static readonly Command Command = ConstructCommand();

        public static Command GetCommand()
        {
            return Command;
        }

        private static Command ConstructCommand()
        {
            var command = new Command("internal-reportinstallsuccess")
            {
                IsHidden = true
            };

            command.AddArgument(Argument);

            command.SetHandler(InternalReportinstallsuccess.Run);

            return command;
        }
    }
}
