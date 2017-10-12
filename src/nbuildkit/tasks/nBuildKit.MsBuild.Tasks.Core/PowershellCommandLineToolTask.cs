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
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that invoke Powershell.
    /// </summary>
    public abstract class PowershellCommandLineToolTask : CommandLineToolTask
    {
        private ITaskItem _powershellExePath = new TaskItem(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");

        /// <summary>
        /// Initializes a new instance of the <see cref="PowershellCommandLineToolTask"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        protected PowershellCommandLineToolTask(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets the event handler that processes data from the data stream, or standard output stream, of
        /// the command line application.By default logs a message for each output.
        /// </summary>
        protected override DataReceivedEventHandler DefaultErrorHandler
        {
            get
            {
                return (s, e) =>
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
            }
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

        private void InvokePowershell(
            string powershellArgument,
            ITaskItem workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            var arguments = new List<string>();
            {
                arguments.Add("-NoLogo ");
                arguments.Add("-NonInteractive ");
                arguments.Add("-NoProfile ");
                arguments.Add("-ExecutionPolicy Bypass ");
                arguments.Add("-WindowStyle Hidden ");
                arguments.Add(powershellArgument);
            }

            var exitCode = InvokeCommandLineTool(
                PowershellExePath,
                arguments,
                workingDirectory: workingDirectory,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);

            if (exitCode != 0)
            {
                var text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    Path.GetFileName(PowershellExePath.ItemSpec),
                    exitCode);
                if (IgnoreErrors)
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
        }

        /// <summary>
        /// Executes a powershell script.
        /// </summary>
        /// <param name="powershellCommand">The powershell command that should be executed.</param>
        /// <param name="workingDirectory">The full path to the working directory.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <param name="standardErrorHandler">
        ///     The event handler that handles the standard error stream of the command line application. If no value is provided
        ///     then all messages are logged as errors.
        /// </param>
        protected void InvokePowershellCommand(
            string powershellCommand,
            ITaskItem workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            var powershellArgument = string.Format(
                CultureInfo.InvariantCulture,
                "-Command \"{0}\"",
                powershellCommand);

            InvokePowershell(
                powershellArgument,
                workingDirectory: workingDirectory,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
        }

        /// <summary>
        /// Executes a powershell script.
        /// </summary>
        /// <param name="powershellFile">The full path to the powershell script.</param>
        /// <param name="workingDirectory">The full path to the working directory.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <param name="standardErrorHandler">
        ///     The event handler that handles the standard error stream of the command line application. If no value is provided
        ///     then all messages are logged as errors.
        /// </param>
        protected void InvokePowershellFile(
            string powershellFile,
            ITaskItem workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            var powershellArgument = string.Format(
                CultureInfo.InvariantCulture,
                "-File \"{0}\"",
                powershellFile);

            InvokePowershell(
                powershellArgument,
                workingDirectory: workingDirectory,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
        }

        /// <summary>
        /// Gets or sets the full path to the Powershell executable.
        /// </summary>
        public ITaskItem PowershellExePath
        {
            get
            {
                return _powershellExePath;
            }

            set
            {
                _powershellExePath = value;
            }
        }
    }
}
