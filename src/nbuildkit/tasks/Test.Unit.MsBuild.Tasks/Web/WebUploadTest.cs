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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
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
    public sealed class WebUploadTest : TaskTest
    {
        [Test]
        public void Execute()
        {
            var fileToUpload = Assembly.GetExecutingAssembly().LocalFilePath();

            var targetUri = "http://www.example.com/mypath";

            var webClient = new Mock<IInternalWebClient>();
            {
                webClient.Setup(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<Uri, string, string>(
                        (uri, method, path) =>
                        {
                            Assert.AreEqual(new Uri(targetUri + "/" + Path.GetFileName(path)), uri);
                            Assert.AreEqual(fileToUpload, path);
                        })
                    .Returns(Array.Empty<byte>())
                    .Verifiable();
            }

            Func<IInternalWebClient> builder = () => webClient.Object;

            InitializeBuildEngine();

            var task = new WebUpload(builder);
            task.BuildEngine = BuildEngine.Object;
            task.Items = new ITaskItem[] { new TaskItem(fileToUpload) };
            task.Source = new TaskItem(targetUri);
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            webClient.Verify(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 3);
        }

        [Test]
        public void ExecuteWithEmptyUrl()
        {
            var fileToUpload = Assembly.GetExecutingAssembly().LocalFilePath();

            var webClient = new Mock<IInternalWebClient>();
            {
                webClient.Setup(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Verifiable();
            }

            Func<IInternalWebClient> builder = () => webClient.Object;

            InitializeBuildEngine();

            var task = new WebUpload(builder);
            task.BuildEngine = BuildEngine.Object;
            task.Items = new ITaskItem[] { new TaskItem(fileToUpload) };
            task.Source = new TaskItem(string.Empty);
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            webClient.Verify(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithInvalidUrl()
        {
            var fileToUpload = Assembly.GetExecutingAssembly().LocalFilePath();

            var webClient = new Mock<IInternalWebClient>();
            {
                webClient.Setup(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Verifiable();
            }

            Func<IInternalWebClient> builder = () => webClient.Object;

            InitializeBuildEngine();

            var task = new WebUpload(builder);
            task.BuildEngine = BuildEngine.Object;
            task.Items = new ITaskItem[] { new TaskItem(fileToUpload) };
            task.Source = new TaskItem("this is not a valid URL");
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            webClient.Verify(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithoutItems()
        {
            var webClient = new Mock<IInternalWebClient>();
            {
                webClient.Setup(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Verifiable();
            }

            Func<IInternalWebClient> builder = () => webClient.Object;

            InitializeBuildEngine();

            var task = new WebUpload(builder);
            task.BuildEngine = BuildEngine.Object;
            task.Items = Array.Empty<ITaskItem>();
            task.Source = new TaskItem("http://www.microsoft.com/default.aspx");
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            webClient.Verify(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithoutUrl()
        {
            var fileToUpload = Assembly.GetExecutingAssembly().LocalFilePath();

            var webClient = new Mock<IInternalWebClient>();
            {
                webClient.Setup(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Verifiable();
            }

            Func<IInternalWebClient> builder = () => webClient.Object;

            InitializeBuildEngine();

            var task = new WebUpload(builder);
            task.BuildEngine = BuildEngine.Object;
            task.Items = new ITaskItem[] { new TaskItem(fileToUpload) };
            task.UseDefaultCredentials = false;

            var result = task.Execute();
            Assert.IsFalse(result, "Expected the task to not finish successfully");

            webClient.Verify(w => w.UploadFile(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }
    }
}
