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
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Validation
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class FxCopViaAssembliesTest : TaskTest
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

            var assemblyPath = Assembly.GetExecutingAssembly().LocalFilePath();
            var ruleset = Path.Combine(directory, "rules.ruleset");
            var rulesetDirectory = Path.Combine(directory, "rules");
            var dictionary = Path.Combine(directory, "dictionary.xml");

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

            var task = new FxCopViaAssemblies(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Assemblies = new ITaskItem[]
            {
                new TaskItem(
                    assemblyPath,
                    new Hashtable
                    {
                        { "TargetFramework", "4.5" },
                        { "RuleSet", ruleset },
                        { "CustomDictionary", dictionary }
                    }),
            };
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.RuleSetDirectory = new TaskItem(rulesetDirectory);
            task.WarningsAsErrors = false;

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            Assert.AreEqual(10, invokedArgs.Count);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/ruleset:=\"{0}\" ",
                    ruleset),
                invokedArgs[0]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/rulesetdirectory:\"{0}\" ",
                    rulesetDirectory),
                invokedArgs[1]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/out:\"{0}\\{1}_45-0.xml\" ",
                    directory,
                    Path.GetFileNameWithoutExtension(outputPath)),
                invokedArgs[2]);
            Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[3]);
            Assert.AreEqual("/searchgac ", invokedArgs[4]);
            Assert.AreEqual("/forceoutput ", invokedArgs[5]);
            Assert.AreEqual("/successfile ", invokedArgs[6]);
            Assert.AreEqual("/targetframeworkversion:v4.5 ", invokedArgs[7]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/dictionary:\"{0}\" ",
                    dictionary),
                invokedArgs[8]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/file:\"{0}\" ",
                    assemblyPath),
                invokedArgs[9]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 6);
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

            var assemblyPath = Assembly.GetExecutingAssembly().LocalFilePath();
            var ruleset = Path.Combine(directory, "rules.ruleset");
            var rulesetDirectory = Path.Combine(directory, "rules");

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

            var task = new FxCopViaAssemblies(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Assemblies = new ITaskItem[]
            {
                new TaskItem(
                    assemblyPath,
                    new Hashtable
                    {
                        { "TargetFramework", "4.5" },
                        { "RuleSet", ruleset }
                    }),
            };
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.RuleSetDirectory = new TaskItem(rulesetDirectory);
            task.WarningsAsErrors = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            Assert.AreEqual(9, invokedArgs.Count);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/ruleset:=\"{0}\" ",
                    ruleset),
                invokedArgs[0]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/rulesetdirectory:\"{0}\" ",
                    rulesetDirectory),
                invokedArgs[1]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/out:\"{0}\\{1}_45-0.xml\" ",
                    directory,
                    Path.GetFileNameWithoutExtension(outputPath)),
                invokedArgs[2]);
            Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[3]);
            Assert.AreEqual("/searchgac ", invokedArgs[4]);
            Assert.AreEqual("/forceoutput ", invokedArgs[5]);
            Assert.AreEqual("/successfile ", invokedArgs[6]);
            Assert.AreEqual("/targetframeworkversion:v4.5 ", invokedArgs[7]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/file:\"{0}\" ",
                    assemblyPath),
                invokedArgs[8]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 6);
        }

        [Test]
        public void ExecuteWithMultipleAssemblySets()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var outputPath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.xml",
                    Guid.NewGuid().ToString()));

            var assemblyPath = Assembly.GetExecutingAssembly().LocalFilePath();
            var ruleset = Path.Combine(directory, "rules.ruleset");

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<List<string>>();
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
                            invokedArgs.Add(new List<string>(args));
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new FxCopViaAssemblies(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Assemblies = new ITaskItem[]
            {
                new TaskItem(
                    assemblyPath,
                    new Hashtable
                    {
                        { "TargetFramework", "4.5" },
                        { "RuleSet", ruleset }
                    }),
                new TaskItem(
                    assemblyPath,
                    new Hashtable
                    {
                        { "TargetFramework", "4.5" },
                        { "RuleSet", ruleset }
                    }),
            };
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.WarningsAsErrors = false;

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            for (int i = 0; i < invokedArgs.Count; i++)
            {
                Assert.AreEqual(9, invokedArgs[i].Count);
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/ruleset:=\"{0}\" ",
                        ruleset),
                    invokedArgs[i][0]);
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/rulesetdirectory:\"{0}\" ",
                        string.Empty),
                    invokedArgs[i][1]);
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/out:\"{0}\\{1}_45-{2}.xml\" ",
                        directory,
                        Path.GetFileNameWithoutExtension(outputPath),
                        i),
                    invokedArgs[i][2]);
                Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[i][3]);
                Assert.AreEqual("/searchgac ", invokedArgs[i][4]);
                Assert.AreEqual("/forceoutput ", invokedArgs[i][5]);
                Assert.AreEqual("/successfile ", invokedArgs[i][6]);
                Assert.AreEqual("/targetframeworkversion:v4.5 ", invokedArgs[i][7]);
                Assert.AreEqual(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/file:\"{0}\" ",
                        assemblyPath),
                    invokedArgs[i][8]);
            }

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 7);
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

            var assemblyPath = Assembly.GetExecutingAssembly().LocalFilePath();
            var ruleset = Path.Combine(directory, "rules.ruleset");

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

            var task = new FxCopViaAssemblies(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.Assemblies = new ITaskItem[]
            {
                new TaskItem(
                    assemblyPath,
                    new Hashtable
                    {
                        { "TargetFramework", "4.5" },
                        { "RuleSet", ruleset }
                    }),
            };
            task.FxCopDirectory = new TaskItem(directory);
            task.OutputFile = new TaskItem(outputPath);
            task.WarningsAsErrors = false;

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(Path.Combine(directory, "FxCopCmd.exe"), invokedPath);

            Assert.AreEqual(9, invokedArgs.Count);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/ruleset:=\"{0}\" ",
                    ruleset),
                invokedArgs[0]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/rulesetdirectory:\"{0}\" ",
                    string.Empty),
                invokedArgs[1]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/out:\"{0}\\{1}_45-0.xml\" ",
                    directory,
                    Path.GetFileNameWithoutExtension(outputPath)),
                invokedArgs[2]);
            Assert.AreEqual("/ignoregeneratedcode ", invokedArgs[3]);
            Assert.AreEqual("/searchgac ", invokedArgs[4]);
            Assert.AreEqual("/forceoutput ", invokedArgs[5]);
            Assert.AreEqual("/successfile ", invokedArgs[6]);
            Assert.AreEqual("/targetframeworkversion:v4.5 ", invokedArgs[7]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/file:\"{0}\" ",
                    assemblyPath),
                invokedArgs[8]);

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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 7);
        }
    }
}
