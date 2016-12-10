//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that work with the FxCop command line tool.
    /// </summary>
    public abstract class FxCopCommandLineToolTask : CommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the full path to the directory that contains the 'FxCopCmd' executable.
        /// </summary>
        [Required]
        public ITaskItem FxCopDir
        {
            get;
            set;
        }

        /// <summary>
        /// Invokes the FxCop commandline application with the provided arguments.
        /// </summary>
        /// <param name="arguments">The collection containing the arguments.</param>
        protected void InvokeFxCop(IEnumerable<string> arguments)
        {
            var exePath = Path.Combine(GetAbsolutePath(FxCopDir), "FxCopCmd.exe");
            DataReceivedEventHandler standardOutputhandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };

            DataReceivedEventHandler standardErrorHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogError(e.Data);
                    }
                };
            var exitCode = InvokeCommandlineTool(
                exePath,
                arguments,
                standardOutputHandler: standardOutputhandler,
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                if (!WarningsAsErrors)
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        string.Format(
                            "{0} exited with exit code: {1}. Build will continue because errors are assumed to be warnings. To change this set FxCopWarningsAsErrors to 'true' in the settings file.",
                            Path.GetFileName(exePath),
                            exitCode));
                }
                else
                {
                    Log.LogError(
                        string.Format(
                            "{0} exited with a non-zero exit code. Exit code was: {1}",
                            Path.GetFileName(exePath),
                            exitCode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the full path to the FxCop log file.
        /// </summary>
        [Required]
        public ITaskItem OutputFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether warnings should be treated as errors.
        /// </summary>
        public bool WarningsAsErrors
        {
            get;
            set;
        }
    }
}