﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;

namespace Microsoft.NET.Build.Tasks
{
    /// <summary>
    /// Check if Runtime Identifier of the current MSBuild task exists in the targets of the project.assets.json.
    /// Throw an error if RID is not present in the targets.
    /// </summary>
    public sealed class CheckRuntimeIdentifier : TaskBase
    {
        #region Input Items
        /// <summary>
        /// Path to assets.json.
        /// </summary>
        public string ProjectAssetsFile { get; set; }
        /// <summary>
        /// TargetFramework to use for compile-time assets.
        /// </summary>
        [Required]
        public string TargetFramework { get; set; }
        /// <summary>
        /// RID to use for runtime assets (may be empty)
        /// </summary>
        public string RuntimeIdentifier { get; set; }
        #endregion

        protected override void ExecuteCore()
        {
            if (string.IsNullOrEmpty(ProjectAssetsFile))
            {
                return; // can't check RuntimeIdentifier without ProjectAssetsFile
            }

            if (string.IsNullOrEmpty(RuntimeIdentifier))
            {
                return; // can't check RuntimeIdentifier if it is null
            }

            if (LockFileHasMatchingRuntimeIdentifier())
            {
                return;
            }

            ThrowRuntimeIdentifierMismatchError();
        }

        private bool LockFileHasMatchingRuntimeIdentifier()
        {
            var lockFile = new LockFileCache(this).GetLockFile(ProjectAssetsFile);
            var target = lockFile.GetTargetAndReturnNullIfNotFound(TargetFramework, RuntimeIdentifier);
            return target != null;
        }

        private void ThrowRuntimeIdentifierMismatchError()
        {
            var ridMismatchMessage = string.Format(Strings.AssetsFileRuntimeIdentifierMismatch, RuntimeIdentifier);
            throw new BuildErrorException(ridMismatchMessage);
        }
    }
}


