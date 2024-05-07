﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.NET.Build.Containers.Resources
{
    using System;


    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings
    {

        private static global::System.Resources.ResourceManager resourceMan;

        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings()
        {
        }

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.NET.Build.Containers.Resources.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER0000: Value for unit test {0}.
        /// </summary>
        internal static string _Test
        {
            get
            {
                return ResourceManager.GetString("_Test", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1002: Request to Amazon Elastic Container Registry failed prematurely. This is often caused when the target repository does not exist in the registry..
        /// </summary>
        internal static string AmazonRegistryFailed
        {
            get
            {
                return ResourceManager.GetString("AmazonRegistryFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2008: Both {0} and {1} were provided, but only one or the other is allowed..
        /// </summary>
        internal static string AmbiguousTags
        {
            get
            {
                return ResourceManager.GetString("AmbiguousTags", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2025: ContainerAppCommandArgs are provided without specifying a ContainerAppCommand..
        /// </summary>
        internal static string AppCommandArgsSetNoAppCommand
        {
            get
            {
                return ResourceManager.GetString("AppCommandArgsSetNoAppCommand", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2026: ContainerAppCommand and ContainerAppCommandArgs must be empty when ContainerAppCommandInstruction is &apos;{0}&apos;..
        /// </summary>
        internal static string AppCommandSetNotUsed
        {
            get
            {
                return ResourceManager.GetString("AppCommandSetNotUsed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to local archive at &apos;{0}&apos;.
        /// </summary>
        internal static string ArchiveRegistry_PushInfo
        {
            get
            {
                return ResourceManager.GetString("ArchiveRegistry_PushInfo", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2022: The base image has an entrypoint that will be overwritten to start the application. Set ContainerAppCommandInstruction to &apos;Entrypoint&apos; if this is desired. To preserve the base image entrypoint, set ContainerAppCommandInstruction to &apos;DefaultArgs&apos;..
        /// </summary>
        internal static string BaseEntrypointOverwritten
        {
            get
            {
                return ResourceManager.GetString("BaseEntrypointOverwritten", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2009: Could not parse {0}: {1}.
        /// </summary>
        internal static string BaseImageNameParsingFailed
        {
            get
            {
                return ResourceManager.GetString("BaseImageNameParsingFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2020: {0} does not specify a registry and will be pulled from Docker Hub. Please prefix the name with the image registry, for example: &apos;{1}/&lt;image&gt;&apos;..
        /// </summary>
        internal static string BaseImageNameRegistryFallback
        {
            get
            {
                return ResourceManager.GetString("BaseImageNameRegistryFallback", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2013: {0} had spaces in it, replacing with dashes..
        /// </summary>
        internal static string BaseImageNameWithSpaces
        {
            get
            {
                return ResourceManager.GetString("BaseImageNameWithSpaces", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1011: Couldn&apos;t find matching base image for {0} that matches RuntimeIdentifier {1}..
        /// </summary>
        internal static string BaseImageNotFound
        {
            get
            {
                return ResourceManager.GetString("BaseImageNotFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1001: Failed to upload blob using {0}; received status code &apos;{1}&apos;..
        /// </summary>
        internal static string BlobUploadFailed
        {
            get
            {
                return ResourceManager.GetString("BlobUploadFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Pushed image &apos;{0}&apos; to {1}..
        /// </summary>
        internal static string ContainerBuilder_ImageUploadedToLocalDaemon
        {
            get
            {
                return ResourceManager.GetString("ContainerBuilder_ImageUploadedToLocalDaemon", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Pushed image &apos;{0}&apos; to registry &apos;{1}&apos;..
        /// </summary>
        internal static string ContainerBuilder_ImageUploadedToRegistry
        {
            get
            {
                return ResourceManager.GetString("ContainerBuilder_ImageUploadedToRegistry", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Building image &apos;{0}&apos; with tags &apos;{1}&apos; on top of base image &apos;{2}&apos;..
        /// </summary>
        internal static string ContainerBuilder_StartBuildingImage
        {
            get
            {
                return ResourceManager.GetString("ContainerBuilder_StartBuildingImage", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1007: Could not deserialize token from JSON..
        /// </summary>
        internal static string CouldntDeserializeJsonToken
        {
            get
            {
                return ResourceManager.GetString("CouldntDeserializeJsonToken", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2012: Could not recognize registry &apos;{0}&apos;..
        /// </summary>
        internal static string CouldntRecognizeRegistry
        {
            get
            {
                return ResourceManager.GetString("CouldntRecognizeRegistry", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to local registry via &apos;{0}&apos;.
        /// </summary>
        internal static string DockerCli_PushInfo
        {
            get
            {
                return ResourceManager.GetString("DockerCli_PushInfo", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER3002: Failed to get docker info({0})\n{1}\n{2}.
        /// </summary>
        internal static string DockerInfoFailed
        {
            get
            {
                return ResourceManager.GetString("DockerInfoFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER3002: Failed to get docker info: {0}.
        /// </summary>
        internal static string DockerInfoFailed_Ex
        {
            get
            {
                return ResourceManager.GetString("DockerInfoFailed_Ex", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER3001: Failed creating {0} process..
        /// </summary>
        internal static string ContainerRuntimeProcessCreationFailed
        {
            get
            {
                return ResourceManager.GetString("ContainerRuntimeProcessCreationFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER4006: Property &apos;{0}&apos; is empty or contains whitespace and will be ignored..
        /// </summary>
        internal static string EmptyOrWhitespacePropertyIgnored
        {
            get
            {
                return ResourceManager.GetString("EmptyOrWhitespacePropertyIgnored", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER4004: Items &apos;{0}&apos; contain empty item(s) which will be ignored..
        /// </summary>
        internal static string EmptyValuesIgnored
        {
            get
            {
                return ResourceManager.GetString("EmptyValuesIgnored", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2023: A ContainerEntrypoint and ContainerAppCommandArgs are provided. ContainerAppInstruction must be set to configure how the application is started. Valid instructions are {0}..
        /// </summary>
        internal static string EntrypointAndAppCommandArgsSetNoAppCommandInstruction
        {
            get
            {
                return ResourceManager.GetString("EntrypointAndAppCommandArgsSetNoAppCommandInstruction", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2024: ContainerEntrypointArgs are provided without specifying a ContainerEntrypoint..
        /// </summary>
        internal static string EntrypointArgsSetNoEntrypoint
        {
            get
            {
                return ResourceManager.GetString("EntrypointArgsSetNoEntrypoint", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2029: ContainerEntrypointArgsSet are provided. Change to use ContainerAppCommandArgs for arguments that must always be set, or ContainerDefaultArgs for arguments that can be overridden when the container is created..
        /// </summary>
        internal static string EntrypointArgsSetPreferAppCommandArgs
        {
            get
            {
                return ResourceManager.GetString("EntrypointArgsSetPreferAppCommandArgs", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2028: ContainerEntrypoint can not be combined with ContainerAppCommandInstruction &apos;{0}&apos;..
        /// </summary>
        internal static string EntrypointConflictAppCommand
        {
            get
            {
                return ResourceManager.GetString("EntrypointConflictAppCommand", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2027: A ContainerEntrypoint is provided. ContainerAppInstruction must be set to configure how the application is started. Valid instructions are {0}..
        /// </summary>
        internal static string EntrypointSetNoAppCommandInstruction
        {
            get
            {
                return ResourceManager.GetString("EntrypointSetNoAppCommandInstruction", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1008: Failed retrieving credentials for &quot;{0}&quot;: {1}.
        /// </summary>
        internal static string FailedRetrievingCredentials
        {
            get
            {
                return ResourceManager.GetString("FailedRetrievingCredentials", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2030: GenerateLabels was disabled but GenerateDigestLabel was enabled - no digest label will be created.
        /// </summary>
        internal static string GenerateDigestLabelWithoutGenerateLabels
        {
            get
            {
                return ResourceManager.GetString("GenerateDigestLabelWithoutGenerateLabels", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No host object detected..
        /// </summary>
        internal static string HostObjectNotDetected
        {
            get
            {
                return ResourceManager.GetString("HostObjectNotDetected", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1009: Failed to load image from local registry. stdout: {0}.
        /// </summary>
        internal static string ImageLoadFailed
        {
            get
            {
                return ResourceManager.GetString("ImageLoadFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1010: Pulling images from local registry is not supported..
        /// </summary>
        internal static string ImagePullNotSupported
        {
            get
            {
                return ResourceManager.GetString("ImagePullNotSupported", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2015: {0}: &apos;{1}&apos; was not a valid Environment Variable. Ignoring..
        /// </summary>
        internal static string InvalidEnvVar
        {
            get
            {
                return ResourceManager.GetString("InvalidEnvVar", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2005: The inferred image name &apos;{0}&apos; contains entirely invalid characters. The valid characters for an image name are alphanumeric characters, -, /, or _, and the image name must start with an alphanumeric character..
        /// </summary>
        internal static string InvalidImageName_EntireNameIsInvalidCharacters
        {
            get
            {
                return ResourceManager.GetString("InvalidImageName_EntireNameIsInvalidCharacters", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2005: The first character of the image name &apos;{0}&apos; must be a lowercase letter or a digit and all characters in the name must be an alphanumeric character, -, /, or _..
        /// </summary>
        internal static string InvalidImageName_NonAlphanumericStartCharacter
        {
            get
            {
                return ResourceManager.GetString("InvalidImageName_NonAlphanumericStartCharacter", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2017: A ContainerPort item was provided with an invalid port number &apos;{0}&apos;. ContainerPort items must have an Include value that is an integer, and a Type value that is either &apos;tcp&apos; or &apos;udp&apos;..
        /// </summary>
        internal static string InvalidPort_Number
        {
            get
            {
                return ResourceManager.GetString("InvalidPort_Number", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2017: A ContainerPort item was provided with an invalid port number &apos;{0}&apos; and an invalid port type &apos;{1}&apos;. ContainerPort items must have an Include value that is an integer, and a Type value that is either &apos;tcp&apos; or &apos;udp&apos;..
        /// </summary>
        internal static string InvalidPort_NumberAndType
        {
            get
            {
                return ResourceManager.GetString("InvalidPort_NumberAndType", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2017: A ContainerPort item was provided with an invalid port type &apos;{0}&apos;. ContainerPort items must have an Include value that is an integer, and a Type value that is either &apos;tcp&apos; or &apos;udp&apos;..
        /// </summary>
        internal static string InvalidPort_Type
        {
            get
            {
                return ResourceManager.GetString("InvalidPort_Type", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2018: Invalid SDK prerelease version &apos;{0}&apos; - only &apos;rc&apos; and &apos;preview&apos; are supported..
        /// </summary>
        internal static string InvalidSdkPrereleaseVersion
        {
            get
            {
                return ResourceManager.GetString("InvalidSdkPrereleaseVersion", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2019: Invalid SDK semantic version &apos;{0}&apos;..
        /// </summary>
        internal static string InvalidSdkVersion
        {
            get
            {
                return ResourceManager.GetString("InvalidSdkVersion", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2007: Invalid {0} provided: {1}. Image tags must be alphanumeric, underscore, hyphen, or period..
        /// </summary>
        internal static string InvalidTag
        {
            get
            {
                return ResourceManager.GetString("InvalidTag", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2010: Invalid {0} provided: {1}. {0} must be a semicolon-delimited list of valid image tags. Image tags must be alphanumeric, underscore, hyphen, or period..
        /// </summary>
        internal static string InvalidTags
        {
            get
            {
                return ResourceManager.GetString("InvalidTags", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1003: Token response had neither token nor access_token..
        /// </summary>
        internal static string InvalidTokenResponse
        {
            get
            {
                return ResourceManager.GetString("InvalidTokenResponse", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER4005: Item &apos;{0}&apos; contains items without metadata &apos;Value&apos;, and they will be ignored..
        /// </summary>
        internal static string ItemsWithoutMetadata
        {
            get
            {
                return ResourceManager.GetString("ItemsWithoutMetadata", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Error while reading daemon config: {0}.
        /// </summary>
        internal static string LocalDocker_FailedToGetConfig
        {
            get
            {
                return ResourceManager.GetString("LocalDocker_FailedToGetConfig", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to The daemon server reported errors: {0}.
        /// </summary>
        internal static string LocalDocker_LocalDaemonErrors
        {
            get
            {
                return ResourceManager.GetString("LocalDocker_LocalDaemonErrors", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1012: The local registry is not available, but pushing to a local registry was requested..
        /// </summary>
        internal static string LocalRegistryNotAvailable
        {
            get
            {
                return ResourceManager.GetString("LocalRegistryNotAvailable", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2004: Unable to download layer with descriptor &apos;{0}&apos; from registry &apos;{1}&apos; because it does not exist..
        /// </summary>
        internal static string MissingLinkToRegistry
        {
            get
            {
                return ResourceManager.GetString("MissingLinkToRegistry", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2016: ContainerPort item &apos;{0}&apos; does not specify the port number. Please ensure the item&apos;s Include is a port number, for example &apos;&lt;ContainerPort Include=&quot;80&quot; /&gt;&apos;.
        /// </summary>
        internal static string MissingPortNumber
        {
            get
            {
                return ResourceManager.GetString("MissingPortNumber", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1004: No RequestUri specified..
        /// </summary>
        internal static string NoRequestUriSpecified
        {
            get
            {
                return ResourceManager.GetString("NoRequestUriSpecified", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; was not a valid container image name, it was normalized to &apos;{1}&apos;.
        /// </summary>
        internal static string NormalizedContainerName
        {
            get
            {
                return ResourceManager.GetString("NormalizedContainerName", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2011: {0} &apos;{1}&apos; does not exist.
        /// </summary>
        internal static string PublishDirectoryDoesntExist
        {
            get
            {
                return ResourceManager.GetString("PublishDirectoryDoesntExist", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploaded config to registry..
        /// </summary>
        internal static string Registry_ConfigUploaded
        {
            get
            {
                return ResourceManager.GetString("Registry_ConfigUploaded", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploading config to registry at blob &apos;{0}&apos;,.
        /// </summary>
        internal static string Registry_ConfigUploadStarted
        {
            get
            {
                return ResourceManager.GetString("Registry_ConfigUploadStarted", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Layer &apos;{0}&apos; already exists..
        /// </summary>
        internal static string Registry_LayerExists
        {
            get
            {
                return ResourceManager.GetString("Registry_LayerExists", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Finished uploading layer &apos;{0}&apos; to &apos;{1}&apos;..
        /// </summary>
        internal static string Registry_LayerUploaded
        {
            get
            {
                return ResourceManager.GetString("Registry_LayerUploaded", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploading layer &apos;{0}&apos; to &apos;{1}&apos;..
        /// </summary>
        internal static string Registry_LayerUploadStarted
        {
            get
            {
                return ResourceManager.GetString("Registry_LayerUploadStarted", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploaded manifest to &apos;{0}&apos;..
        /// </summary>
        internal static string Registry_ManifestUploaded
        {
            get
            {
                return ResourceManager.GetString("Registry_ManifestUploaded", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploading manifest to registry &apos;{0}&apos; as blob &apos;{1}&apos;..
        /// </summary>
        internal static string Registry_ManifestUploadStarted
        {
            get
            {
                return ResourceManager.GetString("Registry_ManifestUploadStarted", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploaded tag &apos;{0}&apos; to &apos;{1}&apos;..
        /// </summary>
        internal static string Registry_TagUploaded
        {
            get
            {
                return ResourceManager.GetString("Registry_TagUploaded", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Uploading tag &apos;{0}&apos; to &apos;{1}&apos;..
        /// </summary>
        internal static string Registry_TagUploadStarted
        {
            get
            {
                return ResourceManager.GetString("Registry_TagUploadStarted", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1017: Unable to communicate with the registry &apos;{0}&apos;..
        /// </summary>
        internal static string RegistryOperationFailed
        {
            get
            {
                return ResourceManager.GetString("RegistryOperationFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1013: Failed to push to the output registry: {0}.
        /// </summary>
        internal static string RegistryOutputPushFailed
        {
            get
            {
                return ResourceManager.GetString("RegistryOutputPushFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1014: Manifest pull failed..
        /// </summary>
        internal static string RegistryPullFailed
        {
            get
            {
                return ResourceManager.GetString("RegistryPullFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1005: Registry push failed; received status code &apos;{0}&apos;..
        /// </summary>
        internal static string RegistryPushFailed
        {
            get
            {
                return ResourceManager.GetString("RegistryPushFailed", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1015: Unable to access the repository &apos;{0}&apos; at tag &apos;{1}&apos; in the registry &apos;{2}&apos;. Please confirm that this name and tag are present in the registry..
        /// </summary>
        internal static string RepositoryNotFound
        {
            get
            {
                return ResourceManager.GetString("RepositoryNotFound", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER4003: Required &apos;{0}&apos; items contain empty items..
        /// </summary>
        internal static string RequiredItemsContainsEmptyItems
        {
            get
            {
                return ResourceManager.GetString("RequiredItemsContainsEmptyItems", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER4002: Required &apos;{0}&apos; items were not set..
        /// </summary>
        internal static string RequiredItemsNotSet
        {
            get
            {
                return ResourceManager.GetString("RequiredItemsNotSet", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER4001: Required property &apos;{0}&apos; was not set or empty..
        /// </summary>
        internal static string RequiredPropertyNotSetOrEmpty
        {
            get
            {
                return ResourceManager.GetString("RequiredPropertyNotSetOrEmpty", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1006: Too many retries, stopping..
        /// </summary>
        internal static string TooManyRetries
        {
            get
            {
                return ResourceManager.GetString("TooManyRetries", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER1016: Unable to access the repository &apos;{0}&apos; in the registry &apos;{1}&apos;. Please confirm your credentials are correct and that you have access to this repository and registry..
        /// </summary>
        internal static string UnableToAccessRepository
        {
            get
            {
                return ResourceManager.GetString("UnableToAccessRepository", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2021: Unknown AppCommandInstruction &apos;{0}&apos;. Valid instructions are {1}..
        /// </summary>
        internal static string UnknownAppCommandInstruction
        {
            get
            {
                return ResourceManager.GetString("UnknownAppCommandInstruction", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2002: Unknown local registry type &apos;{0}&apos;. Valid local container registry types are {1}..
        /// </summary>
        internal static string UnknownLocalRegistryType
        {
            get
            {
                return ResourceManager.GetString("UnknownLocalRegistryType", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2003: The manifest for {0}:{1} from registry {2} was an unknown type: {3}. Please raise an issue at https://github.com/dotnet/sdk-container-builds/issues with this message..
        /// </summary>
        internal static string UnknownMediaType
        {
            get
            {
                return ResourceManager.GetString("UnknownMediaType", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to CONTAINER2001: Unrecognized mediaType &apos;{0}&apos;..
        /// </summary>
        internal static string UnrecognizedMediaType
        {
            get
            {
                return ResourceManager.GetString("UnrecognizedMediaType", resourceCulture);
            }
        }
    }
}
