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
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class InvokePowershellFileTest : TaskTest
    {
        [Test]
        public void Execute()
        {
            var scriptPath = Path.Combine(
                Assembly.GetExecutingAssembly().LocalDirectoryPath(),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.ps1",
                    Guid.NewGuid().ToString()));

            var text = "hello world";
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Write-Output '{0}'",
                        text));
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

                            o(null, CreateDataReceivedEventArgs(text));
                        });
            }

            var task = new InvokePowershellFile(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();

            Assert.IsTrue(result);

            Assert.AreEqual(text, task.Output);

            Assert.AreEqual(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("-NoLogo ", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[1]);
            Assert.AreEqual("-NoProfile ", invokedArgs[2]);
            Assert.AreEqual("-ExecutionPolicy Bypass ", invokedArgs[3]);
            Assert.AreEqual("-WindowStyle Hidden ", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-File \"{0}\"",
                    scriptPath),
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
            var scriptPath = Path.Combine(
                Assembly.GetExecutingAssembly().LocalDirectoryPath(),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.ps1",
                    Guid.NewGuid().ToString()));

            var text = "hello world";
            var errorText = "It is all wrong";
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("$ErrorActionPreference = 'Stop'");
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Write-Error '{0}'",
                        errorText));
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Write-Output '{0}'",
                        text));
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

                            o(null, CreateDataReceivedEventArgs(text));
                            err(null, CreateDataReceivedEventArgs(errorText));
                        })
                    .Returns(-1);
            }

            var task = new InvokePowershellFile(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();

            Assert.IsFalse(result);
            Assert.AreEqual(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("-NoLogo ", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[1]);
            Assert.AreEqual("-NoProfile ", invokedArgs[2]);
            Assert.AreEqual("-ExecutionPolicy Bypass ", invokedArgs[3]);
            Assert.AreEqual("-WindowStyle Hidden ", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-File \"{0}\"",
                    scriptPath),
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
        public void ExecuteWithErrorsAsWarnings()
        {
            var scriptPath = Path.Combine(
                Assembly.GetExecutingAssembly().LocalDirectoryPath(),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.ps1",
                    Guid.NewGuid().ToString()));

            var text = "hello world";
            var errorText = "It is all wrong";
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("$ErrorActionPreference = 'Continue'");
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Write-Error '{0}'",
                        errorText));
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Write-Output '{0}'",
                        text));
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

                            err(null, CreateDataReceivedEventArgs(errorText));
                        });
            }

            var task = new InvokePowershellFile(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = true;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(string.Empty, task.Output);

            Assert.AreEqual(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", invokedPath);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("-NoLogo ", invokedArgs[0]);
            Assert.AreEqual("-NonInteractive ", invokedArgs[1]);
            Assert.AreEqual("-NoProfile ", invokedArgs[2]);
            Assert.AreEqual("-ExecutionPolicy Bypass ", invokedArgs[3]);
            Assert.AreEqual("-WindowStyle Hidden ", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-File \"{0}\"",
                    scriptPath),
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
        public void ExecuteWithMissingFile()
        {
            var scriptPath = Path.Combine(
                Assembly.GetExecutingAssembly().LocalDirectoryPath(),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.ps1",
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

            var task = new InvokePowershellFile(invoker.Object);
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Script = new TaskItem(scriptPath);
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
        }
    }
}
