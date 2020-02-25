// <copyright file="UpdateProjectSettings.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that updates the project settings like version numbers for a Visual Studio project.
    /// </summary>
    public sealed class UpdateProjectSettings : BaseTask
    {
        private const string ErrorIdFailedToExtractPublicKey = "NBuildKit.GenerateInternalsVisibleTo.FailedToExtractPublicKey";

        private const string MetadataValueTag = "ReplacementValue";
        private const string ProjectNodeName = "Project";

        private static void CreateOrUpdateNode(XElement projectNode, string nodeName, string content, ref XElement propertyGroupNode)
        {
            var versionNode = FindElementInPropertyGroup(projectNode, nodeName);
            if (versionNode is null)
            {
                if (propertyGroupNode is null)
                {
                    propertyGroupNode = new XElement("PropertyGroup");
                    projectNode.Add(propertyGroupNode);
                }

                versionNode = new XElement(nodeName);
                propertyGroupNode.Add(versionNode);
            }

            versionNode.Value = content;
        }

        private static string FindAssemblyInfoFile(string projectPath)
        {
            var projectLevelAssemblyInfoFiles = Directory.GetFiles(
                Path.GetDirectoryName(projectPath),
                "AssemblyInfo.*",
                SearchOption.TopDirectoryOnly);
            if (string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var propertiesDirectory = Path.Combine(Path.GetDirectoryName(projectPath), "Properties");
                if (Directory.Exists(propertiesDirectory))
                {
                    return projectLevelAssemblyInfoFiles
                        .Concat(
                            Directory.GetFiles(
                                propertiesDirectory,
                                "AssemblyInfo.*",
                                SearchOption.TopDirectoryOnly))
                        .FirstOrDefault();
                }
            }

            if (string.Equals(Path.GetExtension(projectPath), ".vbproj", StringComparison.OrdinalIgnoreCase))
            {
                var myProjectDirectory = Path.Combine(Path.GetDirectoryName(projectPath), "My Project");
                if (Directory.Exists(myProjectDirectory))
                {
                    return projectLevelAssemblyInfoFiles
                        .Concat(
                            Directory.GetFiles(
                                myProjectDirectory,
                                "AssemblyInfo.*",
                                SearchOption.TopDirectoryOnly))
                        .FirstOrDefault();
                }
            }

            return projectLevelAssemblyInfoFiles.FirstOrDefault();
        }

        private static string FindAssemblyName(XDocument doc, string projectPath)
        {
            // The document may have a namespace
            var ns = doc.Root.GetDefaultNamespace().NamespaceName;

            var namespacedProjectName = XName.Get(ProjectNodeName, ns);
            var projectNode = doc.Element(namespacedProjectName);

            var namespacedAssemblyName = XName.Get("AssemblyName", ns);
            var assemblyNameNode = projectNode.Descendants(namespacedAssemblyName).FirstOrDefault();

            if (assemblyNameNode != null)
            {
                return assemblyNameNode.Value;
            }

            return Path.GetFileNameWithoutExtension(projectPath);
        }

        private static XElement FindElementInPropertyGroup(XElement parent, string elementName)
        {
            return parent.Descendants(elementName).FirstOrDefault();
        }

        private static bool ShouldUseAssemblyInfoFile(XDocument doc)
        {
            // The document may have a namespace
            var ns = doc.Root.GetDefaultNamespace();

            var namespacedProjectName = XName.Get(ProjectNodeName, ns.NamespaceName);
            var projectNode = doc.Element(namespacedProjectName);

            var namespacedSdkAttribute = XName.Get("Sdk", ns.NamespaceName);
            var sdkAttribute = projectNode.Attribute(namespacedSdkAttribute);

            var useAssemblyInfo = false;
            if (sdkAttribute is null)
            {
                // Old project format. Go the AssemblyInfo way
                useAssemblyInfo = true;
            }
            else
            {
                // New project format. See if we redirect to the assembly info file
                var generateAssemblyInfoNode = FindElementInPropertyGroup(projectNode, "GenerateAssemblyInfo");
                if (generateAssemblyInfoNode != null)
                {
                    var innerText = generateAssemblyInfoNode.Value;
                    if (!string.IsNullOrWhiteSpace(innerText))
                    {
                        if (bool.TryParse(innerText, out var shouldNotUseAssemblyInfo))
                        {
                            useAssemblyInfo = !shouldNotUseAssemblyInfo;
                        }
                    }
                }
            }

            return useAssemblyInfo;
        }

        private static void UpdateProject(XDocument document, string projectPath, Dictionary<string, string> tokens)
        {
            var projectNode = document.Element(ProjectNodeName);
            XElement propertyGroupNode = null;

            // Update the project with the appropriate attributes
            // AssemblyInformationalVersion AND NuGet version -> Version
            CreateOrUpdateNode(
                projectNode,
                "Version",
                tokens.TryGetValue("VersionSemantic", out var value) ? value : string.Empty,
                ref propertyGroupNode);

            // AU AssemblyVersion -> AssemblyVersion
            CreateOrUpdateNode(
                projectNode,
                "AssemblyVersion",
                tokens.TryGetValue("VersionAssembly", out value) ? value : string.Empty,
                ref propertyGroupNode);

            // AU AssemblyFileVersion -> FileVersion
            CreateOrUpdateNode(
                projectNode,
                "FileVersion",
                tokens.TryGetValue("VersionAssemblyFile", out value) ? value : string.Empty,
                ref propertyGroupNode);

            // AU AssemblyFileVersion -> FileVersion
            CreateOrUpdateNode(
                projectNode,
                "InformationalVersion",
                tokens.TryGetValue("VersionProduct", out value) ? value : string.Empty,
                ref propertyGroupNode);

            // AU AssemblyCompany -> Company
            CreateOrUpdateNode(
                projectNode,
                "Company",
                tokens.TryGetValue("CompanyName", out value) ? value : string.Empty,
                ref propertyGroupNode);

            // AU AssemblyCopyright -> Copyright
            CreateOrUpdateNode(
                projectNode,
                "Copyright",
                tokens.TryGetValue("CopyrightLong", out value) ? value : string.Empty,
                ref propertyGroupNode);

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "  ",
                WriteEndDocumentOnClose = true,
            };

            using (var xw = XmlWriter.Create(projectPath, settings))
            {
                document.Save(xw);
            }
        }

        private readonly IApplicationInvoker _invoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProjectSettings"/> class.
        /// </summary>
        public UpdateProjectSettings()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProjectSettings"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public UpdateProjectSettings(IApplicationInvoker invoker)
        {
            _invoker = invoker ?? new ApplicationInvoker(new MsBuildLogger(Log));
        }

        /// <summary>
        /// Gets or sets the encoding for the AssemblyInfo file.
        /// </summary>
        public string AssemblyInfoEncoding
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the build information attributes should be added
        /// to the AssemblyInfo file.
        /// </summary>
        [Required]
        public bool GenerateBuildInformation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of assemblies which should be able to
        /// see the internals of their 'friend' assemblies.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] InternalsVisibleTo
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the compiler directive that is used to indicate that an InternalsVisibleTo attribute
        /// should be included.
        /// </summary>
        [Required]
        public string InternalsVisibleToCompilerDirective
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the packages directory into which all NuGet packages are expanded.
        /// </summary>
        [Required]
        public ITaskItem PackagesDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the project file which should be updated.
        /// </summary>
        [Required]
        public ITaskItem Project
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the strong name tool (sn.exe).
        /// </summary>
        [Required]
        public ITaskItem SnExe
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to a directory into which temporary files may be created.
        /// </summary>
        [Required]
        public ITaskItem TemporaryDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of tokens.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Tokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the event handler that processes data from the data stream, or standard output stream, of
        /// the command line application.By default logs a message for each output.
        /// </summary>
        private DataReceivedEventHandler DefaultDataHandler
        {
            get
            {
                return (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(e.Data);
                    }
                };
            }
        }

        private DataReceivedEventHandler DefaultErrorHandler
        {
            get
            {
                // Fix for the issue reported here: https://github.com/Microsoft/msbuild/issues/397
                var encoding = Console.OutputEncoding;
                return (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        // If the error stream encoding is UTF8
                        // it is possible that the error stream contains the BOM marker for UTF-8
                        // So even if the error stream is actually empty, we still get something in
                        // it, which means we'll fail.
                        if (Encoding.UTF8.Equals(encoding) && (e.Data.Length == 1))
                        {
                            return;
                        }

                        Log.LogError(
                            string.Empty,
                            ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationErrorStream),
                            Core.ErrorInformation.ErrorIdApplicationErrorStream,
                            string.Empty,
                            0,
                            0,
                            0,
                            0,
                            e.Data);
                    }
                };
            }
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA2204:Literals should be spelled correctly",
            MessageId = "AssemblyInfo",
            Justification = "References the AssemblyInfo file.")]
        public override bool Execute()
        {
            var projectPath = GetAbsolutePath(Project);
            if (string.IsNullOrEmpty(projectPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No project file path provided.");
                return false;
            }

            if (!File.Exists(projectPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Expected the project to be at: '{0}' but no such file exists",
                    projectPath);
                return false;
            }

            // If the project is an old Visual Studio project then update the AssemblyInfo file
            //
            // If the project is a new Visual Studio project then we
            // - Check if the version number elements are in the project file -> if so, overwrite them
            // - Check if we redirect to the AssemblyInfo file -> if so, update the AssemblyInfo file
            var doc = XDocument.Load(projectPath, LoadOptions.None);
            var useAssemblyInfo = ShouldUseAssemblyInfoFile(doc);

            var assemblyInfoPath = FindAssemblyInfoFile(projectPath);
            var tokens = TokensToCollection();

            var encoding = Encoding.ASCII;
            if (!string.IsNullOrWhiteSpace(AssemblyInfoEncoding))
            {
                encoding = Encoding.GetEncoding(AssemblyInfoEncoding);
            }

            if (useAssemblyInfo)
            {
                if (string.IsNullOrWhiteSpace(assemblyInfoPath) || !File.Exists(assemblyInfoPath))
                {
                    Log.LogError(
                        string.Empty,
                        ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                        Core.ErrorInformation.ErrorIdFileNotFound,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "Expected the project at: '{0}' to have an AssemblyInfo file but no such file exists",
                        projectPath);

                    return false;
                }

                var attributesToUpdate = new List<Tuple<string, string, bool>>
                {
                    Tuple.Create(
                        "AssemblyVersion",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            tokens.TryGetValue("VersionAssembly", out var value) ? value : string.Empty),
                        true),
                    Tuple.Create(
                        "AssemblyFileVersion",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            tokens.TryGetValue("VersionAssemblyFile", out value) ? value : string.Empty),
                        true),
                    Tuple.Create(
                        "AssemblyInformationalVersion",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            tokens.TryGetValue("VersionProduct", out value) ? value : string.Empty),
                        true),
                    Tuple.Create(
                        "AssemblyCompany",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            tokens.TryGetValue("CompanyName", out value) ? value : string.Empty),
                        true),
                    Tuple.Create(
                        "AssemblyCopyright",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            tokens.TryGetValue("CopyrightLong", out value) ? value : string.Empty),
                        true),
                    Tuple.Create(
                        "AssemblyConfiguration",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            tokens.TryGetValue("Configuration", out value) ? value : string.Empty),
                        true),
                };

                UpdateAssemblyInfo(assemblyInfoPath, encoding, attributesToUpdate);
            }
            else
            {
                UpdateProject(doc, projectPath, tokens);
            }

            if (GenerateBuildInformation && !string.IsNullOrWhiteSpace(assemblyInfoPath) && File.Exists(assemblyInfoPath))
            {
                var attributesToUpdate = new List<Tuple<string, string, bool>>
                {
                    Tuple.Create(
                        "Nuclei.Build.AssemblyBuildTime",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "buildTime: \"{0}\"",
                            DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture)),
                        true),
                    Tuple.Create(
                        "Nuclei.Build.AssemblyBuildInformation",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "buildNumber: {0}, versionControlInformation: \"{1}\"",
                            tokens.TryGetValue("VersionBuild", out var value) ? value : string.Empty,
                            tokens.TryGetValue("VcsRevision", out value) ? value : string.Empty),
                        true),
                };

                UpdateAssemblyInfo(assemblyInfoPath, encoding, attributesToUpdate);
            }

            if ((InternalsVisibleTo != null) && (InternalsVisibleTo.Length > 0))
            {
                // Find the current project in the list
                WriteInternalsVisibleToAttributes(doc, projectPath, assemblyInfoPath, encoding);
            }

            return !Log.HasLoggedErrors;
        }

        private string ExtractPublicKeyFromAssemblyFile(string assemblyFromPackage)
        {
            var snExeFileName = GetAbsolutePath(SnExe);
            Log.LogMessage(MessageImportance.Normal, "Extracting public key from assembly file: " + assemblyFromPackage);

            var assemblyPath = Directory.EnumerateFiles(GetAbsolutePath(PackagesDirectory), assemblyFromPackage, SearchOption.AllDirectories)
                .OrderBy(k => Path.GetDirectoryName(k))
                .LastOrDefault();
            if (string.IsNullOrEmpty(assemblyPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Failed to find the full path of: " + assemblyFromPackage);
                return null;
            }

            var text = new StringBuilder();
            DataReceivedEventHandler standardOutputHandler =
                (s, e) =>
                {
                    text.Append(e.Data);
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Low, e.Data);
                    }
                };
            var exitCode = _invoker.Invoke(
                snExeFileName,
                new[] { string.Format(CultureInfo.InvariantCulture, "-Tp \"{0}\"", assemblyPath.TrimEnd('\\')) },
                standardOutputHandler: standardOutputHandler);

            if (exitCode != 0)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                    Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "{0} exited with a non-zero exit code while trying to extract the public key from a signed assembly. Exit code was: {1}",
                    snExeFileName,
                    exitCode);
                return null;
            }

            var publicKeyText = text.ToString();
            if (string.IsNullOrEmpty(publicKeyText))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                    Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Failed to extract public key from assembly.");
                return null;
            }

            const string startString = "Public key (hash algorithm: sha1):";
            const string endString = "Public key token is";
            var startIndex = publicKeyText.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
            var endIndex = publicKeyText.IndexOf(endString, StringComparison.OrdinalIgnoreCase);
            return publicKeyText.Substring(startIndex + startString.Length, endIndex - (startIndex + startString.Length));
        }

        private string ExtractPublicKeyFromKeyFile(string keyFile)
        {
            var snExeFileName = Path.GetFileName(GetAbsolutePath(SnExe));
            Log.LogMessage(MessageImportance.Normal, "Extracting public key from key file: " + keyFile);

            var tempDir = GetAbsolutePath(TemporaryDirectory);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            var publicKeyFile = Path.Combine(tempDir, Path.GetRandomFileName());
            try
            {
                {
                    var arguments = new[] { string.Format(CultureInfo.InvariantCulture, "-p \"{0}\" \"{1}\"", keyFile, publicKeyFile) };
                    var exitCode = _invoker.Invoke(
                        snExeFileName,
                        arguments,
                        standardOutputHandler: DefaultDataHandler,
                        standardErrorHandler: DefaultErrorHandler);

                    if (exitCode != 0)
                    {
                        Log.LogError(
                            string.Empty,
                            ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                            Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
                            string.Empty,
                            0,
                            0,
                            0,
                            0,
                            "{0} exited with a non-zero exit code while trying to extract the public key file from the signing key file. Exit code was: {1}",
                            snExeFileName,
                            exitCode);
                        return null;
                    }
                }

                var text = new StringBuilder();
                {
                    var arguments = new[] { string.Format(CultureInfo.InvariantCulture, "-tp \"{0}\"", publicKeyFile) };
                    DataReceivedEventHandler standardOutputHandler = (s, e) =>
                    {
                        text.Append(e.Data);
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            Log.LogMessage(MessageImportance.Low, e.Data);
                        }
                    };
                    var exitCode = _invoker.Invoke(
                        snExeFileName,
                        arguments,
                        standardOutputHandler: standardOutputHandler,
                        standardErrorHandler: DefaultErrorHandler);
                    if (exitCode != 0)
                    {
                        Log.LogError(
                            string.Empty,
                            ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                            Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
                            string.Empty,
                            0,
                            0,
                            0,
                            0,
                            "{0} exited with a non-zero exit code while trying to extract the public key information from the public key file. Exit code was: {1}",
                            snExeFileName,
                            exitCode);
                        return null;
                    }
                }

                var publicKeyText = text.ToString();
                if (string.IsNullOrEmpty(publicKeyText))
                {
                    Log.LogError(
                        string.Empty,
                        ErrorCodeById(ErrorIdFailedToExtractPublicKey),
                        ErrorIdFailedToExtractPublicKey,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "Failed to extract public key from key file.");
                    return null;
                }

                const string startString = "Public key (hash algorithm: sha1):";
                const string endString = "Public key token is";
                var startIndex = publicKeyText.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
                var endIndex = publicKeyText.IndexOf(endString, StringComparison.OrdinalIgnoreCase);
                return publicKeyText.Substring(startIndex + startString.Length, endIndex - (startIndex + startString.Length));
            }
            finally
            {
                if (File.Exists(publicKeyFile))
                {
                    File.Delete(publicKeyFile);
                }
            }
        }

        private Dictionary<string, string> TokensToCollection()
        {
            var toReplace = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Tokens != null)
            {
                ITaskItem[] processedTokens = Tokens;
                for (int i = 0; i < processedTokens.Length; i++)
                {
                    ITaskItem taskItem = processedTokens[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        toReplace.Add(taskItem.ItemSpec, taskItem.GetMetadata(MetadataValueTag));
                    }
                }
            }

            return toReplace;
        }

        private void UpdateAssemblyInfo(string filePath, Encoding encoding, IEnumerable<Tuple<string, string, bool>> attributesToUpdate)
        {
            foreach (var tuple in attributesToUpdate)
            {
                AssemblyInfoExtensions.UpdateAssemblyAttribute(
                    filePath,
                    tuple.Item1,
                    tuple.Item2,
                    encoding,
                    new MsBuildLogger(Log),
                    tuple.Item3);
            }
        }

        private void WriteInternalsVisibleToAttributes(XDocument project, string projectPath, string assemblyInfoPath, Encoding encoding)
        {
            var assemblyName = FindAssemblyName(project, projectPath);
            Log.LogMessage(
                MessageImportance.Normal,
                "Determining if any InternalsVisibleTo attributes should be added for project at: {0} with assembly name: {1}",
                projectPath,
                assemblyName);

            var attributes = new List<Tuple<string, string>>();
            for (int i = 0; i < InternalsVisibleTo.Length; i++)
            {
                var taskItem = InternalsVisibleTo[i];

                // Expecting that the taskItems have:
                // - taskItem.ItemSpec:            Name of the assembly to include in the attribute
                // - taskItem.Projects:            Semi-colon separated list of projects for which the assembly should be added to the internals visible to list
                // - taskItem.KeyFile:             The full path to the key file that contains the strong name public key
                // - taskItem.AssemblyFromPackage: The file name of the assembly that should be included, noting that this assembly is found in the packages directory
                // - taskItem.PublicKey:           The full public key of the assembly
                if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                {
                    var projects = taskItem.GetMetadata("Projects");
                    if (string.IsNullOrEmpty(projects))
                    {
                        continue;
                    }

                    Log.LogMessage(
                        MessageImportance.Low,
                        "Projects to find: {0}",
                        projects);

                    var projectsAsArray = projects.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToArray();
                    if (!projectsAsArray.Any(p => string.Equals(p, assemblyName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Log.LogMessage(
                            MessageImportance.Low,
                            "InternalsVisibleTo for: {0} should be {1}",
                            taskItem.ItemSpec,
                            string.Join(",", projectsAsArray));
                        continue;
                    }

                    Log.LogMessage(MessageImportance.Normal, "Adding InternalsVisibleTo attribute for: " + taskItem.ItemSpec);

                    var key = string.Empty;

                    var publicKey = taskItem.GetMetadata("PublicKey");
                    if (!string.IsNullOrEmpty(publicKey))
                    {
                        Log.LogMessage(MessageImportance.Normal, "Using provided public key: " + publicKey);
                        key = publicKey;
                    }
                    else
                    {
                        var snExeFileName = Path.GetFileName(GetAbsolutePath(SnExe));

                        var keyFile = taskItem.GetMetadata("KeyFile");
                        if (!string.IsNullOrEmpty(keyFile))
                        {
                            key = ExtractPublicKeyFromKeyFile(keyFile);
                        }
                        else
                        {
                            var assemblyFromPackage = taskItem.GetMetadata("AssemblyFromPackage");
                            if (!string.IsNullOrEmpty(assemblyFromPackage))
                            {
                                key = ExtractPublicKeyFromAssemblyFile(assemblyFromPackage);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(key))
                    {
                        attributes.Add(Tuple.Create(taskItem.ItemSpec, key));
                    }
                    else
                    {
                        attributes.Add(Tuple.Create(taskItem.ItemSpec, string.Empty));
                    }
                }
            }

            if (attributes.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(assemblyInfoPath) || !File.Exists(assemblyInfoPath))
                {
                    // Don't do anything because there is no AssemblyInfo file
                    Log.LogError(
                        string.Empty,
                        ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                        Core.ErrorInformation.ErrorIdFileNotFound,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "Cannot set an InternalsVisibleTo attribute for the project at: '{0}' because there is no AssemblyInfo file",
                        projectPath);
                }

                AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                    assemblyInfoPath,
                    InternalsVisibleToCompilerDirective,
                    attributes,
                    encoding,
                    new MsBuildLogger(Log));
            }
        }
    }
}
