
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal static class IncrementalValuesProviderExtensions
    {
        internal static IncrementalValuesProvider<T> WithLambdaComparer<T>(this IncrementalValuesProvider<T> source, Func<T?, T?, bool> equal)
        {
            var comparer = new LambdaComparer<T>(equal);
            return source.WithComparer(comparer);
        }

        internal static IncrementalValueProvider<T> WithLambdaComparer<T>(this IncrementalValueProvider<T> source, Func<T?, T?, bool> equal)
        {
            var comparer = new LambdaComparer<T>(equal);
            return source.WithComparer(comparer);
        }

        internal static IncrementalValuesProvider<TSource> ReportDiagnostics<TSource>(this IncrementalValuesProvider<(TSource?, Diagnostic?)> source, IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(source, (spc, source) =>
            {
                var (sourceItem, diagnostic) = source;
                if (sourceItem == null && diagnostic != null)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            });

            return source.Where((pair) => pair.Item1 != null).Select((pair, ct) => pair.Item1!);
        }

        internal static IncrementalValueProvider<TSource> ReportDiagnostics<TSource>(this IncrementalValueProvider<(TSource?, Diagnostic?)> source, IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(source, (spc, source) =>
            {
                var (sourceItem, diagnostic) = source;
                if (sourceItem == null && diagnostic != null)
                {
                    spc.ReportDiagnostic(diagnostic);
                }
            });

            return source.Select((pair, ct) => pair.Item1!);
        }

        internal static IncrementalValuesProvider<TSource> RecordCalls<TSource>(this IncrementalValuesProvider<TSource> source, string name, Dictionary<string, int> calls)
        {
            return source.Select((a, b) => { calls[name] = calls.ContainsKey(name) ? calls[name] + 1 : 1; return a; });
        }

        internal static IncrementalValueProvider<TSource> RecordCalls<TSource>(this IncrementalValueProvider<TSource> source, string name, Dictionary<string, int> calls)
        {
            return source.Select((a, b) => { calls[name] = calls.ContainsKey(name) ? calls[name] + 1 : 1; return a; });
        }

        internal static IncrementalValuesProvider<(TLhs, TRhs)> WithLHSComparer<TLhs, TRhs>(this IncrementalValuesProvider<(TLhs, TRhs)> source) => WithLHSComparer(source, EqualityComparer<TLhs>.Default);

        internal static IncrementalValuesProvider<(TLhs, TRhs)> WithLHSComparer<TLhs, TRhs>(this IncrementalValuesProvider<(TLhs, TRhs)> source, Func<TLhs?, TLhs?, bool> equal) => WithLHSComparer(source, new LambdaComparer<TLhs>(equal));

        internal static IncrementalValuesProvider<(TLhs, TRhs)> WithLHSComparer<TLhs, TRhs>(this IncrementalValuesProvider<(TLhs, TRhs)> source, IEqualityComparer<TLhs> comparer)
        {
            return source.WithLambdaComparer((@new, old) => comparer.Equals(@new.Item1, old.Item1));
        }
    }

    internal class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _equal;

        public LambdaComparer(Func<T?, T?, bool> equal)
        {
            _equal = equal;
        }

        public bool Equals(T? x, T? y) => _equal(x, y);

        public int GetHashCode(T obj) =>  EqualityComparer<T>.Default.GetHashCode(obj);
    }
}
