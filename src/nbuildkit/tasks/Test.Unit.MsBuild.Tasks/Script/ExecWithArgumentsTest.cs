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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class ExecWithArgumentsTest : TaskTest
    {
        [Test]
        public void ExecuteWhileIgnoringErrorStream()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

                            err(null, CreateDataReceivedEventArgs("Failure!"));
                        });
            }

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.IgnoreErrors = true;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 1, numberOfNormalMessages: 3);
        }

        [Test]
        public void ExecuteWhileIgnoringExitCode()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = true;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 1, numberOfNormalMessages: 3);
        }

        [Test]
        public void ExecuteWithAdditionalEnvironmentPaths()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.AdditionalEnvironmentPaths = new ITaskItem[]
            {
                new TaskItem("a"),
            };
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);

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

            var environmentVariables = new StringDictionary();
            environmentVariableBuilder(environmentVariables);

            Assert.AreEqual(1, environmentVariables.Count);
            var pathElements = environmentVariables["PATH"].Split(new[] { ';' });
            Assert.AreEqual(Environment.GetEnvironmentVariable("PATH").Split(new[] { ';' }).Length + 1, pathElements.Length);
            Assert.AreEqual(Path.Combine(directory, "a"), pathElements[pathElements.Length - 1]);
        }

        public void ExecuteWithArgumentPrefix()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.ArgumentPrefix = "-";
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("-argument1 value1", invokedArgs[0]);

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

        public void ExecuteWithArgumentSeparator()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.ArgumentSeparator = ":";
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1:value1", invokedArgs[0]);

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
        public void ExecuteWithArgumentWithoutValue()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", string.Empty }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1", invokedArgs[0]);

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
        public void ExecuteWithArgumentWithSpaces()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument 1",
                    new Hashtable
                    {
                        { "Value", string.Empty }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("\"argument 1\"", invokedArgs[0]);

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
        public void ExecuteWithArgumentWithSpacesInValue()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value 1" }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 \"value 1\"", invokedArgs[0]);

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
        public void ExecuteWithDataOnErrorStream()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

                            err(null, CreateDataReceivedEventArgs("Failure!"));
                        });
            }

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);

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
        public void ExecuteWithFailureExitCode()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);

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
        public void ExecuteWithMultipleArguments()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    }),
                new TaskItem(
                    "argument2",
                    new Hashtable
                    {
                        { "Value", "value2" }
                    }),
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(2, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);
            Assert.AreEqual("argument2 value2", invokedArgs[1]);

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
        public void ExecuteWithSingleArgument()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var file = Assembly.GetExecutingAssembly().LocalFilePath();

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

            var task = new ExecWithArguments(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Arguments = new ITaskItem[]
            {
                new TaskItem(
                    "argument1",
                    new Hashtable
                    {
                        { "Value", "value1" }
                    })
            };
            task.IgnoreErrors = false;
            task.IgnoreExitCode = false;
            task.ToolPath = new TaskItem(file);
            task.WorkingDirectory = new TaskItem(directory);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(file, invokedPath);

            Assert.AreEqual(1, invokedArgs.Count);
            Assert.AreEqual("argument1 value1", invokedArgs[0]);

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
    }
}
