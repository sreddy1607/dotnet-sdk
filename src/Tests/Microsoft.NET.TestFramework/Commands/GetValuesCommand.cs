﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.TestFramework.Assertions;
using Xunit.Abstractions;

namespace Microsoft.NET.TestFramework.Commands
{
    public sealed class GetValuesCommand : MSBuildCommand
    {
        public enum ValueType
        {
            Property,
            Item
        }

        string _targetFramework;

        string _valueName;
        ValueType _valueType;

        public bool ShouldCompile { get; set; } = true;

        public string DependsOnTargets { get; set; } = "Compile";

        public string TargetName { get; set; } = "WriteValuesToFile";

        public string Configuration { get; set; }

        public List<string> MetadataNames { get; set; } = new List<string>();
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        public bool ShouldRestore { get; set; } = true;

        protected override bool ExecuteWithRestoreByDefault => ShouldRestore;

        public GetValuesCommand(ITestOutputHelper log, string projectPath, string targetFramework,
            string valueName, ValueType valueType = ValueType.Property)
            : base(log, "WriteValuesToFile", projectPath, relativePathToProject: null)
        {
            _targetFramework = targetFramework;

            _valueName = valueName;
            _valueType = valueType;
        }

        public GetValuesCommand(TestAsset testAsset,
            string valueName, ValueType valueType = ValueType.Property,
            string targetFramework = null)
            : base(testAsset, "WriteValuesToFile", relativePathToProject: null)
        {
            _targetFramework = targetFramework ?? testAsset.TestProject?.TargetFrameworks;

            _valueName = valueName;
            _valueType = valueType;
        }

        protected override SdkCommandSpec CreateCommand(IEnumerable<string> args)
        {
            var newArgs = new List<string>();
            newArgs.Add(FullPathProjectFile);

            newArgs.Add($"/p:ValueName={_valueName}");
            newArgs.AddRange(args);

            //  Override build target to write out DefineConstants value to a file in the output directory
            Directory.CreateDirectory(GetBaseIntermediateDirectory().FullName);
            string injectTargetPath = Path.Combine(
                GetBaseIntermediateDirectory().FullName,
                Path.GetFileName(ProjectFile) + ".WriteValuesToFile.g.targets");

            string linesAttribute;
            if (_valueType == ValueType.Property)
            {
                linesAttribute = $"$({_valueName})";
            }
            else
            {
                linesAttribute = $"%({_valueName}.Identity)";
                foreach (var metadataName in MetadataNames)
                {
                    linesAttribute += $"%09%({_valueName}.{metadataName})";
                }
            }

            string propertiesElement = "";
            if (Properties.Count != 0)
            {
                propertiesElement += "<PropertyGroup>\n";
                foreach (var pair in Properties)
                {
                    propertiesElement += $"    <{pair.Key}>{pair.Value}</{pair.Key}>\n";
                }
                propertiesElement += "  </PropertyGroup>";
            }

            string injectTargetContents =
$@"<Project ToolsVersion=`14.0` xmlns=`http://schemas.microsoft.com/developer/msbuild/2003`>
  {propertiesElement}
  <Target Name=`{TargetName}` {(ShouldCompile ? $"DependsOnTargets=`{DependsOnTargets}`" : "")}>
    <ItemGroup>
      <LinesToWrite Include=`{linesAttribute}`/>
    </ItemGroup>
    <WriteLinesToFile
      File=`bin\$(Configuration)\$(TargetFramework)\{_valueName}Values.txt`
      Lines=`@(LinesToWrite)`
      Overwrite=`true`
      Encoding=`Unicode`
      />
  </Target>
</Project>";
            injectTargetContents = injectTargetContents.Replace('`', '"');

            File.WriteAllText(injectTargetPath, injectTargetContents);

            var outputDirectory = GetOutputDirectory(_targetFramework);
            outputDirectory.Create();

            return TestContext.Current.ToolsetUnderTest.CreateCommandForTarget(TargetName, newArgs);
        }

        public List<string> GetValues()
        {
            return GetValuesWithMetadata().Select(t => t.value).ToList();
        }

        public List<(string value, Dictionary<string, string> metadata)> GetValuesWithMetadata()
        {
            string outputFilename = $"{_valueName}Values.txt";
            var outputDirectory = GetOutputDirectory(_targetFramework, Configuration ?? "Debug");
            string fullFileName = Path.Combine(outputDirectory.FullName, outputFilename);

            if (File.Exists(fullFileName))
            {
                return File.ReadAllLines(fullFileName)
                   .Where(line => !string.IsNullOrWhiteSpace(line))
                   .Select(line =>
                   {
                       if (!MetadataNames.Any())
                       {
                           return (value: line, metadata: new Dictionary<string, string>());
                       }
                       else
                       {
                           var fields = line.Split('\t');

                           var dict = new Dictionary<string, string>();
                           for (int i = 0; i < MetadataNames.Count; i++)
                           {
                               dict[MetadataNames[i]] = fields[i + 1];
                           }

                           return (value: fields[0], metadata: dict);
                       }
                   })
                   .ToList();
            }
            else
            {
                return new List<(string value, Dictionary<string, string> metadata)>();
            }
        }
    }
}
