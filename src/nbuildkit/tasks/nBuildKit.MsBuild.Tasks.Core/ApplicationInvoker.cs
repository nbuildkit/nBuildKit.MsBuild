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
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Provides methods for invoking an application.
    /// </summary>
    public sealed class ApplicationInvoker : IApplicationInvoker
    {
        /// <summary>
        /// The generic application error exit code.
        /// </summary>
        private const int ApplicationConfigurationErrorExitCode = -1;

        /// <summary>
        /// The default windows exit code for cases where the application could not be found.
        /// </summary>
        private const int ApplicationNotFoundExitCode = 9009;

        /// <summary>
        /// The object that provides the diagnostics methods for the application.
        /// </summary>
        private readonly TaskLoggingHelper _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInvoker"/> class.
        /// </summary>
        /// <param name="logger">The object that provides the diagnostics for the application.</param>
        public ApplicationInvoker(TaskLoggingHelper logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the application and returns the exit code.
        /// </summary>
        /// <param name="applicationPath">The path to the command line executable.</param>
        /// <param name="arguments">The ordered list of command line arguments that should be passed to the application.</param>
        /// <param name="workingDirectory">The full path to the working directory.</param>
        /// <param name="updateEnvironmentVariables">The function that is used to update the list of environment variables for the application.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <param name="standardErrorHandler">
        ///     The event handler that handles the standard error stream of the command line application. If no value is provided
        ///     then all messages are logged as errors.
        /// </param>
        /// <param name="logEnvironmentVariables">A flag that indicates whether the environment variables for the process should be logged.</param>
        /// <returns>
        ///     The exit code of the application. If the application does not return an exit code then zero is returned.
        /// </returns>
        public int Invoke(
            string applicationPath,
            IEnumerable<string> arguments,
            string workingDirectory = null,
            Action<StringDictionary> updateEnvironmentVariables = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null,
            bool logEnvironmentVariables = false)
        {
            if (string.IsNullOrWhiteSpace(applicationPath))
            {
                _logger.LogError("The path to the application was null or empty. Cannot execute the application without a valid path.");

                return ApplicationNotFoundExitCode;
            }

            if (arguments == null)
            {
                _logger.LogError("The arguments collection is null.");
                return ApplicationConfigurationErrorExitCode;
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }

            if (!Path.IsPathRooted(workingDirectory))
            {
                workingDirectory = Path.GetFullPath(workingDirectory);
            }

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
                FileName = applicationPath,
                Arguments = Environment.ExpandEnvironmentVariables(argumentsAsText),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            updateEnvironmentVariables?.Invoke(info.EnvironmentVariables);

            _logger.LogMessage(
                MessageImportance.Low,
                "Executing {0} in {1} with arguments: {2}",
                applicationPath,
                workingDirectory,
                argumentsAsText);
            if (logEnvironmentVariables)
            {
                _logger.LogMessage(MessageImportance.Low, "Environment variables for the process are: ");
                foreach (DictionaryEntry pair in info.EnvironmentVariables)
                {
                    _logger.LogMessage(
                        MessageImportance.Low,
                        "{0}: {1}",
                        pair.Key,
                        pair.Value);
                }
            }

            var process = new Process
                {
                    StartInfo = info,
                };

            using (process)
            {
                if (standardOutputHandler != null)
                {
                    process.OutputDataReceived += standardOutputHandler;
                }

                if (standardErrorHandler != null)
                {
                    process.ErrorDataReceived += standardErrorHandler;
                }

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                var exitCode = process.ExitCode;
                return exitCode;
            }
        }
    }
}
