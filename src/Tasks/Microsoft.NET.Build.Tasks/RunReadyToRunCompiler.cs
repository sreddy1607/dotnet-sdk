﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    public class RunReadyToRunCompiler : ToolTask
    {
        public ITaskItem CrossgenTool { get; set; }
        public ITaskItem Crossgen2Tool { get; set; }

        [Required]
        public ITaskItem CompilationEntry { get; set; }
        [Required]
        public ITaskItem[] ImplementationAssemblyReferences { get; set; }
        public bool ShowCompilerWarnings { get; set; }
        public bool UseCrossgen2 { get; set; }
        public bool Crossgen2Composite { get; set; }
        public bool EmitSymbols { get; set; }
        public string Crossgen2ExtraCommandLineArgs { get; set; }

        [Output]
        public bool WarningsDetected { get; set; }

        private string _inputAssembly;
        private string _outputR2RImage;
        private string _outputPDBImage;
        private string _createPDBCommand;

        private bool IsPdbCompilation => !String.IsNullOrEmpty(_createPDBCommand);
        private bool ActuallyUseCrossgen2 => UseCrossgen2 && !IsPdbCompilation;

        private string DotNetHostPath => Crossgen2Tool?.GetMetadata(MetadataKeys.DotNetHostPath);

        private bool Crossgen2IsVersion5
        {
            get
            {
                string version5 = Crossgen2Tool?.GetMetadata(MetadataKeys.IsVersion5);
                return !string.IsNullOrEmpty(version5) && bool.Parse(version5);
            }
        }

        protected override string ToolName
        {
            get
            {
                if (ActuallyUseCrossgen2)
                {
                    string hostPath = DotNetHostPath;
                    if (!string.IsNullOrEmpty(hostPath))
                    {
                        return hostPath;
                    }
                    return Crossgen2Tool.ItemSpec;
                }
                return CrossgenTool.ItemSpec;
            }
        }

        protected override string GenerateFullPathToTool() => ToolName;

        private string DiaSymReader => CrossgenTool.GetMetadata(MetadataKeys.DiaSymReader);

        public RunReadyToRunCompiler()
        {
            LogStandardErrorAsError = true;
        }

        protected override bool ValidateParameters()
        {
            _createPDBCommand = CompilationEntry.GetMetadata(MetadataKeys.CreatePDBCommand);

            if (IsPdbCompilation && CrossgenTool == null)
            {
                // PDB compilation is a step specific to Crossgen1 and 5.0 Crossgen2
                // which didn't support PDB generation. 6.0  Crossgen2 produces symbols
                // directly during native compilation.
                Log.LogError(Strings.CrossgenToolMissingInPDBCompilationMode);
                return false;
            }

            if (ActuallyUseCrossgen2)
            {
                if (Crossgen2Tool == null)
                {
                    Log.LogError(Strings.Crossgen2ToolMissingWhenUseCrossgen2IsSet);
                    return false;
                }
                if (!File.Exists(Crossgen2Tool.ItemSpec))
                {
                    Log.LogError(Strings.Crossgen2ToolExecutableNotFound, Crossgen2Tool.ItemSpec);
                    return false;
                }
                string hostPath = DotNetHostPath;
                if (!string.IsNullOrEmpty(hostPath) && !File.Exists(hostPath))
                {
                    Log.LogError(Strings.DotNetHostExecutableNotFound, hostPath);
                    return false;
                }
                if (Crossgen2IsVersion5)
                {
                    // We expect JitPath to be set for .NET 5 and {TargetOS, TargetArch} to be set for .NET 6 and later
                    string jitPath = Crossgen2Tool.GetMetadata(MetadataKeys.JitPath);
                    if (String.IsNullOrEmpty(jitPath))
                    {
                        Log.LogError(Strings.Crossgen2MissingRequiredMetadata, MetadataKeys.JitPath);
                        return false;
                    }
                    if (!File.Exists(jitPath))
                    {
                        Log.LogError(Strings.JitLibraryNotFound, jitPath);
                        return false;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS)))
                    {
                        Log.LogError(Strings.Crossgen2MissingRequiredMetadata, MetadataKeys.TargetOS);
                        return false;
                    }
                    if (string.IsNullOrEmpty(Crossgen2Tool.GetMetadata(MetadataKeys.TargetArch)))
                    {
                        Log.LogError(Strings.Crossgen2MissingRequiredMetadata, MetadataKeys.TargetArch);
                        return false;
                    }
                }
            }
            else
            {
                if (CrossgenTool == null)
                {
                    Log.LogError(Strings.CrossgenToolMissingWhenUseCrossgen2IsNotSet);
                    return false;
                }
                if (!File.Exists(CrossgenTool.ItemSpec))
                {
                    Log.LogError(Strings.CrossgenToolExecutableNotFound, CrossgenTool.ItemSpec);
                    return false;
                }
                if (!File.Exists(CrossgenTool.GetMetadata(MetadataKeys.JitPath)))
                {
                    Log.LogError(Strings.JitLibraryNotFound, MetadataKeys.JitPath);
                    return false;
                }
            }

            if (IsPdbCompilation)
            {
                _outputR2RImage = CompilationEntry.ItemSpec;
                _outputPDBImage = CompilationEntry.GetMetadata(MetadataKeys.OutputPDBImage);

                if (!String.IsNullOrEmpty(DiaSymReader) && !File.Exists(DiaSymReader))
                {
                    Log.LogError(Strings.DiaSymReaderLibraryNotFound, DiaSymReader);
                    return false;
                }

                // R2R image has to be created before emitting native symbols (crossgen needs this as an input argument)
                if (String.IsNullOrEmpty(_outputPDBImage))
                {
                    Log.LogError(Strings.MissingOutputPDBImagePath);
                }

                if (!File.Exists(_outputR2RImage))
                {
                    Log.LogError(Strings.PDBGeneratorInputExecutableNotFound, _outputR2RImage);
                    return false;
                }
            }
            else
            {
                _inputAssembly = CompilationEntry.ItemSpec;
                _outputR2RImage = CompilationEntry.GetMetadata(MetadataKeys.OutputR2RImage);

                if (!File.Exists(_inputAssembly))
                {
                    Log.LogError(Strings.InputAssemblyNotFound, _inputAssembly);
                    return false;
                }
                
                if (string.IsNullOrEmpty(_outputR2RImage))
                {
                    Log.LogError(Strings.MissingOutputR2RImageFileName);
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

                if (UseCrossgen2 && !IsPdbCompilation)
                {
                    result.AppendLine($"-r:\"{reference}\"");
                }
                else
                {
                    result.AppendLine($"-r \"{reference}\"");
                }
            }

            return result.ToString();
        }

        protected override string GenerateCommandLineCommands()
        {
            if (ActuallyUseCrossgen2 && !string.IsNullOrEmpty(DotNetHostPath))
            {
                return $"\"{Crossgen2Tool.ItemSpec}\"";
            }
            return null;
        }

        protected override string GenerateResponseFileCommands()
        {
            // Crossgen2 5.0 doesn't support PDB generation so Crossgen1 is used for that purpose.
            if (ActuallyUseCrossgen2)
            {
                return GenerateCrossgen2ResponseFile();
            }
            else
            {
                return GenerateCrossgenResponseFile();
            }
        }

        private string GenerateCrossgenResponseFile()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("/nologo");

            if (IsPdbCompilation)
            {
                result.Append(GetAssemblyReferencesCommands());

                if (!String.IsNullOrEmpty(DiaSymReader))
                {
                    result.AppendLine($"/DiasymreaderPath \"{DiaSymReader}\"");
                }

                result.AppendLine(_createPDBCommand);
                result.AppendLine($"\"{_outputR2RImage}\"");
            }
            else
            {
                result.AppendLine("/MissingDependenciesOK");
                result.AppendLine($"/JITPath \"{CrossgenTool.GetMetadata(MetadataKeys.JitPath)}\"");
                result.Append(GetAssemblyReferencesCommands());
                result.AppendLine($"/out \"{_outputR2RImage}\"");
                result.AppendLine($"\"{_inputAssembly}\"");
            }

            return result.ToString();
        }

        private string GenerateCrossgen2ResponseFile()
        {
            StringBuilder result = new StringBuilder();
            
            if (Crossgen2IsVersion5)
            {
                result.AppendLine($"--jitpath:\"{Crossgen2Tool.GetMetadata(MetadataKeys.JitPath)}\"");
            }
            else
            {
                result.AppendLine($"--targetos:{Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS)}");
                result.AppendLine($"--targetarch:{Crossgen2Tool.GetMetadata(MetadataKeys.TargetArch)}");
            }

            result.AppendLine("-O");
            
            // 5.0 Crossgen2 doesn't support PDB generation.
            if (!Crossgen2IsVersion5 && EmitSymbols)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    result.AppendLine("--pdb");
                }
                else
                {
                    result.AppendLine("--perfmap");
                }
            }

            if (!String.IsNullOrEmpty(Crossgen2ExtraCommandLineArgs))
            {
                foreach (string extraArg in Crossgen2ExtraCommandLineArgs.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    result.AppendLine(extraArg);
                }
            }

            if (Crossgen2Composite)
            {
                result.AppendLine("--composite");
                result.AppendLine("--inputbubble");
                result.AppendLine($"--out:\"{_outputR2RImage}\"");

                // Note: do not add double quotes around the input assembly, even if the file path contains spaces. The command line
                // parsing logic will append this string to the working directory if it's a relative path, so any double quotes will result in errors.
                foreach (var reference in ImplementationAssemblyReferences)
                {
                    result.AppendLine(reference.ItemSpec);
                }
            }
            else
            {
                result.Append(GetAssemblyReferencesCommands());
                result.AppendLine($"--out:\"{_outputR2RImage}\"");

                // Note: do not add double quotes around the input assembly, even if the file path contains spaces. The command line
                // parsing logic will append this string to the working directory if it's a relative path, so any double quotes will result in errors.
                result.AppendLine($"{_inputAssembly}");
            }

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
