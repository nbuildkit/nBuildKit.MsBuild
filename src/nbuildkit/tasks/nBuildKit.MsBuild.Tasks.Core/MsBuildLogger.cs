//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines an <see cref="ILogger"/> which writes to the MsBuild logger.
    /// </summary>
    public sealed class MsBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsBuildLogger"/> class.
        /// </summary>
        /// <param name="logger">The internal logger that will write the log messages to MsBuild.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        public MsBuildLogger(TaskLoggingHelper logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="importance">The message importance.</param>
        /// <param name="format">The message format.</param>
        /// <param name="arguments">The message arguments.</param>
        public void LogMessage(MessageImportance importance, string format, params object[] arguments)
        {
            _logger.LogMessage(importance, format, arguments);
        }
    }
}
