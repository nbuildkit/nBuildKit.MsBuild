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
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Packaging
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class NuGetInstallTest : TaskTest
    {
        [Test]
        public void Execute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var toolPath = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new NuGetInstall(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.NuGetExecutablePath = new TaskItem(toolPath);
            task.PackageName = "A";
            task.PackagesDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(toolPath, invokedPath);

            Assert.AreEqual(5, invokedArgs.Count);
            Assert.AreEqual("install \"A\" ", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[1]);
            Assert.AreEqual("-Verbosity detailed ", invokedArgs[2]);
            Assert.AreEqual("-NoCache ", invokedArgs[3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-OutputDirectory \"{0}\" ",
                    directory),
                invokedArgs[4]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteExcludingVersion()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var toolPath = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new NuGetInstall(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.ExcludeVersion = true;
            task.NuGetExecutablePath = new TaskItem(toolPath);
            task.PackageName = "A";
            task.PackagesDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(toolPath, invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("install \"A\" ", invokedArgs[0]);
            Assert.AreEqual("-ExcludeVersion ", invokedArgs[1]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[2]);
            Assert.AreEqual("-Verbosity detailed ", invokedArgs[3]);
            Assert.AreEqual("-NoCache ", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-OutputDirectory \"{0}\" ",
                    directory),
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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithFailure()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var toolPath = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new NuGetInstall(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.NuGetExecutablePath = new TaskItem(toolPath);
            task.PackageName = "A";
            task.PackagesDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.AreEqual(toolPath, invokedPath);

            Assert.AreEqual(5, invokedArgs.Count);
            Assert.AreEqual("install \"A\" ", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[1]);
            Assert.AreEqual("-Verbosity detailed ", invokedArgs[2]);
            Assert.AreEqual("-NoCache ", invokedArgs[3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-OutputDirectory \"{0}\" ",
                    directory),
                invokedArgs[4]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithSources()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var toolPath = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new NuGetInstall(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.NuGetExecutablePath = new TaskItem(toolPath);
            task.PackageName = "A";
            task.PackagesDirectory = new TaskItem(directory);
            task.Sources = new ITaskItem[]
            {
                new TaskItem("Source1"),
                new TaskItem("Source2")
            };

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(toolPath, invokedPath);

            Assert.AreEqual(7, invokedArgs.Count);
            Assert.AreEqual("install \"A\" ", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[1]);
            Assert.AreEqual("-Verbosity detailed ", invokedArgs[2]);
            Assert.AreEqual("-NoCache ", invokedArgs[3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-OutputDirectory \"{0}\" ",
                    directory),
                invokedArgs[4]);
            Assert.AreEqual("-Source \"Source1\" ", invokedArgs[5]);
            Assert.AreEqual("-Source \"Source2\" ", invokedArgs[6]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithVersion()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var toolPath = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new NuGetInstall(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.NuGetExecutablePath = new TaskItem(toolPath);
            task.PackageName = "A";
            task.PackageVersion = "1.2.3";
            task.PackagesDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(toolPath, invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("install \"A\" ", invokedArgs[0]);
            Assert.AreEqual("-Version \"1.2.3\" ", invokedArgs[1]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[2]);
            Assert.AreEqual("-Verbosity detailed ", invokedArgs[3]);
            Assert.AreEqual("-NoCache ", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-OutputDirectory \"{0}\" ",
                    directory),
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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }
    }
}
