//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Framework;
using Moq;

namespace NBuildKit.MsBuild.Tasks.Tests
{
    /// <summary>
    /// Defines a set of default methods and properties helpful when testing MsBuild <see cref="ITask"/> implementations.
    /// </summary>
    public abstract class TaskTest
    {
        private readonly List<string> _errorMessages = new List<string>();
        private readonly List<string> _logMessages = new List<string>();
        private readonly List<string> _warningMessages = new List<string>();

        private Mock<IBuildEngine> _buildEngine;

        /// <summary>
        /// Gets the <see cref="IBuildEngine"/> instance that can be used as a mock MsBuild build engine.
        /// </summary>
        protected Mock<IBuildEngine> BuildEngine
        {
            get
            {
                return _buildEngine;
            }
        }

        /// <summary>
        /// Initializes the build engine.
        /// </summary>
        public void InitializeBuildEngine()
        {
            _errorMessages.Clear();
            _logMessages.Clear();
            _warningMessages.Clear();

            _buildEngine = new Mock<IBuildEngine>();
            {
                _buildEngine.Setup(b => b.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                    .Callback<BuildErrorEventArgs>(b => _errorMessages.Add(b.Message))
                    .Verifiable();
                _buildEngine.Setup(b => b.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                    .Callback<BuildMessageEventArgs>(b => _logMessages.Add(b.Message))
                    .Verifiable();
                _buildEngine.Setup(b => b.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()))
                    .Callback<BuildWarningEventArgs>(b => _warningMessages.Add(b.Message))
                    .Verifiable();
            }
        }

        /// <summary>
        /// Verifies the number of log messages that should have been recorded.
        /// </summary>
        /// <param name="numberOfErrorMessages">The number of error messages that should have been recorded.</param>
        /// <param name="numberOfWarningMessages">The number of warning messages that should have been recorded.</param>
        /// <param name="numberOfNormalMessages">The number of log messages that should have been recorded.</param>
        public void VerifyNumberOfLogMessages(int numberOfErrorMessages = -1, int numberOfWarningMessages = -1, int numberOfNormalMessages = -1)
        {
            if (numberOfErrorMessages > -1)
            {
                if (numberOfErrorMessages == 0)
                {
                    _buildEngine.Verify(
                        b => b.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()),
                        Times.Never(),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected zero error messages but have {0}: {1}",
                            _errorMessages.Count,
                            string.Join(Environment.NewLine, _errorMessages)));
                }
                else
                {
                    _buildEngine.Verify(
                        b => b.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()),
                        Times.Exactly(numberOfErrorMessages),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected {0} error messages but have {1}: {2}",
                            numberOfErrorMessages,
                            _errorMessages.Count,
                            string.Join(Environment.NewLine, _errorMessages)));
                }
            }

            if (numberOfWarningMessages > -1)
            {
                if (numberOfWarningMessages == 0)
                {
                    _buildEngine.Verify(
                        b => b.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()),
                        Times.Never(),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected zero warning messages but have {0}: {1}",
                            _warningMessages.Count,
                            string.Join(Environment.NewLine, _warningMessages)));
                }
                else
                {
                    _buildEngine.Verify(
                        b => b.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()),
                        Times.Exactly(numberOfWarningMessages),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected {0} warning messages but have {1}: {2}",
                            numberOfWarningMessages,
                            _warningMessages.Count,
                            string.Join(Environment.NewLine, _warningMessages)));
                }
            }

            if (numberOfNormalMessages > -1)
            {
                if (numberOfNormalMessages == 0)
                {
                    _buildEngine.Verify(
                        b => b.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()),
                        Times.Never(),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected zero log messages but have {0}: {1}",
                            _logMessages.Count,
                            string.Join(Environment.NewLine, _logMessages)));
                }
                else
                {
                    _buildEngine.Verify(
                        b => b.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()),
                        Times.Exactly(numberOfNormalMessages),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expected {0} log messages but have {1}: {2}",
                            numberOfNormalMessages,
                            _logMessages.Count,
                            string.Join(Environment.NewLine, _logMessages)));
                }
            }
        }
    }
}
