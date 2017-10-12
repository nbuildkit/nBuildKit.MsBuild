//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes a tool through node.js.
    /// </summary>
    public sealed class InvokeNodeTool : CommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeNodeTool"/> class.
        /// </summary>
        public InvokeNodeTool()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeNodeTool"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public InvokeNodeTool(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the paths that should be added to the PATH environment variable.
        /// </summary>
        public ITaskItem[] AdditionalEnvironmentPaths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments for the node tool invocation.
        /// </summary>
        [Required]
        public string Arguments
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // If the node tool is installed on the machine there will be a 'cmd' type script
            // that can be used to invoke it. In that case we are probably passed the path to this cmd script
            // or just the name of the tool, e.g. 'npm'. In that case we just execute the tool with cmd (running
            // it straight with System.Diagnostics.Process doesn't work, it complains about the node tool not being
            // a windows executable).
            //
            // If we got passed the path to a js file then we assume that the tools are a local install and
            // we invoke them through node.
            var isJsFile = Path.GetExtension(ToolPath.ItemSpec).Equals(".js");
            var toolFileName = (!isJsFile)
                ? "cmd.exe"
                : GetFullToolPath(NodeExecutablePath);
            var toolArguments = (!isJsFile)
                ? string.Format(CultureInfo.InvariantCulture, "/c {0} {1}", ToolPath, Arguments)
                : string.Format(CultureInfo.InvariantCulture, "{0} {1}", ToolPath, Arguments);

            DataReceivedEventHandler standardErrorHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        if (IgnoreErrors)
                        {
                            Log.LogWarning(e.Data);
                        }
                        else
                        {
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
                    }
                };
            var exitCode = InvokeCommandLineTool(
                toolFileName,
                new[] { toolArguments },
                GetAbsolutePath(WorkingDirectory),
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                var text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    Path.GetFileName(NodeExecutablePath.ItemSpec),
                    exitCode);
                if (IgnoreExitCode)
                {
                    Log.LogWarning(text);
                }
                else
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
                        text);
                }
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets a value indicating whether error should be ignored.
        /// </summary>
        [Required]
        public bool IgnoreErrors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the exit code should be ignored.
        /// </summary>
        [Required]
        public bool IgnoreExitCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the node.js executable.
        /// </summary>
        [Required]
        public ITaskItem NodeExecutablePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the node tool executable, e.g. npm.
        /// </summary>
        [Required]
        public ITaskItem ToolPath
        {
            get;
            set;
        }

        /// <summary>
        /// Updates the environment variables for the application prior to execution.
        /// </summary>
        /// <param name="environmentVariables">
        ///     The environment variables for the application. The environment variables for the process can be
        ///     changed by altering the collection.
        /// </param>
        protected override void UpdateEnvironmentVariables(StringDictionary environmentVariables)
        {
            if (environmentVariables == null)
            {
                return;
            }

            environmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");

            var nodeWorkingDirectory = Path.GetDirectoryName(GetFullToolPath(NodeExecutablePath));
            if (!string.IsNullOrWhiteSpace(nodeWorkingDirectory))
            {
                environmentVariables["PATH"] += ";" + nodeWorkingDirectory;
            }

            if ((AdditionalEnvironmentPaths != null) && (AdditionalEnvironmentPaths.Length > 0))
            {
                foreach (var path in AdditionalEnvironmentPaths)
                {
                    environmentVariables["PATH"] += ";" + GetAbsolutePath(path);
                }
            }
        }

        /// <summary>
        /// Gets or sets the full path to the working directory.
        /// </summary>
        [Required]
        public ITaskItem WorkingDirectory
        {
            get;
            set;
        }
    }
}
