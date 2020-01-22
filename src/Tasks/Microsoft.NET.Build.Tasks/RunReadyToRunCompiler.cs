﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    public class RunReadyToRunCompiler : ToolTask
    {
        [Required]
        public ITaskItem CrossgenTool { get; set; }
        [Required]
        public ITaskItem CompilationEntry { get; set; }
        [Required]
        public ITaskItem[] ImplementationAssemblyReferences { get; set; }
        public bool ShowCompilerWarnings { get; set; }
        public string DotNetHostDirectory { get; set; }

        [Output]
        public bool WarningsDetected { get; set; }

        private string _crossgenPath;
        private string _clrjitPath;
        private string _diasymreaderPath;

        private string _inputAssembly;
        private string _outputR2RImage;
        private string _outputPDBImage;
        private string _createPDBCommand;

        private bool _isCrossgen2;

        private bool IsPdbCompilation => !String.IsNullOrEmpty(_createPDBCommand);

        private string DotNetHostFileName => Path.Combine(DotNetHostDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet");

        protected override string ToolName => _isCrossgen2 ? DotNetHostFileName : _crossgenPath;

        protected override string GenerateFullPathToTool() => ToolName;

        public RunReadyToRunCompiler()
        {
            LogStandardErrorAsError = true;
        }

        protected override bool ValidateParameters()
        {
            _crossgenPath = CrossgenTool.ItemSpec;
            _clrjitPath = CrossgenTool.GetMetadata("JitPath");
            _diasymreaderPath = CrossgenTool.GetMetadata("DiaSymReader");
            _isCrossgen2 = CrossgenTool.HasMetadataValue("UseCrossgen2");

            if (!File.Exists(_crossgenPath) || !File.Exists(_clrjitPath))
            {
                return false;
            }

            _createPDBCommand = CompilationEntry.GetMetadata("CreatePDBCommand");

            if (IsPdbCompilation)
            {
                _outputR2RImage = CompilationEntry.ItemSpec;
                _outputPDBImage = CompilationEntry.GetMetadata("OutputPDBImage");

                if (!String.IsNullOrEmpty(_diasymreaderPath) && !File.Exists(_diasymreaderPath))
                {
                    return false;
                }

                // R2R image has to be created before emitting native symbols (crossgen needs this as an input argument)
                if (String.IsNullOrEmpty(_outputPDBImage) || !File.Exists(_outputR2RImage))
                {
                    return false;
                }
            }
            else
            {
                _inputAssembly = CompilationEntry.ItemSpec;
                _outputR2RImage = CompilationEntry.GetMetadata("OutputR2RImage");

                if (!File.Exists(_inputAssembly))
                {
                    return false;
                }
            }

            return true;
        }

        private string GetAssemblyReferencesCommands()
        {
            StringBuilder result = new StringBuilder();

            foreach (var reference in ImplementationAssemblyReferences)
            {
                // When generating PDBs, we must not add a reference to the IL version of the R2R image for which we're trying to generate a PDB
                if (IsPdbCompilation && String.Equals(Path.GetFileName(reference.ItemSpec), Path.GetFileName(_outputR2RImage), StringComparison.OrdinalIgnoreCase))
                    continue;
                
                result.AppendLine( $"-r{(_isCrossgen2 ? ":" : " ")}\"{reference}\"");
            }

            return result.ToString();
        }

        protected override string GenerateResponseFileCommands()
        {
            return _isCrossgen2 ? GenerateCrossgen2ResponseFile() : GenerateCrossgenResponseFile();
        }

        protected override string GenerateCommandLineCommands()
        {
            return _isCrossgen2 ? _crossgenPath : null;
        }

        private string GenerateCrossgenResponseFile()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("/nologo");

            if (IsPdbCompilation)
            {
                result.Append(GetAssemblyReferencesCommands());

                if (!String.IsNullOrEmpty(_diasymreaderPath))
                {
                    result.AppendLine($"/DiasymreaderPath \"{_diasymreaderPath}\"");
                }

                result.AppendLine(_createPDBCommand);
                result.AppendLine($"\"{_outputR2RImage}\"");
            }
            else
            {
                result.AppendLine("/MissingDependenciesOK");
                result.AppendLine($"/JITPath \"{_clrjitPath}\"");
                result.Append(GetAssemblyReferencesCommands());
                result.AppendLine($"/out \"{_outputR2RImage}\"");
                result.AppendLine($"\"{_inputAssembly}\"");
            }

            return result.ToString();
        }

        private string GenerateCrossgen2ResponseFile()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("-O");
            result.AppendLine($"--jitpath:\"{_clrjitPath}\"");
            result.Append(GetAssemblyReferencesCommands());
            result.AppendLine($"--out:\"{_outputR2RImage}\"");
            // Note: do not add double quotes around the input assembly, even if the file path contains spaces. The command line 
            // parsing logic will append this string to the working directory if it's a relative path, so any double quotes will result in errors.
            result.AppendLine($"{_inputAssembly}");

            return result.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            // Ensure output sub-directories exists - Crossgen does not create directories for output files. Any relative path used with the 
            // '/out' parameter has to have an existing directory.
            Directory.CreateDirectory(Path.GetDirectoryName(_outputR2RImage));

            WarningsDetected = false;

            return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (!ShowCompilerWarnings && singleLine.IndexOf("warning:", StringComparison.OrdinalIgnoreCase) != -1)
            {
                Log.LogMessage(MessageImportance.Normal, singleLine);
                WarningsDetected = true;
            }
            else
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
            }
        }
    }
}
