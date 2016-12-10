//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Nuclei.AppDomains;
using Nuclei;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="Task"/> that generates a targets file from a given <see cref="Assembly"/> that
    /// contains one or more <see cref="ITask"/> implementations.
    /// </summary>
    public sealed class GenerateTargetsFile : NBuildKitMsBuildTask
    {
        private static void AppendUsingTask(XmlNode node, string typeName)
        {
            /*
                Create a node similar to

                <UsingTask
                    AssemblyFile="$(FileTasksAssembly)"
                    Condition="Exists('$(FileTasksAssembly)')"
                    TaskName="NBuildKit.MsBuild.Tasks.AddOrUpdateAttributeInCode" />
            */

            var doc = node.OwnerDocument;

            var usingTaskNode = doc.CreateElement(string.Empty, "UsingTask", string.Empty);
            node.AppendChild(usingTaskNode);

            var assemblyFileAttribute = doc.CreateAttribute("AssemblyFile");
            assemblyFileAttribute.Value = "$(FileTasksAssembly)";
            usingTaskNode.Attributes.Append(assemblyFileAttribute);

            var conditionAttribute = doc.CreateAttribute("Condition");
            conditionAttribute.Value = "Exists('$(FileTasksAssembly)')";
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
                    Assembly.GetExecutingAssembly().LocalDirectoryPath(),
                    new List<string>(),
                    new List<string>
                    {
                        applicationBase
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
        public override bool Execute()
        {
            var filePath = GetAbsolutePath(AssemblyFile);
            if (string.IsNullOrEmpty(filePath))
            {
                Log.LogError("No input file provided");
                return false;
            }

            if (!File.Exists(filePath))
            {
                Log.LogError("Input File '{0}' cannot be found", AssemblyFile);
                return false;
            }

            var outputPath = GetAbsolutePath(TargetsFile);
            if (string.IsNullOrEmpty(outputPath))
            {
                Log.LogError("No output file provided");
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

            var doc = new XmlDocument();

            var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            var root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            var projectNode = doc.CreateElement(string.Empty, "Project", "http://schemas.microsoft.com/developer/msbuild/2003");
            doc.AppendChild(projectNode);

            var propertyGroupNode = doc.CreateElement(string.Empty, "PropertyGroup", string.Empty);
            projectNode.AppendChild(propertyGroupNode);

            var existsExtensionsNode = doc.CreateElement(string.Empty, "ExistsExtensions", string.Empty);
            existsExtensionsNode.InnerText = "true";
            propertyGroupNode.AppendChild(existsExtensionsNode);

            var filePropertyNode = doc.CreateElement(string.Empty, "FileTasksAssembly", string.Empty);
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
                    AppendUsingTask(projectNode, typeName);
                }
            }
            catch (Exception e)
            {
                Log.LogError("Failed to extract the Task types from the assembly at {0}. Error was: {1}", filePath, e);
            }

            try
            {
                doc.Save(outputPath);
            }
            catch (Exception)
            {
                Log.LogError("Failed to save the targets file to {0}", outputPath);
            }

            return !Log.HasLoggedErrors;
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

                string fileName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    return Assembly.Load(fileName);
                }
                catch (FileNotFoundException)
                {
                    // The file does not exist. Only possible if somebody removes the file
                    // between the check and the loading.
                    Log.LogError(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The assembly file containing the MsBuild tasks was expected to be at {0} but it could not be found.",
                            fileName));
                }
                catch (FileLoadException)
                {
                    Log.LogError(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The assembly file containing the MsBuild tasks at {0} could not be loaded.",
                            fileName));
                }
                catch (BadImageFormatException)
                {
                    Log.LogError(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The assembly file containing the MsBuild tasks at {0} was invalid.",
                            fileName));
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

            public List<string> Scan(string assemblyFileToScan)
            {
                if (assemblyFileToScan == null)
                {
                    throw new ArgumentNullException("assemblyFilesToScan");
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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Failed to get the Task types from the assembly at: {0}. Error was: {1}",
                            assemblyFileToScan,
                            e));

                    return new List<string>();
                }
            }
        }

        private sealed class RemoteAssemblyScannerLoader : MarshalByRefObject
        {
            public RemoteAssemblyScanner Load(TaskLoggingHelper logger)
            {
                return new RemoteAssemblyScanner(logger);
            }
        }
    }
}
