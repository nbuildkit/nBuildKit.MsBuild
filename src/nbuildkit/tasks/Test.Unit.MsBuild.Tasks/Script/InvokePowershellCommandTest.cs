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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Moq;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Tests;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class InvokePowershellCommandTest : TaskTest
    {
        [Test]
        public void Execute()
        {
            var text = "hello world";
            var command = string.Format(
                CultureInfo.InvariantCulture,
                "Write-Output '{0}'",
                text);

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokePowershellCommand();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;
            task.Command = command;

            var result = task.Execute();
            Assert.IsTrue(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            Assert.AreEqual(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", invokedPath);

            Assert.AreEqual(5, invokedArgs.Count);
            Assert.AreEqual("-NoLogo", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive", invokedArgs[1]);
            Assert.AreEqual("-NoProfile", invokedArgs[2]);
            Assert.AreEqual("-ExecutionPolicy Bypass", invokedArgs[3]);
            Assert.AreEqual("-WindowStyle Hidden", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-Command \"{0}\"",
                    command),
                invokedArgs[5]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Once());
        }

        [Test]
        public void ExecuteWithErrors()
        {
            var text = "hello world";
            var errorText = "It is all wrong";
            var command = string.Format(
                CultureInfo.InvariantCulture,
                "$ErrorActionPreference = 'Stop'; Write-Error '{0}'; Write-Output '{1}'",
                errorText,
                text);

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        })
                    .Returns(-1);
            }

            var task = new InvokePowershellCommand();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;
            task.Command = command;

            var result = task.Execute();

            Assert.IsFalse(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 3, numberOfNormalMessages: 2);
        }

        [Test]
        public void ExecuteWithErrorsAsWarnings()
        {
            var text = "hello world";
            var errorText = "It is all wrong";
            var command = string.Format(
                CultureInfo.InvariantCulture,
                "$ErrorActionPreference = 'Continue'; Write-Error '{0}'; Write-Output '{1}'",
                errorText,
                text);

            InitializeBuildEngine();

            var task = new InvokePowershellCommand();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = true;

            task.Command = command;
            var result = task.Execute();

            Assert.IsTrue(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            VerifyNumberOfLogMessages(numberOfWarningMessages: 3, numberOfNormalMessages: 2);
        }

        [Test]
        public void ExecuteWithEmptyCommand()
        {
            var task = new InvokePowershellCommand();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Command = string.Empty;
            var result = task.Execute();

            Assert.IsFalse(result);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1);
        }
    }
}
