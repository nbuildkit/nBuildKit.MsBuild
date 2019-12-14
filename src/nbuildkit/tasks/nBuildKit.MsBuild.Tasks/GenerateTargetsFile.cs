//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.AppDomains;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="Task"/> that generates a targets file from a given <see cref="Assembly"/> that
    /// contains one or more <see cref="ITask"/> implementations.
    /// </summary>
    public sealed class GenerateTargetsFile : BaseTask
    {
        private const string DefaultNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        private const string ErrorIdFailedToLoadTypes = "NBuildKit.GenerateTargets.FailedToLoadTypes";
        private const string ErrorIdFailedToSaveFile = "NBuildKit.GenerateTargets.FailedToSaveFile";

        private static void AppendUsingTask(XmlNode node, string assemblyFilePropertyName, string typeName)
        {
            /*
                Create a node similar to

                <UsingTask
                    AssemblyFile="$(FileTasksAssembly)"
                    Condition="Exists('$(FileTasksAssembly)')"
                    TaskName="NBuildKit.MsBuild.Tasks.AddOrUpdateAttributeInCode" />
            */

            var doc = node.OwnerDocument;

            var usingTaskNode = doc.CreateElement("UsingTask", DefaultNamespace);
            node.AppendChild(usingTaskNode);

            var assemblyFileAttribute = doc.CreateAttribute("AssemblyFile");
            assemblyFileAttribute.Value = string.Format(
                CultureInfo.InvariantCulture,
                "$({0})",
                assemblyFilePropertyName);
            usingTaskNode.Attributes.Append(assemblyFileAttribute);

            var conditionAttribute = doc.CreateAttribute("Condition");
            conditionAttribute.Value = string.Format(
                CultureInfo.InvariantCulture,
                "Exists('$({0})')",
                assemblyFilePropertyName);
            usingTaskNode.Attributes.Append(conditionAttribute);

            var taskNameAttribute = doc.CreateAttribute("TaskName");
            taskNameAttribute.Value = typeName;
            usingTaskNode.Attributes.Append(taskNameAttribute);
        }

        private static AppDomain CreateAppDomain(string applicationBase)
        {
            return AppDomainBuilder.Assemble(
                "nBuildKit.MsBuild Task scanning AppDomain",
                AppDomainResolutionPaths.WithFilesAndDirectories(
                    Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                    new List<string>
                    {
                        // the nBuildKit task assembly
                        AppDomainBuilder.LocalFilePath(Assembly.GetExecutingAssembly()),
                    },
                    new List<string>
                    {
                        // The directory in which the newly created task assembly lives
                        applicationBase,
                    }));
        }

        /// <summary>
        /// Gets or sets the assembly file from which the targets file should be generated.
        /// </summary>
        [Required]
        public ITaskItem AssemblyFile
        {
            get;
            set;
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catching to log. Letting MsBuild handle the rest.")]
        public override bool Execute()
        {
            var filePath = GetAbsolutePath(AssemblyFile);
            if (string.IsNullOrEmpty(filePath))
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
                    "No input file provided");
                return false;
            }

            if (!File.Exists(filePath))
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
                    "Input File '{0}' cannot be found",
                    AssemblyFile);
                return false;
            }

            var outputPath = GetAbsolutePath(TargetsFile);
            if (string.IsNullOrEmpty(outputPath))
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
                    "No output file provided");
                return false;
            }

            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            }

            /*
                Create a file similar to:

                <?xml version="1.0" encoding="utf-8" ?>
                <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

                    <PropertyGroup>
                        <ExistsExtensions>true</ExistsExtensions>
                        <FileTasksAssembly>$(MSBuildThisFileDirectory)nBuildKit.MsBuild.Tasks.dll</FileTasksAssembly>
                    </PropertyGroup>

                    <UsingTask
                        AssemblyFile="$(FileTasksAssembly)"
                        Condition="Exists('$(FileTasksAssembly)')"
                        TaskName="NBuildKit.MsBuild.Tasks.AddOrUpdateAttributeInCode" />

                    <!-- MORE HERE -->
                </Project>
            */

            var doc = new XmlDocument
            {
                XmlResolver = null,
            };

            var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            var root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            var projectNode = doc.CreateElement(string.Empty, "Project", DefaultNamespace);
            doc.AppendChild(projectNode);

            var propertyGroupNode = doc.CreateElement("PropertyGroup", DefaultNamespace);
            projectNode.AppendChild(propertyGroupNode);

            var existsExtensionsNode = doc.CreateElement(ExtensionsProperty, DefaultNamespace);
            existsExtensionsNode.InnerText = "true";
            propertyGroupNode.AppendChild(existsExtensionsNode);

            var assemblyFilePropertyName = Path.GetFileNameWithoutExtension(filePath).Replace(".", string.Empty);
            var filePropertyNode = doc.CreateElement(assemblyFilePropertyName, DefaultNamespace);
            filePropertyNode.InnerText = string.Format(
                CultureInfo.InvariantCulture,
                "{0}(MSBuildThisFileDirectory){1}",
                "$",
                Path.GetFileName(filePath));
            propertyGroupNode.AppendChild(filePropertyNode);

            try
            {
                var taskTypes = GetTaskTypes(filePath);
                foreach (var typeName in taskTypes)
                {
                    AppendUsingTask(projectNode, assemblyFilePropertyName, typeName);
                }
            }
            catch (Exception e)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdFailedToLoadTypes),
                    ErrorIdFailedToLoadTypes,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Failed to extract the Task types from the assembly at {0}. Error was: {1}",
                    filePath,
                    e);
            }

            try
            {
                doc.Save(outputPath);
            }
            catch (Exception)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdFailedToSaveFile),
                    ErrorIdFailedToSaveFile,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Failed to save the targets file to {0}",
                    outputPath);
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the name of the property that should be used to indicate that the extensions targets file is loaded.
        /// </summary>
        [Required]
        public string ExtensionsProperty
        {
            get;
            set;
        }

        private IEnumerable<string> GetTaskTypes(string assemblyPath)
        {
            var domain = CreateAppDomain(Path.GetDirectoryName(assemblyPath));
            try
            {
                var loader = domain.CreateInstanceAndUnwrap(
                    typeof(RemoteAssemblyScannerLoader).Assembly.FullName,
                    typeof(RemoteAssemblyScannerLoader).FullName) as RemoteAssemblyScannerLoader;

                var scannerProxy = loader.Load(Log);
                return scannerProxy.Scan(assemblyPath);
            }
            finally
            {
                if ((domain != null) && !AppDomain.CurrentDomain.Equals(domain))
                {
                    AppDomain.Unload(domain);
                }
            }
        }

        /// <summary>
        /// Gets or sets the path to the location where the targets file should be written to.
        /// </summary>
        [Required]
        [Output]
        public ITaskItem TargetsFile
        {
            get;
            set;
        }

        private sealed class RemoteAssemblyScanner : MarshalByRefObject
        {
            private readonly TaskLoggingHelper _logger;

            public RemoteAssemblyScanner(TaskLoggingHelper logger)
            {
                _logger = logger;
            }

            private Assembly LoadAssembly(string file)
            {
                if (file == null)
                {
                    return null;
                }

                if (file.Length == 0)
                {
                    return null;
                }

                if (!File.Exists(file))
                {
                    return null;
                }

                var fileName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    return Assembly.Load(fileName);
                }
                catch (FileNotFoundException)
                {
                    // The file does not exist. Only possible if somebody removes the file
                    // between the check and the loading.
                    Log.LogError(
                        string.Empty,
                        "NBK0300",
                        "NBuildKit.FileNotFound",
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "The assembly file containing the MsBuild tasks was expected to be at {0} but it could not be found.",
                        fileName);
                }
                catch (FileLoadException)
                {
                    Log.LogError(
                        string.Empty,
                        "NBK0355",
                        "NBuildKit.GenerateTargets.FailedToLoadAssembly",
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "The assembly file containing the MsBuild tasks at {0} could not be loaded.",
                        fileName);
                }
                catch (BadImageFormatException)
                {
                    Log.LogError(
                        string.Empty,
                        "NBK0355",
                        "NBuildKit.GenerateTargets.FailedToLoadAssembly",
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "The assembly file containing the MsBuild tasks at {0} was invalid.",
                        fileName);
                }

                return null;
            }

            public TaskLoggingHelper Log
            {
                get
                {
                    return _logger;
                }
            }

            [SuppressMessage(
                "Microsoft.Design",
                "CA1031:DoNotCatchGeneralExceptionTypes",
                Justification = "Assembly loading can throw many exceptions. Don't know which ones they are.")]
            public List<string> Scan(string assemblyFileToScan)
            {
                if (assemblyFileToScan == null)
                {
                    throw new ArgumentNullException(nameof(assemblyFileToScan));
                }

                try
                {
                    var assembly = LoadAssembly(assemblyFileToScan);
                    return assembly.GetTypes()
                        .Where(t => t.IsPublic && t.IsClass && !t.IsAbstract && typeof(ITask).IsAssignableFrom(t))
                        .Select(t => t.FullName)
                        .ToList();
                }
                catch (Exception e)
                {
                    Log.LogError(
                        string.Empty,
                        "NBK0356",
                        ErrorIdFailedToLoadTypes,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "Failed to get the Task types from the assembly at: {0}. Error was: {1}",
                        assemblyFileToScan,
                        e);

                    return new List<string>();
                }
            }
        }

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "This class is instantiated via AppDomain.CreateInstanceAndUnwrap.")]
        private sealed class RemoteAssemblyScannerLoader : MarshalByRefObject
        {
            [SuppressMessage(
                "Microsoft.Performance",
                "CA1822:MarkMembersAsStatic",
                Justification = "This class is injected into a remote AppDomain in order to create a new RemoteAssemblyScanner instance. Should stay an instance method.")]
            public RemoteAssemblyScanner Load(TaskLoggingHelper logger)
            {
                return new RemoteAssemblyScanner(logger);
            }
        }
    }
}
