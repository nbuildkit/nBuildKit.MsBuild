//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Moq;

namespace NBuildKit.MsBuild.Tasks.Tests
{
    /// <summary>
    /// Defines a set of default methods and properties helpful when testing MsBuild <see cref="ITask"/> implementations.
    /// </summary>
    public abstract class TaskTest
    {
        /// <summary>
        /// Creates a <see cref="DataReceivedEventArgs"/> instance with the given test data.
        /// </summary>
        /// <param name="testData">The test data.</param>
        /// <returns>A new instance of the <see cref="DataReceivedEventArgs"/> class.</returns>
        public static DataReceivedEventArgs CreateDataReceivedEventArgs(string testData)
        {
            if (string.IsNullOrEmpty(testData))
            {
                throw new ArgumentException("testData is null or empty.", "testData");
            }

            DataReceivedEventArgs mockEventArgs =
                (DataReceivedEventArgs)System.Runtime.Serialization.FormatterServices
                 .GetUninitializedObject(typeof(DataReceivedEventArgs));

            var eventFields = typeof(DataReceivedEventArgs)
                .GetFields(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

            if (eventFields.Count() > 0)
            {
                eventFields[0].SetValue(mockEventArgs, testData);
            }
            else
            {
                throw new InvalidOperationException("Failed to find _data field!");
            }

            return mockEventArgs;
        }

        private readonly List<string> _errorMessages = new List<string>();
        private readonly List<string> _logMessages = new List<string>();
        private readonly List<string> _warningMessages = new List<string>();

        private Mock<IBuildEngine4> _buildEngine;

        /// <summary>
        /// Gets the <see cref="IBuildEngine"/> instance that can be used as a mock MsBuild build engine.
        /// </summary>
        protected Mock<IBuildEngine4> BuildEngine
        {
            get
            {
                return _buildEngine;
            }
        }

        /// <summary>
        /// Initializes the build engine.
        /// </summary>
        /// <param name="projectBuildResults">An array containing the expected project build results.</param>
        public void InitializeBuildEngine(bool[] projectBuildResults = null)
        {
            _errorMessages.Clear();
            _logMessages.Clear();
            _warningMessages.Clear();

            var buildEngine = new Mock<IBuildEngine>();
            {
                buildEngine.Setup(b => b.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                    .Callback<BuildErrorEventArgs>(b => _errorMessages.Add(b.Message))
                    .Verifiable();
                buildEngine.Setup(b => b.LogMessageEvent(It.IsAny<BuildMessageEventArgs>()))
                    .Callback<BuildMessageEventArgs>(b => _logMessages.Add(b.Message))
                    .Verifiable();
                buildEngine.Setup(b => b.LogWarningEvent(It.IsAny<BuildWarningEventArgs>()))
                    .Callback<BuildWarningEventArgs>(b => _warningMessages.Add(b.Message))
                    .Verifiable();
            }

            var buildEngine2 = buildEngine.As<IBuildEngine2>();
            var buildEngine3 = buildEngine2.As<IBuildEngine3>();
            {
                var count = 0;
                buildEngine3.Setup(
                    b => b.BuildProjectFilesInParallel(
                        It.IsAny<string[]>(),
                        It.IsAny<string[]>(),
                        It.IsAny<IDictionary[]>(),
                        It.IsAny<IList<string>[]>(),
                        It.IsAny<string[]>(),
                        It.IsAny<bool>()))
                    .Returns(
                        () =>
                        {
                            var result = (projectBuildResults != null) ? projectBuildResults[count] : true;
                            count++;

                            return new BuildEngineResult(result, new List<IDictionary<string, ITaskItem[]>>());
                        })
                    .Verifiable();
            }

            _buildEngine = buildEngine3.As<IBuildEngine4>();
        }

        /// <summary>
        /// Verifies the number of times the build engine has been invoked.
        /// </summary>
        /// <param name="numberOfInvocations">The expected number of times the build engine should have been invoked.</param>
        public void VerifyNumberOfInvocations(int numberOfInvocations = 0)
        {
            _buildEngine.Verify(
                b => b.BuildProjectFilesInParallel(
                    It.IsAny<string[]>(),
                    It.IsAny<string[]>(),
                    It.IsAny<IDictionary[]>(),
                    It.IsAny<IList<string>[]>(),
                    It.IsAny<string[]>(),
                    It.IsAny<bool>()),
                Times.Exactly(numberOfInvocations));
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
