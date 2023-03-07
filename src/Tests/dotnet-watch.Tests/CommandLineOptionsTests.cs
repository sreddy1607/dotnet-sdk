﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.CommandLine.IO;
using System.Linq;

using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class CommandLineOptionsTests
    {
        private readonly Extensions.Tools.Internal.TestReporter _testReporter;
        private readonly TestConsole _console;

        public CommandLineOptionsTests(ITestOutputHelper output)
        {
            _console = new();
            _testReporter = new(output);
        }

        [Theory]
        [InlineData(new object[] { new[] { "-h" } })]
        [InlineData(new object[] { new[] { "-?" } })]
        [InlineData(new object[] { new[] { "--help" } })]
        [InlineData(new object[] { new[] { "--help", "--bogus" } })]
        public void HelpArgs(string[] args)
        {
            Assert.Null(CommandLineOptions.Parse(args, _testReporter, out var errorCode, _console));
            Assert.Equal(0, errorCode);

            Assert.Contains("Usage:", _console.Out.ToString());
        }

        [Fact]
        public void RunOptions_NoRun()
        {
            var args = new[] { "--verbose" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.True(options.Verbose);
            Assert.False(options.NoLaunchProfile);
            Assert.Null(options.LaunchProfileName);
            Assert.Empty(options.RemainingArguments);

            Assert.Null(options.RunOptions);

            Assert.Equal(new[] { "run" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out var watchNoProfile, out var watchProfileName));
            Assert.False(watchNoProfile);
            Assert.Null(watchProfileName);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out watchNoProfile, out watchProfileName));
            Assert.False(watchNoProfile);
            Assert.Null(watchProfileName);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_Run()
        {
            var args = new[] { "--verbose", "run" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.True(options.Verbose);
            Assert.False(options.NoLaunchProfile);
            Assert.Null(options.LaunchProfileName);
            Assert.Empty(options.RemainingArguments);

            Assert.False(options.RunOptions.NoLaunchProfile);
            Assert.Null(options.RunOptions.LaunchProfileName);
            Assert.Empty(options.RunOptions.RemainingArguments);

            Assert.Equal(new[] { "run" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out var watchNoProfile, out var watchProfileName));
            Assert.False(watchNoProfile);
            Assert.Null(watchProfileName);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out watchNoProfile, out watchProfileName));
            Assert.False(watchNoProfile);
            Assert.Null(watchProfileName);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_LaunchProfile_Watch()
        {
            var args = new[] { "-lp", "P", "run" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.Equal("P", options.LaunchProfileName);
            Assert.Null(options.RunOptions.LaunchProfileName);

            Assert.Equal(new[] { "run" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out _, out var watchProfileName));
            Assert.Equal("P", watchProfileName);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out _, out watchProfileName));
            Assert.Equal("P", watchProfileName);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_LaunchProfile_Run()
        {
            var args = new[] { "run", "-lp", "P" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.Null(options.LaunchProfileName);
            Assert.Equal("P", options.RunOptions.LaunchProfileName);

            Assert.Equal(new[] { "run", "--launch-profile", "P" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out _, out var watchProfileName));
            Assert.Null(watchProfileName);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out _, out watchProfileName));
            Assert.Equal("P", watchProfileName);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_LaunchProfile_Both()
        {
            var args = new[] { "-lp", "P1", "run", "-lp", "P2" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.Equal("P1", options.LaunchProfileName);
            Assert.Equal("P2", options.RunOptions.LaunchProfileName);

            Assert.Equal(new[] { "run", "--launch-profile", "P2" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out _, out var watchProfileName));
            Assert.Equal("P1", watchProfileName);

            var reporter = new Mock<Extensions.Tools.Internal.IReporter>();
            reporter.Setup(r => r.Warn($"Using launch profile name 'P1', ignoring 'P2'.", It.IsAny<string>())).Verifiable();

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, reporter.Object, out _, out watchProfileName));
            Assert.Equal("P1", watchProfileName);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_NoProfile_Watch()
        {
            var args = new[] { "--no-launch-profile", "run" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.True(options.NoLaunchProfile);
            Assert.False(options.RunOptions.NoLaunchProfile);

            Assert.Equal(new[] { "run", }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out var watchNoLaunchProfile, out _));
            Assert.True(watchNoLaunchProfile);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out watchNoLaunchProfile, out _));
            Assert.True(watchNoLaunchProfile);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_NoProfile_Run()
        {
            var args = new[] { "run", "--no-launch-profile" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.False(options.NoLaunchProfile);
            Assert.True(options.RunOptions.NoLaunchProfile);

            Assert.Equal(new[] { "run", "--no-launch-profile" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out var watchNoLaunchProfile, out _));
            Assert.False(watchNoLaunchProfile);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out watchNoLaunchProfile, out _));
            Assert.True(watchNoLaunchProfile);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RunOptions_NoProfile_Both()
        {
            var args = new[] { "--no-launch-profile", "run", "--no-launch-profile" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.True(options.NoLaunchProfile);
            Assert.True(options.RunOptions.NoLaunchProfile);

            Assert.Equal(new[] { "run", "--no-launch-profile" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out var watchNoLaunchProfile, out _));
            Assert.True(watchNoLaunchProfile);

            Assert.Empty(options.GetLaunchProcessArguments(hotReload: true, _testReporter, out watchNoLaunchProfile, out _));
            Assert.True(watchNoLaunchProfile);

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RemainingOptions()
        {
            var args = new[] { "-watchArg", "--verbose", "run", "-runArg" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);
            //dotnet watch -- --verbose run
            Assert.True(options.Verbose);
            Assert.Equal(new[] { "-watchArg" }, options.RemainingArguments);
            Assert.Equal(new[] { "-runArg" }, options.RunOptions.RemainingArguments);

            Assert.Equal(new[] { "run", "-watchArg", "-runArg" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out _, out _));
            Assert.Equal(new[] { "-watchArg", "-runArg" }, options.GetLaunchProcessArguments(hotReload: true, _testReporter, out _, out _));

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RemainingOptionsDashDash()
        {
            var args = new[] { "-watchArg", "--", "--verbose", "run", "-runArg" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.False(options.Verbose);
            Assert.Equal(new[] { "-watchArg", "--verbose", "run", "-runArg" }, options.RemainingArguments);
            Assert.Null(options.RunOptions);

            Assert.Equal(new[] { "run", "-watchArg", "--verbose", "run", "-runArg" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out _, out _));
            Assert.Equal(new[] { "-watchArg", "--verbose", "run", "-runArg" }, options.GetLaunchProcessArguments(hotReload: true, _testReporter, out _, out _));

            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void RemainingOptionsDashDashRun()
        {
            var args = new[] { "--", "run" };
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.False(options.Verbose);
            Assert.Equal(new[] { "run" }, options.RemainingArguments);
            Assert.Null(options.RunOptions);

            Assert.Equal(new[] { "run", "run" }, options.GetLaunchProcessArguments(hotReload: false, _testReporter, out _, out _));
            Assert.Equal(new[] { "run" }, options.GetLaunchProcessArguments(hotReload: true, _testReporter, out _, out _));

            Assert.Empty(_console.Out.ToString());
        }

        [Theory]
        [CombinatorialData]
        public void Project_BeforeOrAfterRun(bool afterRun)
        {
            var args = new[] { "--project", "abc" };
            args = afterRun ? args.Prepend("run").ToArray() : args.Append("run").ToArray();

            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.Equal("abc", options.Project);
            Assert.Empty(_console.Out.ToString());
        }

        public enum ArgPosition
        {
            Before,
            After,
            Both
        }

        [Theory]
        [CombinatorialData]
        public void OptionDuplicates_Allowed(
            ArgPosition position,
            [CombinatorialValues(
                "--verbose",
                "--quiet",
                "--list",
                "--no-hot-reload",
                "--non-interactive"
            )] string arg)
        {
            var args = new[] { arg };

            args = position switch
            {
                ArgPosition.Before => args.Prepend("run").ToArray(),
                ArgPosition.Both => args.Concat(new[] { "run" }).Concat(args).ToArray(),
                ArgPosition.After => args.Append("run").ToArray(),
                _ => args,
            };

            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.True(arg switch
            {
                "--verbose" => options.Verbose,
                "--quiet" => options.Quiet,
                "--list" => options.List,
                "--no-hot-reload" => options.NoHotReload,
                "--non-interactive" => options.NonInteractive,
                _ => false
            });

            Assert.Empty(_console.Out.ToString());
        }

        [Theory]
        [InlineData(new object[] { new[] { "--project", "abc" } })]
        public void OptionDuplicates_NotAllowed(string[] args)
        {
            args = args.Concat(new[] { "run" }).Concat(args).ToArray();

            var options = CommandLineOptions.Parse(args, _testReporter, out var errorCode, _console);
            Assert.Null(options);
            Assert.Equal(1, errorCode);

            Assert.Equal("", _console.Out.ToString());
        }

        [Theory]
        [InlineData(new[] { "--unrecognized-arg" }, new[] { "--unrecognized-arg" }, new string[0])]
        [InlineData(new[] { "run" }, new string[0], new string[0])]
        [InlineData(new[] { "run", "--", "runarg" }, new string[0], new[] { "runarg" })]
        [InlineData(new[] { "-watcharg", "run", "runarg1", "-runarg2" }, new[] { "-watcharg" }, new[] { "runarg1", "-runarg2" })]
        // run is after -- and therefore not parsed as a command:
        [InlineData(new[] { "-watcharg", "--", "run", "--", "runarg" }, new[] { "-watcharg", "run", "--", "runarg" }, new string[0])]
        // run is before -- and therefore parsed as a command:
        [InlineData(new[] { "-watcharg", "run", "--", "--", "runarg" }, new[] { "-watcharg" }, new[] { "--", "runarg" })]
        public void ParsesRemainingArgs(string[] args, string[] expectedWatch, string[] expectedRun)
        {
            var options = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.NotNull(options);

            Assert.Equal(expectedWatch, options.RemainingArguments);
            Assert.Equal(expectedRun, options.RunOptions?.RemainingArguments ?? Array.Empty<string>());
            Assert.Empty(_console.Out.ToString());
        }

        [Fact]
        public void CannotHaveQuietAndVerbose()
        {
            var args = new[] { "--quiet", "--verbose" };
            _ = CommandLineOptions.Parse(args, _testReporter, out _, _console);

            Assert.Contains(Resources.Error_QuietAndVerboseSpecified, _console.Error.ToString());
        }

        [Fact]
        public void ShortFormForProjectArgumentPrintsWarning()
        {
            var reporter = new Mock<Extensions.Tools.Internal.IReporter>();
            reporter.Setup(r => r.Warn(Resources.Warning_ProjectAbbreviationDeprecated, It.IsAny<string>())).Verifiable();

            var args = new[] { "-p", "MyProject.csproj" };
            var options = CommandLineOptions.Parse(args, reporter.Object, out _, _console);

            reporter.Verify();
            Assert.NotNull(options);
            Assert.Equal("MyProject.csproj", options.Project);
        }

        [Fact]
        public void LongFormForProjectArgumentWorks()
        {
            var reporter = new Mock<Extensions.Tools.Internal.IReporter>();

            var args = new[] { "--project", "MyProject.csproj" };
            var options = CommandLineOptions.Parse(args, reporter.Object, out _, _console);

            reporter.Verify(r => r.Warn(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Assert.NotNull(options);
            Assert.Equal("MyProject.csproj", options.Project);
        }

        [Fact]
        public void LongFormForLaunchProfileArgumentWorks()
        {
            var reporter = new Mock<Extensions.Tools.Internal.IReporter>();

            var args = new[] { "--launch-profile", "CustomLaunchProfile" };
            var options = CommandLineOptions.Parse(args, reporter.Object, out _, _console);

            reporter.Verify(r => r.Warn(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Assert.NotNull(options);
            Assert.Equal("CustomLaunchProfile", options.LaunchProfileName);
        }

        [Fact]
        public void ShortFormForLaunchProfileArgumentWorks()
        {
            var reporter = new Mock<Extensions.Tools.Internal.IReporter>();

            var args = new[] { "-lp", "CustomLaunchProfile" };
            var options = CommandLineOptions.Parse(args, reporter.Object, out _, _console);

            reporter.Verify(r => r.Warn(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Assert.NotNull(options);
            Assert.Equal("CustomLaunchProfile", options.LaunchProfileName);
        }
    }
}
