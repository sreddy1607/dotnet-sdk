// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.NET.Build.Containers;

/// <summary>
/// Abstracts over the concept of a local container runtime of some kind. Currently this is only modeled by Docker,
/// but users have expressed desires for Podman, nerdctl, etc as well so this kind of abstraction makes sense to have.
/// </summary>
internal interface ILocalDaemon {

    /// <summary>
    /// Loads an image (presumably from a tarball) into the local container runtime.
    /// </summary>
    public Task LoadAsync(BuiltImage image, ImageReference sourceReference, ImageReference destinationReference, CancellationToken cancellationToken);

    /// <summary>
    /// Checks to see if the local container runtime is available. This is used to give nice errors to the user.
    /// </summary>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks to see if the local container runtime is available. This is used to give nice errors to the user.
    /// See <see cref="IsAvailableAsync(CancellationToken)"/> for async version.
    /// </summary>
    public bool IsAvailable();
}
