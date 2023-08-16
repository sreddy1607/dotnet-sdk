﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Workloads.Workload.History;

namespace Microsoft.DotNet.Cli
{
    internal static class WorkloadHistoryCommandParser
    {
        private static readonly Command Command = ConstructCommand();

        public static Command GetCommand()
        {
            return Command;
        }

        private static Command ConstructCommand()
        {
            var command = new Command("history", LocalizableStrings.CommandDescription);

            command.SetHandler(parseResult => new WorkloadHistoryCommand(parseResult).Execute());

            return command;
        }
    }
}
