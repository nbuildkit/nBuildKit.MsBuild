// <copyright file="UpdateProjectSettings.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            return Directory.GetFiles(
                    Path.GetDirectoryName(projectPath),
                    "AssemblyInfo.*",
                    SearchOption.TopDirectoryOnly)
                .Concat(
                    Directory.GetFiles(
                        Path.Combine(Path.GetDirectoryName(projectPath), "Properties"),
                        "AssemblyInfo.*",
                        SearchOption.TopDirectoryOnly))
                .FirstOrDefault();
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

            document.Save(projectPath);
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
        /// Gets or sets the path to the project file which should be updated.
        /// </summary>
        [Required]
        public ITaskItem Project
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of tokens.
        /// </summary>
        [Required]
        public ITaskItem[] Tokens
        {
            get;
            set;
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
                        "AssemblyBuildTime",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "buildTime: \"{0}\"",
                            DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture)),
                        true),
                    Tuple.Create(
                        "AssemblyBuildInformation",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "buildNumber: {0}, versionControlInformation: \"{1}\"",
                            tokens.TryGetValue("VersionBuild", out var value) ? value : string.Empty,
                            tokens.TryGetValue("VcsRevision", out value) ? value : string.Empty),
                        true),
                };

                UpdateAssemblyInfo(assemblyInfoPath, encoding, attributesToUpdate);
            }

            return !Log.HasLoggedErrors;
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
                    (importance, message) => Log.LogMessage(importance, message),
                    tuple.Item3);
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
    }
}
