//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that invoke MsBuild in a separate process.
    /// </summary>
    public abstract class MsBuildCommandLineToolTask : CommandLineToolTask
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

        private static IEnumerable<string> GetPotentialMsBuildPaths()
        {
            // Use vswhere to find MsBuild: https://github.com/microsoft/vswhere/wiki/Find-MSBuild
            var arguments = new string[]
            {
                "-latest",
                "-requires Microsoft.Component.MSBuild",
                "-find MSBuild\\**\\Bin\\MSBuild.exe",
            };

            var result = new List<string>();
            DataReceivedEventHandler standardOutputHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        result.Add(e.Data);
                    }
                };

            var errorsReported = false;
            DataReceivedEventHandler standardErrorHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        errorsReported = true;
                    }
                };

            var argumentBuilder = new StringBuilder();
            {
                foreach (var argument in arguments)
                {
                    argumentBuilder.Append(argument);
                    argumentBuilder.Append(" ");
                }
            }

            var info = new ProcessStartInfo
            {
                FileName = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%\\Microsoft Visual Studio\\Installer\\vswhere.exe"),
                Arguments = argumentBuilder.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            using (var process = new Process())
            {
                process.StartInfo = info;
                process.OutputDataReceived += standardOutputHandler;
                process.ErrorDataReceived += standardErrorHandler;

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0 || errorsReported)
                {
                    return Array.Empty<string>();
                }

                return result;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsBuildCommandLineToolTask"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        protected MsBuildCommandLineToolTask(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        private Hashtable GetCommandLineProperties()
        {
            var regex = new Regex(
                @"(\/)(p|property)(:)(.*)",
                RegexOptions.IgnoreCase
                | RegexOptions.Multiline
                | RegexOptions.Compiled
                | RegexOptions.Singleline);

            var propertyArguments = new List<string>();
            var commandLineArguments = Environment.GetCommandLineArgs();

            Log.LogMessage(
                MessageImportance.Low,
                "Searching for additional properties provided to current MsBuild instance ...");
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                var argument = commandLineArguments[i];
                Log.LogMessage(
                    MessageImportance.Low,
                    "Searching: {0}",
                    argument);

                var propertyMatch = regex.Match(argument);
                if (propertyMatch.Success)
                {
                    var property = propertyMatch.Groups[4].Value;

                    Log.LogMessage(
                        MessageImportance.Low,
                        "Adding command line property key-value pair: {0}",
                        property);
                    propertyArguments.Add(property);
                }
            }

            // Add the user provided properties
            if (Properties != null)
            {
                foreach (var propertyPair in Properties)
                {
                    var property = propertyPair.ItemSpec;
                    Log.LogMessage(
                        MessageImportance.Low,
                        "Adding user provided property key-value pair: {0}",
                        property);

                    propertyArguments.Add(property);
                }
            }

            Hashtable result;
            if (!PropertyParser.GetTableWithEscaping(Log, "GlobalProperties", "Properties", propertyArguments.ToArray(), out result))
            {
                return new Hashtable();
            }

            return result;
        }

        /// <summary>
        /// Invokes MsBuild with the given additional arguments.
        /// </summary>
        /// <param name="instanceArguments">The instance specific arguments.</param>
        /// <returns>The exit code of the process.</returns>
        protected int InvokeMsBuild(IEnumerable<string> instanceArguments)
        {
            var commandLineProperties = GetCommandLineProperties();
            var arguments = new List<string>();
            {
                arguments.Add("/nodeReuse:false");
                arguments.Add("/nologo");

                if (ShowDetailedSummary && CurrentInstanceHasDetailedSummary())
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

                var verbosity = VerbosityForCurrentMsBuildInstance();
                if (!string.IsNullOrEmpty(verbosity))
                {
                    arguments.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "/verbosity:{0}",
                            verbosity));
                }

                foreach (DictionaryEntry pair in commandLineProperties)
                {
                    var propertyName = string.Format(
                        CultureInfo.InvariantCulture,
                        "/P:{0}=",
                        pair.Key);

                    // Only add the command line property if there is no user version of the property.
                    if (instanceArguments.FirstOrDefault(s => s.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        arguments.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}\"{1}\"",
                                propertyName,
                                EscapingUtilities.UnescapeAll(pair.Value as string).TrimEnd(new[] { '\\' })));
                    }
                }

                arguments.AddRange(instanceArguments);
            }

            var msbuildPath = Process.GetCurrentProcess().MainModule.FileName;
            if (!string.Equals(Path.GetFileNameWithoutExtension(msbuildPath), "msbuild", StringComparison.OrdinalIgnoreCase))
            {
                msbuildPath = _potentialMsBuildPaths.FirstOrDefault();
                if (string.IsNullOrEmpty(msbuildPath))
                {
                    Log.LogError(
                        string.Empty,
                        ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationPathNotFound),
                        Core.ErrorInformation.ErrorIdApplicationPathNotFound,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "Could not locate a suitable version of MsBuild.");
                    return 9009; // Generally this seems to be the exit code presented when the executable cannot be found.
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
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                    Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    Path.GetFileName(msbuildPath),
                    exitCode);
            }

            return exitCode;
        }

        /// <summary>
        /// Gets or sets the full paths to the MsBuild scripts, Visual Studio solutions or Visual Studio project files that should
        /// be executed.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Projects
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the additional MsBuild properties.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
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
        /// Gets or sets a value indicating whether the detailed summary should be displayed or not.
        /// </summary>
        protected bool ShowDetailedSummary
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
        /// Gets or sets a semi-colon separated list of targets that should be used.
        /// </summary>
        public string Targets
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
