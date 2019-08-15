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

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that work with the TFS command line tool.
    /// </summary>
    public abstract class TfCommandLineToolTask : CommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TfCommandLineToolTask"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        protected TfCommandLineToolTask(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the path to the TF command line executable.
        /// </summary>
        [Required]
        public ITaskItem TfExecutablePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory that contains the root of the TFS workspace.
        /// </summary>
        [Required]
        public ITaskItem Workspace
        {
            get;
            set;
        }

        /// <summary>
        /// Invokes the TF command line tool with the given arguments in the provided workspace directory.
        /// </summary>
        /// <param name="arguments">The collection of arguments.</param>
        /// <returns>The output of the TF process.</returns>
        protected string GetTfOutput(IEnumerable<string> arguments)
        {
            var text = new StringBuilder();
            DataReceivedEventHandler standardOutputHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    text.AppendLine(e.Data);
                }
            };

            var exitCode = InvokeTf(arguments, standardOutputHandler: standardOutputHandler);
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
                    System.IO.Path.GetFileName(TfExecutablePath.ItemSpec),
                    exitCode);
                Log.LogWarning(
                    "Output was: {0}",
                    text);
            }

            return text.ToString();
        }

        /// <summary>
        /// Invokes the TF command line tool with the given arguments in the provided workspace directory.
        /// </summary>
        /// <param name="arguments">The collection of arguments.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <returns>The output of the TF process.</returns>
        protected int InvokeTf(IEnumerable<string> arguments, DataReceivedEventHandler standardOutputHandler = null)
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
                TfExecutablePath,
                arguments,
                Workspace,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
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
                    System.IO.Path.GetFileName(TfExecutablePath.ItemSpec),
                    exitCode);
            }

            return exitCode;
        }
    }
}
