//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that work with the GIT command line tool.
    /// </summary>
    public abstract class GitCommandLineToolTask : CommandLineToolTask
    {
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
        /// <returns>The output of the GIT process</returns>
        protected string InvokeGit(IEnumerable<string> arguments)
        {
            var text = new StringBuilder();
            DataReceivedEventHandler standardOutputHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    text.Append(e.Data);
                }
            };

            DataReceivedEventHandler standardErrorHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Log.LogWarning(e.Data);
                }
            };

            var exitCode = InvokeCommandlineTool(
                GitExecutablePath,
                arguments,
                Workspace,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        System.IO.Path.GetFileName(GitExecutablePath.ItemSpec),
                        exitCode));
                Log.LogError(string.Format("Output was: {0}", text));

                return null;
            }

            return text.ToString();
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