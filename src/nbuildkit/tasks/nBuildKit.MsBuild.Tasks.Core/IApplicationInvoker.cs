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

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the interface for objects that invoke executable applications.
    /// </summary>
    public interface IApplicationInvoker
    {
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
        int Invoke(
            string applicationPath,
            IEnumerable<string> arguments,
            string workingDirectory = null,
            Action<StringDictionary> updateEnvironmentVariables = null,
            DataReceivedEventHandler standardOutputHandler = null,
            DataReceivedEventHandler standardErrorHandler = null,
            bool logEnvironmentVariables = false);
    }
}
