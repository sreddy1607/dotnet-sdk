// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Tasks;
using Microsoft.DotNet.Watcher;

namespace Microsoft.Extensions.Tools.Internal
{
    internal enum MessageSeverity
    {
        None,
        Verbose,
        Output,
        Warning,
        Error,
    }

    internal readonly record struct MessageDescriptor(string? Format, string? Emoji, MessageSeverity Severity, int? Id)
    {
        private static readonly int s_id;

        [MemberNotNullWhen(true, nameof(Format), nameof(Emoji))]
        public bool HasMessage
            => Severity != MessageSeverity.None;


        // predefined messages used for testing:
        public static readonly MessageDescriptor HotReloadSessionStarting = new(Format: null, Emoji: null, MessageSeverity.None, s_id++);
        public static readonly MessageDescriptor HotReloadSessionStarted = new("Hot reload session started.", "🔥", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor HotReloadSessionEnded = new("Hot reload session ended.", "🔥", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor FixBuildError = new("Fix the error to continue or press Ctrl+C to exit.", "⌚", MessageSeverity.Warning, s_id++);
        public static readonly MessageDescriptor WaitingForChanges = new("Waiting for changes", "⌚", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor LaunchedProcess = new("Launched '{0}' with arguments '{1}': process id {2}", "🚀", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor KillingProcess = new("Killing process {0}", "⌚", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor HotReloadChangeHandled = new("Hot reload change handled in {0}ms.", "🔥", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor BuildCompleted = new("Build completed.", "⌚", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor UpdatesApplied = new("Updates applied: {0} out of {1}.", "🔥", MessageSeverity.Verbose, s_id++);
        public static readonly MessageDescriptor WaitingForFileChangeBeforeRestarting = new("Waiting for a file to change before restarting dotnet...", "⏳", MessageSeverity.Warning, s_id++);
    }

    internal interface IReporter
    {
        void Report(MessageDescriptor descriptor, string prefix, object?[] args);
        void ProcessOutput(string projectPath, string data);

        public bool IsVerbose
            => false;

        /// <summary>
        /// True to call <see cref="ProcessOutput"/> when launched process writes to standard output.
        /// Used for testing.
        /// </summary>
        bool ReportProcessOutput { get; }

        void Report(MessageDescriptor descriptor, params object?[] args)
            => Report(descriptor, prefix: "", args);

        void Verbose(string message, string emoji = "⌚")
            => Report(new MessageDescriptor(message, emoji, MessageSeverity.Verbose, Id: null));

        void Output(string message, string emoji = "⌚")
            => Report(new MessageDescriptor(message, emoji, MessageSeverity.Output, Id: null));

        void Warn(string message, string emoji = "⌚")
            => Report(new MessageDescriptor(message, emoji, MessageSeverity.Warning, Id: null));

        void Error(string message, string emoji = "❌")
            => Report(new MessageDescriptor(message, emoji, MessageSeverity.Error, Id: null));
    }
}
