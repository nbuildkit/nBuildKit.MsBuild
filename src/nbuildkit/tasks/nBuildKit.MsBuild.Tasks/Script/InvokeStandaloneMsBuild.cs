//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Nuclei;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes MsBuild in a separate process.
    /// </summary>
    public sealed class InvokeStandaloneMsBuild : CommandLineToolTask
    {
        private static IEnumerable<string> _potentialMsBuildPaths = GetPotentialMsBuildPaths();

        private static bool CurrentInstanceHasDetailedSummary()
        {
            const string FullVerbositySwitch = "/detailedsummary:";
            const string ShortVerbositySwitch = "/ds:";

            var commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                var argument = commandLineArguments[i];
                if (argument.Contains(FullVerbositySwitch))
                {
                    return true;
                }

                if (argument.Contains(ShortVerbositySwitch))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetOutputPath(XmlElement element)
        {
            var node = element.SelectSingleNode("path") as XmlElement;
            if (node == null)
            {
                return string.Empty;
            }

            return node.InnerText;
        }

        private static IEnumerable<string> GetPotentialMsBuildPaths()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!Environment.Is64BitOperatingSystem)
            {
                programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }

            var msbuildBasePath = Path.Combine(programFilesPath, "MSBuild");
            return Directory.GetFiles(msbuildBasePath, "msbuild.exe", SearchOption.AllDirectories)
                .OrderByDescending(f => f)
                .ToList();
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

        private static string GetVerbosityForCurrentInstance()
        {
            const string FullVerbositySwitch = "/verbosity:";
            const string ShortVerbositySwitch = "/v:";

            var commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                var argument = commandLineArguments[i];

                var hasSwitch = false;
                var remainder = string.Empty;
                if (argument.Contains(FullVerbositySwitch))
                {
                    hasSwitch = true;
                    remainder = argument.Substring(FullVerbositySwitch.Length).Trim();
                }

                if (argument.Contains(ShortVerbositySwitch))
                {
                    hasSwitch = true;
                    remainder = argument.Substring(ShortVerbositySwitch.Length).Trim();
                }

                if (hasSwitch)
                {
                    if (!string.IsNullOrEmpty(remainder))
                    {
                        return remainder;
                    }
                    else
                    {
                        if (commandLineArguments.Length > i + 1)
                        {
                            return commandLineArguments[i + 1].Trim();
                        }
                    }
                }
            }

            return "normal";
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
                arguments.Add("/nodeReuse:false");
                arguments.Add("/nologo");

                if (CurrentInstanceHasDetailedSummary())
                {
                    arguments.Add("/detailedsummary");
                }

                if (!string.IsNullOrEmpty(ToolsVersion))
                {
                    arguments.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "/toolsversion:{0}",
                            ToolsVersion));
                }

                var verbosity = GetVerbosityForCurrentInstance();
                if (!string.IsNullOrEmpty(verbosity))
                {
                    arguments.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "/verbosity:{0}",
                            verbosity));
                }

                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "\"{0}\"",
                        scriptPath));
            }

            var msbuildPath = Process.GetCurrentProcess().MainModule.FileName;
            if (!string.Equals(Path.GetFileNameWithoutExtension(msbuildPath), "msbuild", StringComparison.OrdinalIgnoreCase))
            {
                msbuildPath = _potentialMsBuildPaths.FirstOrDefault();
                if (string.IsNullOrEmpty(msbuildPath))
                {
                    Log.LogError("Could not locate a suitable version of MsBuild.");
                    return false;
                }
            }

            var workingDirectory = GetAbsolutePath(WorkingDirectory);
            var exitCode = InvokeCommandLineTool(
                msbuildPath,
                arguments,
                workingDirectory);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        Path.GetFileName(msbuildPath),
                        exitCode));
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
                var doc = new XmlDocument();
                doc.Load(targetOutputPath);
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
            var text = EmbeddedResourceExtracter.LoadEmbeddedTextFile(Assembly.GetExecutingAssembly(), "NBuildKit.MsBuild.Tasks.Script.MsBuildTemplate.xml");
            text = text.Replace("${TOOLS_VERSION}$", ToolsVersion);
            text = text.Replace("${OUTPUT_PATH}$", targetOutputPath);
            text = text.Replace("${PROJECTS}$", string.Join(";", Projects.Select(p => GetAbsolutePath(p))));
            text = text.Replace("${PROPERTIES}$", (Properties != null) ? string.Join(";", Properties.Select(p => p.ItemSpec)) : string.Empty);
            text = text.Replace("${TARGETS}$", !string.IsNullOrEmpty(Targets) ? Targets : string.Empty);
            text = text.Replace("${RUN_TARGETS_SEPARATELY}$", RunEachTargetSeparately.ToString());
            text = text.Replace("${SKIP_NONEXISTANT_PROJECTS}$", SkipNonexistentProjects.ToString());
            text = text.Replace("${STOP_ON_FIRST_FAILURE}$", StopOnFirstFailure.ToString());

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
        /// Gets or sets the full paths to the MsBuild scripts, Visual Studio solutions or Visual Studio project files that should
        /// be executed.
        /// </summary>
        [Required]
        public ITaskItem[] Projects
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the additional MsBuild properties
        /// </summary>
        public ITaskItem[] Properties
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the MSBuild task should invokes each target in the list passed to MSBuild one at a time,
        /// instead of at the same time. Setting this parameter to true guarantees that subsequent targets are invoked even if previously
        /// invoked targets failed. Otherwise, a build error would stop invocation of all subsequent targets. Default is false.
        /// </summary>
        public bool RunEachTargetSeparately
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not missing projects should be skipped. If missing projects
        /// are not skipped an error will be raised for each missing project. Default is false.
        /// </summary>
        public bool SkipNonexistentProjects
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the process should stop on the first error or continue.
        /// Default is false.
        /// </summary>
        public bool StopOnFirstFailure
        {
            get;
            set;
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
        public ITaskItem[] TargetOutputs
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a semi-colon separated list of targets that should be used.
        /// </summary>
        public string Targets
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
        /// Gets or sets the version of MsBuild and the build tools that should be used.
        /// </summary>
        public string ToolsVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the working directory.
        /// </summary>
        public ITaskItem WorkingDirectory
        {
            get;
            set;
        }
    }
}
