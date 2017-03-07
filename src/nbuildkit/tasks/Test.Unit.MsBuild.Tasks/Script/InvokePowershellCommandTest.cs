//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

            var task = new InvokePowershellCommand();
            task.BuildEngine = BuildEngine.Object;
            task.IgnoreErrors = false;

            task.Command = command;
            var result = task.Execute();

            Assert.IsTrue(result);

            var output = task.Output;
            Assert.AreEqual(text, output);

            VerifyNumberOfLogMessages(numberOfNormalMessages: 2);
        }

        [Test]
        public void ExecuteWithErrors()
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
