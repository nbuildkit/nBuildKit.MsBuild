//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines the base class for <see cref="ITask"/> classes that invoke a command line tool.
    /// </summary>
    public abstract class CommandLineToolTask : Task
    {
        /// <summary>
        /// Gets the event handler that processes data from the data stream, or standard output stream, of
        /// the command line application.By default logs a message for each output.
        /// </summary>
        private DataReceivedEventHandler DefaultDataHandler
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
        private DataReceivedEventHandler DefaultErrorHandler
        {
            get
            {
                return (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogError(e.Data);
                    }
                };
            }
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The absolute path.</returns>
        protected string GetAbsolutePath(ITaskItem path)
        {
            return GetAbsolutePath(path.ItemSpec);
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The absolute path.</returns>
        protected string GetAbsolutePath(string path)
        {
            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
                    "Searching for full path of {0}",
                    path));

            var result = path;
            if (!Path.IsPathRooted(result))
            {
                result = Path.GetFullPath(result);
            }

            return result;
        }

        /// <summary>
        /// Returns the most complete path for the given executable tool. May return just the name of the tool if the tool path is found via the
        /// PATH environment variable.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The most complete path for the given executable tool.</returns>
        protected string GetFullToolPath(ITaskItem path)
        {
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
                    "Searching for full path of {0}",
                    path));

            var result = path;
            if (!Path.IsPathRooted(result))
            {
                result = Path.GetFullPath(result);
            }

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
                process.ErrorDataReceived +=
                    (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            Log.LogError(e.Data);
                        }
                    };
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
                            "{0} exited with a non-zero exit code. Exit code was: {1}",
                            Path.GetFileName(process.StartInfo.FileName),
                            process.ExitCode));
                }

                // just return first match
                var output = text.ToString();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    result = output.Substring(0, output.IndexOf(Environment.NewLine));
                }
            }

            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
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
        protected int InvokeCommandlineTool(
            ITaskItem exePath,
            IEnumerable<string> arguments,
            ITaskItem workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            if (string.IsNullOrEmpty(exePath.ItemSpec))
            {
                Log.LogError("The command line executable name was not provided");
                return -1;
            }

            var workingDirectoryAsString = workingDirectory?.ItemSpec;
            if (string.IsNullOrEmpty(workingDirectoryAsString))
            {
                workingDirectoryAsString = Directory.GetCurrentDirectory();
            }

            return InvokeCommandlineTool(
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
        protected int InvokeCommandlineTool(
            string exePath,
            IEnumerable<string> arguments,
            string workingDirectory = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null)
        {
            if (workingDirectory == null)
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }

            var filePath = GetFullToolPath(exePath);
            var absoluteWorkingDirectory = GetAbsolutePath(workingDirectory);

            var builder = new StringBuilder();
            {
                foreach (var argument in arguments)
                {
                    builder.Append(argument);
                    builder.Append(" ");
                }
            }

            var argumentsAsText = builder.ToString();
            var info = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = Environment.ExpandEnvironmentVariables(argumentsAsText),
                WorkingDirectory = absoluteWorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            UpdateEnvironmentVariables(info.EnvironmentVariables);

            Log.LogMessage(
                MessageImportance.Low,
                "Executing {0} in {1} with arguments: {2}",
                filePath,
                absoluteWorkingDirectory,
                argumentsAsText);
            Log.LogMessage(MessageImportance.Low, "Environment variables for the process are: ");
            foreach (DictionaryEntry pair in info.EnvironmentVariables)
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    string.Format(
                        "{0}: {1}",
                        pair.Key,
                        pair.Value));
            }

            var text = new StringBuilder();
            var process = new Process();
            process.StartInfo = info;

            var dataHandler = standardOutputHandler ?? DefaultDataHandler;
            process.OutputDataReceived += dataHandler;

            var errorHandler = standardErrorHandler ?? DefaultErrorHandler;
            process.ErrorDataReceived += errorHandler;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var exitCode = process.ExitCode;
            return exitCode;
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
