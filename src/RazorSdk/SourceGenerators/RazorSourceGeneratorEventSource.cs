﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    [EventSource(Name = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator")]
    internal sealed class RazorSourceGeneratorEventSource : EventSource
    {
        public static readonly RazorSourceGeneratorEventSource Log = new();

        private RazorSourceGeneratorEventSource() { }

        private const int ComputeRazorSourceGeneratorOptionsId = 1;

        [Event(ComputeRazorSourceGeneratorOptionsId, Level = EventLevel.Informational)]
        public void ComputeRazorSourceGeneratorOptions()
        {
            if (IsEnabled())
            {
                WriteEvent(ComputeRazorSourceGeneratorOptionsId);
            }
        }

        private const int GenerateDeclarationCodeStartId = 2;

        [Event(GenerateDeclarationCodeStartId, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        public void GenerateDeclarationCodeStart(string filePath)
        {
            if (IsEnabled())
            {
                WriteEvent(GenerateDeclarationCodeStartId, filePath);
            }
        }

        private const int GenerateDeclarationCodeStopId = 4;
        [Event(GenerateDeclarationCodeStopId, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        public void GenerateDeclarationCodeStop(string filePath)
        {
            if (IsEnabled())
            {
                WriteEvent(GenerateDeclarationCodeStopId, filePath);
            }
        }

        private const int DiscoverTagHelpersFromCompilationStartId = 6;
        [Event(DiscoverTagHelpersFromCompilationStartId, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        public void DiscoverTagHelpersFromCompilationStart()
        {
            if (IsEnabled())
            {
                WriteEvent(DiscoverTagHelpersFromCompilationStartId);
            }
        }

        private const int DiscoverTagHelpersFromCompilationStopId = 7;
        [Event(DiscoverTagHelpersFromCompilationStopId, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        public void DiscoverTagHelpersFromCompilationStop()
        {
            if (IsEnabled())
            {
                WriteEvent(DiscoverTagHelpersFromCompilationStopId);
            }
        }

        private const int DiscoverTagHelpersFromReferencesStartId = 8;
        [Event(DiscoverTagHelpersFromReferencesStartId, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        public void DiscoverTagHelpersFromReferencesStart()
        {
            if (IsEnabled())
            {
                WriteEvent(DiscoverTagHelpersFromReferencesStartId);
            }
        }

        private const int DiscoverTagHelpersFromReferencesStopId = 9;
        [Event(DiscoverTagHelpersFromReferencesStopId, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        public void DiscoverTagHelpersFromReferencesStop()
        {
            if (IsEnabled())
            {
                WriteEvent(DiscoverTagHelpersFromReferencesStopId);
            }
        }

        private const int RazorCodeGenerateStartId = 10;
        [Event(RazorCodeGenerateStartId, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        public void RazorCodeGenerateStart(string file)
        {
            if (IsEnabled())
            {
                WriteEvent(RazorCodeGenerateStartId, file);
            }
        }

        private const int RazorCodeGenerateStopId = 11;
        [Event(RazorCodeGenerateStopId, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        public void RazorCodeGenerateStop(string file)
        {
            if (IsEnabled())
            {
                WriteEvent(RazorCodeGenerateStopId, file);
            }
        }

        private const int AddSyntaxTreesId = 12;
        [Event(AddSyntaxTreesId, Level = EventLevel.Informational)]
        public void AddSyntaxTrees(string file)
        {
            if (IsEnabled())
            {
                WriteEvent(AddSyntaxTreesId, file);
            }
        }

        private const int GenerateDeclarationSyntaxTreeStartId = 13;
        [Event(GenerateDeclarationSyntaxTreeStartId, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        public void GenerateDeclarationSyntaxTreeStart()
        {
            if (IsEnabled())
            {
                WriteEvent(GenerateDeclarationSyntaxTreeStartId);
            }
        }

        private const int GenerateDeclarationSyntaxTreeStopId = 14;
        [Event(GenerateDeclarationSyntaxTreeStopId, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        public void GenerateDeclarationSyntaxTreeStop()
        {
            if (IsEnabled())
            {
                WriteEvent(GenerateDeclarationSyntaxTreeStopId);
            }
        }
    }
}
