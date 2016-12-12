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

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that work with the NuGet command line tool.
    /// </summary>
    public abstract class NuGetCommandLineToolTask : CommandLineToolTask
    {
        /// <summary>
        /// Invokes the GIT command line tool with the given arguments in the provided workspace directory.
        /// </summary>
        /// <param name="arguments">The collection of arguments.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <returns>The output of the GIT process</returns>
        protected int InvokeNuGet(IEnumerable<string> arguments, DataReceivedEventHandler standardOutputHandler = null)
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
                    Log.LogError(e.Data);
                }
            };

            var exitCode = InvokeCommandLineTool(
                NuGetExecutablePath,
                arguments,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        System.IO.Path.GetFileName(NuGetExecutablePath.ItemSpec),
                        exitCode));
            }

            return exitCode;
        }

        /// <summary>
        /// Gets or sets the path to the NuGet command line executable.
        /// </summary>
        [Required]
        public ITaskItem NuGetExecutablePath
        {
            get;
            set;
        }
    }
}
