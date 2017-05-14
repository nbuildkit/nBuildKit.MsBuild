//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Web
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class WebDownloadTest : TaskTest
    {
        [Test]
        public void Execute()
        {
            var baseDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var targetDirectory = Path.Combine(baseDirectory, Guid.NewGuid().ToString());

            InitializeBuildEngine();

            var task = new WebDownload();
            task.BuildEngine = BuildEngine.Object;
            task.DestinationDirectory = new TaskItem(targetDirectory);
            task.Source = new TaskItem("http://www.microsoft.com/default.aspx");
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            Assert.IsTrue(Directory.Exists(targetDirectory), "Expected the task to create the target directory");

            var file = Path.Combine(targetDirectory, "default.aspx");
            Assert.IsTrue(File.Exists(file), "Expected the task to download the file");
            Assert.Greater(new FileInfo(file).Length, 0);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 2);
        }

        [Test]
        public void ExecuteWithEmptyTargetDirectory()
        {
            var baseDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var targetDirectory = Path.Combine(baseDirectory, Guid.NewGuid().ToString());

            InitializeBuildEngine();

            var task = new WebDownload();
            task.BuildEngine = BuildEngine.Object;
            task.DestinationDirectory = new TaskItem(string.Empty);
            task.Source = new TaskItem("http://www.microsoft.com/default.aspx");
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            Assert.IsFalse(Directory.Exists(targetDirectory), "Expected the task to not create the target directory");

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithEmptyUrl()
        {
            var baseDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var targetDirectory = Path.Combine(baseDirectory, Guid.NewGuid().ToString());

            InitializeBuildEngine();

            var task = new WebDownload();
            task.BuildEngine = BuildEngine.Object;
            task.DestinationDirectory = new TaskItem(targetDirectory);
            task.Source = new TaskItem(string.Empty);
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            Assert.IsFalse(Directory.Exists(targetDirectory), "Expected the task to not create the target directory");

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithInvalidUrl()
        {
            var baseDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var targetDirectory = Path.Combine(baseDirectory, Guid.NewGuid().ToString());

            InitializeBuildEngine();

            var task = new WebDownload();
            task.BuildEngine = BuildEngine.Object;
            task.DestinationDirectory = new TaskItem(targetDirectory);
            task.Source = new TaskItem("this is not a valid URL");
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            Assert.IsFalse(Directory.Exists(targetDirectory), "Expected the task to not create the target directory");

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithoutTargetDirectory()
        {
            var baseDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var targetDirectory = Path.Combine(baseDirectory, Guid.NewGuid().ToString());

            InitializeBuildEngine();

            var task = new WebDownload();
            task.BuildEngine = BuildEngine.Object;
            task.Source = new TaskItem("http://www.microsoft.com/default.aspx");
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            Assert.IsFalse(Directory.Exists(targetDirectory), "Expected the task to not create the target directory");

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithoutUrl()
        {
            var baseDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var targetDirectory = Path.Combine(baseDirectory, Guid.NewGuid().ToString());

            InitializeBuildEngine();

            var task = new WebDownload();
            task.BuildEngine = BuildEngine.Object;
            task.DestinationDirectory = new TaskItem(targetDirectory);
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            Assert.IsFalse(Directory.Exists(targetDirectory), "Expected the task to not create the target directory");

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }
    }
}
