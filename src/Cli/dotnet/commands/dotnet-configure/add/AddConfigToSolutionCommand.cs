﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.Configure.Add
{
    internal class AddConfigToSolutionCommand : CommandBase
    {
        private readonly string _fileOrDirectory;
        private readonly string _configName;
        private readonly string _platformName;
        private readonly string _copyFrom;
        private readonly string _updateProj;

        public AddConfigToSolutionCommand(
            ParseResult parseResult) : base(parseResult)
        {
            _fileOrDirectory = parseResult.GetValue(ConfigureCommandParser.ConfigureArgument);
            _configName = parseResult.GetValue(ConfigureAddParser.ConfigName);
            _platformName = parseResult.GetValue(ConfigureAddParser.PlatformName);
            _copyFrom = parseResult.GetValue(ConfigureAddParser.CopyFromConfig);
            _updateProj = parseResult.GetValue(ConfigureAddParser.UpdateProject);
        }

        public override int Execute()
        {
            SlnFile slnFile;
            slnFile = SlnFileFactory.CreateFromFileOrDirectory(_fileOrDirectory);

            if(string.IsNullOrEmpty(_configName) && string.IsNullOrEmpty(_platformName))
            {
                throw new GracefulException(LocalizableStrings.ConfigureAddNewConfigPlatformNameMissing);
            }

            if (string.IsNullOrEmpty(_updateProj) && string.IsNullOrEmpty(_updateProj))
            {
                throw new GracefulException(LocalizableStrings.ConfigureAddNewConfigOptionUpdateProjMissing);
            }
 
            try
            {
                slnFile.AddNewSlnConfig(_configName, _copyFrom, _updateProj);

                slnFile.WriteConfigutations();
            }
            catch (GracefulException e)
            {
                switch (e.Message)
                {
                    case "ConfigureAddConfigAlreadyExists":
                        throw new GracefulException(
                            string.Format(LocalizableStrings.ConfigureAddConfigAlreadyExists, _configName));

                    case "ConfigureAddCopyFromDoesNotExists":
                        throw new GracefulException(
                            string.Format(LocalizableStrings.ConfigureAddCopyFromConfigDoesNotExists, _copyFrom));
                }
            }

            return 0;
        }
    }
}

