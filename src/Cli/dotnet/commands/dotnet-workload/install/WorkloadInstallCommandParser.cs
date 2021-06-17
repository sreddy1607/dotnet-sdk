// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using LocalizableStrings = Microsoft.DotNet.Workloads.Workload.Install.LocalizableStrings;

namespace Microsoft.DotNet.Cli
{
    internal static class WorkloadInstallCommandParser
    {
        public static readonly Argument WorkloadIdArgument =
            new Argument<IEnumerable<string>>(LocalizableStrings.WorkloadIdArgumentName)
            {
                Arity = ArgumentArity.OneOrMore, Description = LocalizableStrings.WorkloadIdArgumentDescription
            };

        public static readonly Option ConfigOption =
            new Option<string>("--configfile", LocalizableStrings.ConfigFileOptionDescription)
            {
                ArgumentHelpName = LocalizableStrings.ConfigFileOptionName
            };

        public static readonly Option AddSourceOption =
            new Option<string[]>("--add-source", LocalizableStrings.AddSourceOptionDescription)
            {
                ArgumentHelpName = LocalizableStrings.AddSourceOptionName
            }.AllowSingleArgPerToken();

        public static readonly Option PrintDownloadLinkOnlyOption =
            new Option<bool>("--print-download-link-only", LocalizableStrings.PrintDownloadLinkOnlyDescription)
            {
                IsHidden = true
            };

        public static readonly Option VersionOption =
            new Option<string>("--sdk-version", LocalizableStrings.VersionOptionDescription)
            {
                ArgumentHelpName = LocalizableStrings.VersionOptionName
            };

        public static readonly Option IncludePreviewOption =
            new Option<bool>("--include-previews", LocalizableStrings.IncludePreviewOptionDescription);

        public static readonly Option FromCacheOption = new Option<string>("--from-cache", LocalizableStrings.FromCacheOptionDescription);

        public static readonly Option DownloadToCacheOption = new Option<string>("--download-to-cache", LocalizableStrings.DownloadToCacheOptionDescription);

        public static readonly Option SkipManifestUpdateOption = new Option<bool>("--skip-manifest-update", LocalizableStrings.SkipManifestUpdateOptionDescription);

        public static readonly Option TempDirOption = new Option<string>("--temp-dir", LocalizableStrings.TempDirOptionDescription);

        public static readonly Option VerbosityOption = CommonOptions.VerbosityOption();

        public static Command GetCommand()
        {
            var command = new Command("install", LocalizableStrings.CommandDescription);

            command.AddArgument(WorkloadIdArgument);
            command.AddOption(VersionOption);
            command.AddOption(ConfigOption);
            command.AddOption(AddSourceOption);
            command.AddOption(SkipManifestUpdateOption);
            command.AddOption(PrintDownloadLinkOnlyOption);
            command.AddOption(FromCacheOption);
            command.AddOption(DownloadToCacheOption);
            command.AddOption(IncludePreviewOption);
            command.AddOption(TempDirOption);
            command.AddOption(WorkloadCommandNuGetRestoreActionConfigOptions.DisableParallelOption);
            command.AddOption(WorkloadCommandNuGetRestoreActionConfigOptions.IgnoreFailedSourcesOption);
            command.AddOption(WorkloadCommandNuGetRestoreActionConfigOptions.NoCacheOption);
            command.AddOption(WorkloadCommandNuGetRestoreActionConfigOptions.InteractiveRestoreOption);
            command.AddOption(VerbosityOption);

            return command;
        }
    }
}
