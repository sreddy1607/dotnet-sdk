﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FluentAssertions.Execution;

namespace Microsoft.NET.TestFramework.Assertions
{
    public class FileInfoAssertions
    {
        private FileInfo _fileInfo;

        public FileInfoAssertions(FileInfo file)
        {
            _fileInfo = file;
        }

        public FileInfo FileInfo => _fileInfo;

        public AndConstraint<FileInfoAssertions> Exist(string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(_fileInfo.Exists)
                .BecauseOf(because, reasonArgs)
                .FailWith($"Expected File {_fileInfo.FullName} to exist, but it does not.");
            return new AndConstraint<FileInfoAssertions>(this);
        }

        public AndConstraint<FileInfoAssertions> NotExist(string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .ForCondition(!_fileInfo.Exists)
                .BecauseOf(because, reasonArgs)
                .FailWith($"Expected File {_fileInfo.FullName} to not exist, but it does.");
            return new AndConstraint<FileInfoAssertions>(this);
        }

        public AndWhichConstraint<FileInfoAssertions, DateTimeOffset> HaveLastWriteTimeUtc(string because = "", params object[] reasonArgs)
        {
            var lastWriteTimeUtc = _fileInfo.LastWriteTimeUtc;

            return new AndWhichConstraint<FileInfoAssertions, DateTimeOffset>(this, lastWriteTimeUtc);
        }

        public AndConstraint<FileInfoAssertions> HashEquals(string expectedSha)
        {
            using var algorithm = SHA256.Create();
            var actualSha256 = algorithm.ComputeHash(File.ReadAllBytes(_fileInfo.FullName));
            var actualSha256Base64 = Convert.ToBase64String(actualSha256);

            Execute.Assertion
                .ForCondition(actualSha256Base64 == expectedSha)
                .FailWith($"File {_fileInfo.FullName} did not have SHA matching {expectedSha}. Found {actualSha256Base64}.");

            return new AndConstraint<FileInfoAssertions>(this);
        }

        public AndConstraint<FileInfoAssertions> Contain(string expectedContent)
        {
            var actualContent = File.ReadAllText(_fileInfo.FullName);

            Execute.Assertion
                .ForCondition(actualContent.Contains(expectedContent))
                .FailWith($"File {_fileInfo.FullName} did not have content: {expectedContent}.");

            return new AndConstraint<FileInfoAssertions>(this);
        }

#if NET47_OR_GREATER
        public AndConstraint<FileInfoAssertions> Match(string pattern)
#else
        public AndConstraint<FileInfoAssertions> Match([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
#endif
        {
            var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant);
            var actualContent = File.ReadAllText(_fileInfo.FullName);

            Execute.Assertion
                .ForCondition(regex.IsMatch(actualContent))
                .FailWith($"File {_fileInfo.FullName} did not match pattern: {pattern}.");

            return new AndConstraint<FileInfoAssertions>(this);
        }

        public AndConstraint<FileInfoAssertions> NotContain(string expectedContent)
        {
            var actualContent = File.ReadAllText(_fileInfo.FullName);

            Execute.Assertion
                .ForCondition(!actualContent.Contains(expectedContent))
                .FailWith($"File {_fileInfo.FullName} had content: {expectedContent}.");

            return new AndConstraint<FileInfoAssertions>(this);
        }

    }
}
