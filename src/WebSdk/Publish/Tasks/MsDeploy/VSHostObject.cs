﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.NET.Sdk.Publish.Tasks.MsDeploy
{
    internal class VSHostObject
    {
        IEnumerable<ITaskItem> _hostObject;
        public VSHostObject(IEnumerable<ITaskItem> hostObject)
        {
            _hostObject = hostObject;
        }

        public bool ExtractCredentials(out string username, out string password)
        {
            bool retVal = false;
            username = password = string.Empty;
            if (_hostObject != null)
            {
                ITaskItem credentialItem = _hostObject.FirstOrDefault<ITaskItem>(p => p.ItemSpec == VSMsDeployTaskHostObject.CredentailItemSpecName);
                if (credentialItem != null)
                {
                    retVal = true;
                    username = credentialItem.GetMetadata(VSMsDeployTaskHostObject.UserMetaDataName);
                    if (!string.IsNullOrEmpty(username))
                    {
                        password = credentialItem.GetMetadata(VSMsDeployTaskHostObject.PasswordMetaDataName);
                    }
                }
            }
            return retVal;
        }

        public void GetFileSkips(out ITaskItem[] srcSkips, out ITaskItem[] destSkips)
        {
            srcSkips = null;
            destSkips = null;
            if (_hostObject != null)
            {
                IEnumerable<ITaskItem> items;

                items = from item in _hostObject
                        where (item.ItemSpec == VSMsDeployTaskHostObject.SkipFileItemSpecName
                            && (item.GetMetadata(VSMsDeployTaskHostObject.SkipApplyMetadataName) == VSMsDeployTaskHostObject.SourceDeployObject ||
                                string.IsNullOrEmpty(item.GetMetadata(VSMsDeployTaskHostObject.SkipApplyMetadataName)))
                            )
                        select item;
                srcSkips = items.ToArray();

                items = from item in _hostObject
                        where (item.ItemSpec == VSMsDeployTaskHostObject.SkipFileItemSpecName
                            && (item.GetMetadata(VSMsDeployTaskHostObject.SkipApplyMetadataName) == VSMsDeployTaskHostObject.DestinationDeployObject ||
                                string.IsNullOrEmpty(item.GetMetadata(VSMsDeployTaskHostObject.SkipApplyMetadataName)))
                            )
                        select item;
                destSkips = items.ToArray();
            }
        }
    }
}
