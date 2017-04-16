//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that work with the GIT command line tool.
    /// </summary>
    public abstract class GitCommandLineToolTask : CommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitCommandLineToolTask"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        protected GitCommandLineToolTask(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Invokes the GIT command line tool with the given arguments in the provided workspace directory.
        /// </summary>
        /// <param name="arguments">The collection of arguments.</param>
        /// <returns>The output of the GIT process</returns>
        protected string GetGitOutput(IEnumerable<string> arguments)
        {
            var text = new StringBuilder();
            DataReceivedEventHandler standardOutputHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    text.Append(e.Data);
                }
            };

            var exitCode = InvokeGit(arguments, standardOutputHandler: standardOutputHandler);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        System.IO.Path.GetFileName(GitExecutablePath.ItemSpec),
                        exitCode));
                Log.LogError(string.Format(CultureInfo.InvariantCulture, "Output was: {0}", text));
            }

            return text.ToString();
        }

        /// <summary>
        /// Gets or sets the path to the GIT command line executable.
        /// </summary>
        [Required]
        public ITaskItem GitExecutablePath
        {
            get;
            set;
        }

        /// <summary>
        /// Invokes the GIT command line tool with the given arguments in the provided workspace directory.
        /// </summary>
        /// <param name="arguments">The collection of arguments.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <returns>The output of the GIT process</returns>
        protected int InvokeGit(IEnumerable<string> arguments, DataReceivedEventHandler standardOutputHandler = null)
        {
            if (standardOutputHandler == null)
            {
                standardOutputHandler = (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };
            }

            DataReceivedEventHandler standardErrorHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Log.LogWarning(e.Data);
                }
            };

            var exitCode = InvokeCommandLineTool(
                GitExecutablePath,
                arguments,
                Workspace,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        System.IO.Path.GetFileName(GitExecutablePath.ItemSpec),
                        exitCode));
            }

            return exitCode;
        }

        /// <summary>
        /// Gets or sets the full path to the workspace that contains the .git directory.
        /// </summary>
        [Required]
        public ITaskItem Workspace
        {
            get;
            set;
        }
    }
}
