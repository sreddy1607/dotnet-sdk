﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal class UninstallCommand : BaseUninstallCommand
    {
        public UninstallCommand(
            NewCommand parentCommand,
            Func<ParseResult, ITemplateEngineHost> hostBuilder)
            : base(hostBuilder, "uninstall")
        {
            parentCommand.AddNoLegacyUsageValidators(this);
        }

        protected override async Task<NewCommandStatus> ExecuteAsync(
            UninstallCommandArgs args,
            IEngineEnvironmentSettings environmentSettings,
            TemplatePackageManager templatePackageManager,
            InvocationContext context)
        {
            NewCommandStatus status = await base.ExecuteAsync(args, environmentSettings, templatePackageManager, context).ConfigureAwait(false);
            await CheckTemplatesWithSubCommandName(args, templatePackageManager, context.GetCancellationToken()).ConfigureAwait(false);
            return status;
        }
    }
}
