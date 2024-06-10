﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Test
{
    internal sealed record CommandLineOptionMessage(string Name, string Description, bool IsHidden, bool IsBuiltIn) : IRequest;

    internal sealed record CommandLineOptionMessages(string ModuleName, CommandLineOptionMessage[] CommandLineOptionMessageList) : IRequest;
}
