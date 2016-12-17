//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Utilities;
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

            var task = new InvokePowershellFile();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();

            Assert.IsTrue(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            VerifyNumberOfLogMessages(numberOfNormalMessages: 7);
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

            var task = new InvokePowershellFile();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();

            Assert.IsFalse(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            // Expecting more than one message because powershell adds additional information to the error.
            VerifyNumberOfLogMessages(numberOfErrorMessages: 5, numberOfNormalMessages: 7);
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

            var task = new InvokePowershellFile();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = true;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();

            Assert.IsTrue(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            VerifyNumberOfLogMessages(numberOfWarningMessages: 5, numberOfNormalMessages: 7);
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

            var task = new InvokePowershellFile();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Script = new TaskItem(scriptPath);
            var result = task.Execute();

            Assert.IsFalse(result);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfNormalMessages: 1);
        }
    }
}
