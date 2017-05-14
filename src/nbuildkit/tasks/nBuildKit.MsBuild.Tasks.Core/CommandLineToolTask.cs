//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core.FileSystem;
using Nuclei.Diagnostics;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that invoke a command line tool.
    /// </summary>
    public abstract class CommandLineToolTask : BaseTask
    {
        /// <summary>
        /// Defines the error ID for an error describing an application writing to the error stream.
        /// </summary>
        protected const string ErrorIdApplicationErrorStream = "NBuildKit.Application.WroteToErrorStream";

        /// <summary>
        /// Defines the error ID for an error indicating that a required application argument has not been provided.
        /// </summary>
        protected const string ErrorIdApplicationMissingArgument = "NBuildKit.Application.MissingArgument";

        /// <summary>
        /// Defines the error ID for an error describing an application exiting with a non-zero exit code.
        /// </summary>
        protected const string ErrorIdApplicationNonzeroExitCode = "NBuildKit.Application.NonzeroExitCode";

        /// <summary>
        /// Defines the error ID for an error indicating that the path to the selected tool was not found.
        /// </summary>
        protected const string ErrorIdApplicationPathNotFound = "NBuildKit.FileNotFound.ExecutableTool";

        private readonly IApplicationInvoker _invoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineToolTask"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        protected CommandLineToolTask(IApplicationInvoker invoker)
        {
            _invoker = invoker ?? new ApplicationInvoker(new SystemDiagnostics(new MsBuildLogger(Log), null));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineToolTask"/> class.
        /// </summary>
        /// <param name="diagnostics">The object that provides the diagnostics for the application.</param>
        protected CommandLineToolTask(SystemDiagnostics diagnostics)
            : this(new ApplicationInvoker(diagnostics))
        {
        }

        /// <summary>
        /// Gets the event handler that processes data from the data stream, or standard output stream, of
        /// the command line application.By default logs a message for each output.
        /// </summary>
        protected virtual DataReceivedEventHandler DefaultDataHandler
        {
            get
            {
                return (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            Log.LogMessage(e.Data);
                        }
                    };
            }
        }

        /// <summary>
        /// Gets the event handler that processes data from the data stream, or standard output stream, of
        /// the command line application.By default logs a message for each output.
        /// </summary>
        protected virtual DataReceivedEventHandler DefaultErrorHandler
        {
            get
            {
                // Fix for the issue reported here: https://github.com/Microsoft/msbuild/issues/397
                var encoding = Console.OutputEncoding;
                return (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        // If the error stream encoding is UTF8
                        // it is possible that the error stream contains the BOM marker for UTF-8
                        // So even if the error stream is actually empty, we still get something in
                        // it, which means we'll fail.
                        if (Encoding.UTF8.Equals(encoding) && (e.Data.Length == 1))
                        {
                            return;
                        }

                        Log.LogError(
                            string.Empty,
                            ErrorCodeById(ErrorIdApplicationErrorStream),
                            ErrorIdApplicationErrorStream,
                            string.Empty,
                            0,
                            0,
                            0,
                            0,
                            e.Data);
                    }
                };
            }
        }

        /// <summary>
        /// Returns the most complete path for the given executable tool. May return just the name of the tool if the tool path is found via the
        /// PATH environment variable.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The most complete path for the given executable tool.</returns>
        protected string GetFullToolPath(ITaskItem path)
        {
            if (path == null)
            {
                return string.Empty;
            }

            return GetFullToolPath(path.ItemSpec);
        }

        /// <summary>
        /// Returns the most complete path for the given executable tool. May return just the name of the tool if the tool path is found via the
        /// PATH environment variable.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The most complete path for the given executable tool.</returns>
        protected string GetFullToolPath(string path)
        {
            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Searching for full path of {0}",
                    path));

            var result = PathUtilities.GetAbsolutePath(path);
            if (!File.Exists(result))
            {
                // Fall back to using the 'where' command. This really only searches based on the file name so ...
                var info = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = Path.GetFileName(path),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                var text = new StringBuilder();
                var process = new Process();
                process.StartInfo = info;
                process.OutputDataReceived +=
                    (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            text.AppendLine(e.Data);
                        }
                    };
                process.ErrorDataReceived += DefaultErrorHandler;
                try
                {
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} exited with a non-zero exit code. Exit code was: {1}",
                            Path.GetFileName(process.StartInfo.FileName),
                            process.ExitCode));

                    // The where command is probably not on the path. So we just return the
                    // input value
                    result = path;
                }

                if (process.ExitCode != 0)
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} exited with a non-zero exit code. Exit code was: {1}",
                            Path.GetFileName(process.StartInfo.FileName),
                            process.ExitCode));
                }

                // just return first match
                var output = text.ToString();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    result = output.Substring(0, output.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase));
                }
            }

            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Full path for tool: {0} is: {1}",
                    path,
                    result));

            return result;
        }

        /// <summary>
        /// Invokes the command line tool at the given path with the given arguments.
        /// </summary>
        /// <param name="exePath">The path to the command line executable.</param>
        /// <param name="arguments">The ordered list of command line arguments that should be passed to the application.</param>
        /// <param name="workingDirectory">The full path to the working directory.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <param name="standardErrorHandler">
        ///     The event handler that handles the standard error stream of the command line application. If no value is provided
        ///     then all messages are logged as errors.
        /// </param>
        /// <returns>The exit code of the application.</returns>
        protected int InvokeCommandLineTool(
            ITaskItem exePath,
            IEnumerable<string> arguments,
            ITaskItem workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            if ((exePath == null) || string.IsNullOrEmpty(exePath.ItemSpec))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdApplicationPathNotFound),
                    ErrorIdApplicationPathNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "The command line executable name was not provided");
                return -1;
            }

            var workingDirectoryAsString = workingDirectory != null ? workingDirectory.ItemSpec : null;
            if (string.IsNullOrEmpty(workingDirectoryAsString))
            {
                workingDirectoryAsString = Directory.GetCurrentDirectory();
            }

            return InvokeCommandLineTool(
                exePath.ItemSpec,
                arguments,
                workingDirectoryAsString,
                standardOutputHandler,
                standardErrorHandler);
        }

        /// <summary>
        /// Invokes the command line tool at the given path with the given arguments.
        /// </summary>
        /// <param name="exePath">The path to the command line executable.</param>
        /// <param name="arguments">The ordered list of command line arguments that should be passed to the application.</param>
        /// <param name="workingDirectory">The full path to the working directory.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <param name="standardErrorHandler">
        ///     The event handler that handles the standard error stream of the command line application. If no value is provided
        ///     then all messages are logged as errors.
        /// </param>
        /// <returns>The exit code of the application.</returns>
        protected int InvokeCommandLineTool(
            string exePath,
            IEnumerable<string> arguments,
            string workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            var outputHandler = standardOutputHandler ?? DefaultDataHandler;
            var errorHandler = standardErrorHandler ?? DefaultErrorHandler;

            return _invoker.Invoke(
                exePath,
                arguments,
                workingDirectory,
                UpdateEnvironmentVariables,
                outputHandler,
                errorHandler,
                LogEnvironmentVariables);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the environment variables for the process should be logged.
        /// </summary>
        public bool LogEnvironmentVariables
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
        protected virtual void UpdateEnvironmentVariables(StringDictionary environmentVariables)
        {
            // By default do nothing. We just use the standard environment variables.
        }
    }
}
