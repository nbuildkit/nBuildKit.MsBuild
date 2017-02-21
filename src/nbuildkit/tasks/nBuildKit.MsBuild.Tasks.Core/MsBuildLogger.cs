//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Nuclei.Diagnostics.Logging;

using ILogger = Nuclei.Diagnostics.Logging.ILogger;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines an <see cref="ILogger"/> which writes to the MsBuild logger.
    /// </summary>
    public sealed class MsBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper _logger;

        private bool disposedValue = false;

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
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        /// <summary>
        /// Stops the logger and ensures that all log messages have been
        /// saved to the log.
        /// </summary>
        public void Close()
        {
            // Do nothing. MsBuild will kill the logger.
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        /// <summary>
        ///  Performs application-defined tasks associated with freeing, releasing, or
        ///  resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Gets or sets the current <see cref="LevelToLog"/>.
        /// </summary>
        public LevelToLog Level
        {
            get;
            set;
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1062:Validate arguments of public methods",
            MessageId = "0",
            Justification = "The 'ShouldLog' method validates the message.")]
        public void Log(LogMessage message)
        {
            if (!ShouldLog(message))
            {
                return;
            }

            switch (message.Level)
            {
                case LevelToLog.Trace:
                    _logger.LogMessage(MessageImportance.Low, message.Text, message.FormatParameters);
                    break;
                case LevelToLog.Debug:
                    _logger.LogMessage(MessageImportance.Low, message.Text, message.FormatParameters);
                    break;
                case LevelToLog.Info:
                    _logger.LogMessage(MessageImportance.Normal, message.Text, message.FormatParameters);
                    break;
                case LevelToLog.Warn:
                    _logger.LogWarning(message.Text, message.FormatParameters);
                    break;
                case LevelToLog.Error:
                    _logger.LogError(message.Text, message.FormatParameters);
                    break;
                case LevelToLog.Fatal:
                    _logger.LogError(message.Text, message.FormatParameters);
                    break;
                case LevelToLog.None:
                    break;
                default:
                    _logger.LogMessage(MessageImportance.Normal, message.Text, message.FormatParameters);
                    break;
            }
        }

        /// <summary>
        /// Indicates if a message will be written to the log file based on the
        /// current log level and the level of the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// <see langword="true" /> if the message will be logged; otherwise, <see langword="false" />.
        /// </returns>
        public bool ShouldLog(LogMessage message)
        {
            if (Level == LevelToLog.None)
            {
                return false;
            }

            if (message == null)
            {
                return false;
            }

            if (message.Level == LevelToLog.None)
            {
                return false;
            }

            return message.Level >= Level;
        }
    }
}
