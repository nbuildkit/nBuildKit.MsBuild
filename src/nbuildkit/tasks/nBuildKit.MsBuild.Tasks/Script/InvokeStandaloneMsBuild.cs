//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes MsBuild in a separate process.
    /// </summary>
    public sealed class InvokeStandaloneMsBuild : MsBuildCommandLineToolTask
    {
        private static string GetOutputPath(XmlElement element)
        {
            var node = element.SelectSingleNode("path") as XmlElement;
            if (node == null)
            {
                return string.Empty;
            }

            return node.InnerText;
        }

        private static string GetProjectPath(XmlElement element)
        {
            var node = element.SelectSingleNode("project") as XmlElement;
            if (node == null)
            {
                return string.Empty;
            }

            return node.InnerText;
        }

        private static string GetTarget(XmlElement element)
        {
            var node = element.SelectSingleNode("target") as XmlElement;
            if (node == null)
            {
                return string.Empty;
            }

            return node.InnerText;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeStandaloneMsBuild"/> class.
        /// </summary>
        public InvokeStandaloneMsBuild()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeStandaloneMsBuild"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public InvokeStandaloneMsBuild(IApplicationInvoker invoker)
            : base(invoker)
        {
            ShowDetailedSummary = true;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // Generate the MsBuild script in the temp directory
            var tempDir = GetAbsolutePath(TemporaryDirectory);
            var scriptPath = Path.Combine(
                tempDir,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.msbuild",
                    Guid.NewGuid().ToString()));
            var targetOutputPath = Path.Combine(
                tempDir,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.xml",
                    Guid.NewGuid().ToString()));
            GenerateIntermediateMsBuildScript(scriptPath, targetOutputPath);

            var arguments = new List<string>();
            {
                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "\"{0}\"",
                        scriptPath));
            }

            var exitCode = InvokeMsBuild(arguments);
            if (exitCode != 0)
            {
                return false;
            }

            // Read the generated XML file if it exists
            var outputs = new List<ITaskItem>();
            if (File.Exists(targetOutputPath))
            {
                /*
                    Expecting the xml to look like:

                    <?xml version="1.0" encoding="utf-8"?>
                    <msbuildresults>
                        <targetoutput>
                            <path></path>
                            <project></project>
                            <target></target>
                        </targetoutput>
                    </msbuildresults>
                */
                var doc = new XmlDocument
                {
                    XmlResolver = null,
                };

                var reader = new XmlTextReader(new StreamReader(targetOutputPath))
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                };
                using (reader)
                {
                    doc.Load(reader);
                }

                var nodes = doc.SelectNodes("/msbuildresults/targetoutput");
                foreach (var node in nodes)
                {
                    var element = node as XmlElement;
                    if (element == null)
                    {
                        continue;
                    }

                    var output = GetOutputPath(element);
                    var project = GetProjectPath(element);
                    var target = GetTarget(element);

                    var taskItem = new TaskItem(output);
                    taskItem.SetMetadata("MSBuildSourceProjectFile", project);
                    taskItem.SetMetadata("MSBuildSourceTargetName", target);

                    outputs.Add(taskItem);
                }
            }

            TargetOutputs = outputs.ToArray();

            return !Log.HasLoggedErrors;
        }

        private void GenerateIntermediateMsBuildScript(string path, string targetOutputPath)
        {
            var text = EmbeddedResourceExtracter.LoadEmbeddedTextFile(Assembly.GetExecutingAssembly(), "nBuildKit.MsBuild.Tasks.Script.MsBuildTemplate.xml");
            text = text.Replace("${TOOLS_VERSION}$", ToolsVersion);
            text = text.Replace("${OUTPUT_PATH}$", targetOutputPath);
            text = text.Replace("${PROJECTS}$", string.Join(";", Projects.Select(p => GetAbsolutePath(p))));
            text = text.Replace("${TARGETS}$", !string.IsNullOrEmpty(Targets) ? Targets : string.Empty);
            text = text.Replace("${RUN_TARGETS_SEPARATELY}$", RunEachTargetSeparately.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("${SKIP_NONEXISTANT_PROJECTS}$", SkipNonexistentProjects.ToString(CultureInfo.InvariantCulture));
            text = text.Replace("${STOP_ON_FIRST_FAILURE}$", StopOnFirstFailure.ToString(CultureInfo.InvariantCulture));

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = Path.GetDirectoryName(targetOutputPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllLines(path, new[] { text }, Encoding.UTF8);
        }

        /// <summary>
        /// Gets or sets the outputs of the built targets from all the project files. Only the outputs from the targets that were specified
        /// are returned, not any outputs that may exist on targets that those targets depend on.
        /// The TargetOutputs parameter also contains the following metadata:
        /// - MSBuildSourceProjectFile: The MSBuild project file that contains the target that set the outputs.
        /// - MSBuildSourceTargetName: The target that set the outputs. Note: If you want to identify the outputs from each project file
        /// or target separately, run the MSBuild task separately for each project file or target.If you run the MSBuild task only once to
        /// build all the project files, the outputs of all the targets are collected into one array.
        /// </summary>
        [Output]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] TargetOutputs
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
    }
}
