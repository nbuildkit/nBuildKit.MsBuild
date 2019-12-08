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
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Utilities;
using Moq;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Tests;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Validation
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class FxCopViaProjectTest : TaskTest
    {
        [Test]
        public void Execute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var outputPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.xml",
                    Guid.NewGuid().ToString()));

            var projectPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.proj",
                    Guid.NewGuid().ToString()));

            var text = "FxCopViaProjectsTest.Execute()";
            using (var writer = new StreamWriter(projectPath, false, Encoding.Unicode))
            {
                writer.WriteLine(text);
            }

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

            var task = new FxCopViaProject(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.ProjectFile = new TaskItem(projectPath);
            task.WarningsAsErrors = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/project:\"{0}\" ",
                    projectPath),
                invokedArgs[0]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/out:\"{0}\" ",
                    outputPath),
                invokedArgs[1]);
            Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[2]);
            Assert.AreEqual("/searchgac ", invokedArgs[3]);
            Assert.AreEqual("/forceoutput ", invokedArgs[4]);
            Assert.AreEqual("/successfile ", invokedArgs[5]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 3);
        }

        [Test]
        public void ExecuteWithErrors()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var outputPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.xml",
                    Guid.NewGuid().ToString()));

            var projectPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.proj",
                    Guid.NewGuid().ToString()));

            var text = "FxCopViaProjectsTest.Execute()";
            using (var writer = new StreamWriter(projectPath, false, Encoding.Unicode))
            {
                writer.WriteLine(text);
            }

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

            var task = new FxCopViaProject(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.ProjectFile = new TaskItem(projectPath);
            task.WarningsAsErrors = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/project:\"{0}\" ",
                    projectPath),
                invokedArgs[0]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/out:\"{0}\" ",
                    outputPath),
                invokedArgs[1]);
            Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[2]);
            Assert.AreEqual("/searchgac ", invokedArgs[3]);
            Assert.AreEqual("/forceoutput ", invokedArgs[4]);
            Assert.AreEqual("/successfile ", invokedArgs[5]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 3);
        }

        [Test]
        public void ExecuteWithMissingProject()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var outputPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.xml",
                    Guid.NewGuid().ToString()));

            var projectPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.proj",
                    Guid.NewGuid().ToString()));

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

            var task = new FxCopViaProject(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.ProjectFile = new TaskItem(projectPath);
            task.WarningsAsErrors = false;

            var result = task.Execute();
            Assert.IsFalse(result);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Never());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithWarnings()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var outputPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.xml",
                    Guid.NewGuid().ToString()));

            var projectPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.proj",
                    Guid.NewGuid().ToString()));

            var text = "FxCopViaProjectsTest.Execute()";
            using (var writer = new StreamWriter(projectPath, false, Encoding.Unicode))
            {
                writer.WriteLine(text);
            }

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

            var task = new FxCopViaProject(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.ProjectFile = new TaskItem(projectPath);
            task.WarningsAsErrors = false;

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/project:\"{0}\" ",
                    projectPath),
                invokedArgs[0]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/out:\"{0}\" ",
                    outputPath),
                invokedArgs[1]);
            Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[2]);
            Assert.AreEqual("/searchgac ", invokedArgs[3]);
            Assert.AreEqual("/forceoutput ", invokedArgs[4]);
            Assert.AreEqual("/successfile ", invokedArgs[5]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 4);
        }
    }
}
