﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.DotNet.Cli.commands.restore {
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class LocalizableStrings {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LocalizableStrings() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("Microsoft.DotNet.Cli.commands.restore.LocalizableStrings", typeof(LocalizableStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string CommandDescription {
            get {
                return ResourceManager.GetString("CommandDescription", resourceCulture);
            }
        }
        
        public static string ToolPathOptionName {
            get {
                return ResourceManager.GetString("ToolPathOptionName", resourceCulture);
            }
        }
        
        public static string ToolPathOptionDescription {
            get {
                return ResourceManager.GetString("ToolPathOptionDescription", resourceCulture);
            }
        }
        
        public static string InvalidPackageWarning {
            get {
                return ResourceManager.GetString("InvalidPackageWarning", resourceCulture);
            }
        }
        
        public static string PackageIdColumn {
            get {
                return ResourceManager.GetString("PackageIdColumn", resourceCulture);
            }
        }
        
        public static string VersionColumn {
            get {
                return ResourceManager.GetString("VersionColumn", resourceCulture);
            }
        }
        
        public static string CommandsColumn {
            get {
                return ResourceManager.GetString("CommandsColumn", resourceCulture);
            }
        }
        
        public static string CommandsMismatch {
            get {
                return ResourceManager.GetString("CommandsMismatch", resourceCulture);
            }
        }
        
        public static string InvalidToolPathOption {
            get {
                return ResourceManager.GetString("InvalidToolPathOption", resourceCulture);
            }
        }
        
        public static string AddSourceOptionDescription {
            get {
                return ResourceManager.GetString("AddSourceOptionDescription", resourceCulture);
            }
        }
        
        public static string AddSourceOptionName {
            get {
                return ResourceManager.GetString("AddSourceOptionName", resourceCulture);
            }
        }
        
        public static string ConfigFileOptionDescription {
            get {
                return ResourceManager.GetString("ConfigFileOptionDescription", resourceCulture);
            }
        }
        
        public static string ConfigFileOptionName {
            get {
                return ResourceManager.GetString("ConfigFileOptionName", resourceCulture);
            }
        }
        
        public static string ManifestPathOptionName {
            get {
                return ResourceManager.GetString("ManifestPathOptionName", resourceCulture);
            }
        }
        
        public static string ManifestPathOptionDescription {
            get {
                return ResourceManager.GetString("ManifestPathOptionDescription", resourceCulture);
            }
        }
        
        public static string VersionOptionDescription {
            get {
                return ResourceManager.GetString("VersionOptionDescription", resourceCulture);
            }
        }
        
        public static string VersionOptionName {
            get {
                return ResourceManager.GetString("VersionOptionName", resourceCulture);
            }
        }
        
        public static string PackageFailedToRestore {
            get {
                return ResourceManager.GetString("PackageFailedToRestore", resourceCulture);
            }
        }
        
        public static string PackagesCommandNameCollision {
            get {
                return ResourceManager.GetString("PackagesCommandNameCollision", resourceCulture);
            }
        }
        
        public static string RestoreSuccessful {
            get {
                return ResourceManager.GetString("RestoreSuccessful", resourceCulture);
            }
        }
        
        public static string RestorePartiallySuccessful {
            get {
                return ResourceManager.GetString("RestorePartiallySuccessful", resourceCulture);
            }
        }
        
        public static string RestoreFailed {
            get {
                return ResourceManager.GetString("RestoreFailed", resourceCulture);
            }
        }
    }
}
