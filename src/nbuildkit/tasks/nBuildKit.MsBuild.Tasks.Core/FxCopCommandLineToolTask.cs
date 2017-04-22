//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that work with the FxCop command line tool.
    /// </summary>
    public abstract class FxCopCommandLineToolTask : CommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FxCopCommandLineToolTask"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        protected FxCopCommandLineToolTask(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the full path to the directory that contains the 'FxCopCmd' executable.
        /// </summary>
        [Required]
        public ITaskItem FxCopDirectory
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
            var exePath = Path.Combine(GetAbsolutePath(FxCopDirectory), "FxCopCmd.exe");
            DataReceivedEventHandler standardOutputhandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };

            var exitCode = InvokeCommandLineTool(
                exePath,
                arguments,
                standardOutputHandler: standardOutputhandler,
                standardErrorHandler: DefaultErrorHandler);
            if (exitCode != 0)
            {
                if (!WarningsAsErrors)
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} exited with exit code: {1}. Build will continue because errors are assumed to be warnings. To change this set WarningsAsErrors to 'true' in the settings file.",
                            Path.GetFileName(exePath),
                            exitCode));
                }
                else
                {
                    Log.LogError(
                        string.Empty,
                        ErrorCodeById(ErrorIdApplicationNonzeroExitCode),
                        ErrorIdApplicationNonzeroExitCode,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        Path.GetFileName(exePath),
                        exitCode);
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
