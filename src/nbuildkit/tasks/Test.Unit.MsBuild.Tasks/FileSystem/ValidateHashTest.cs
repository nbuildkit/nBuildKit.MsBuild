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
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class ValidateHashTest : TaskTest
    {
        [Test]
        public void ExecuteWithEmptyPath()
        {
            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "md5";
            task.Path = new TaskItem(string.Empty);

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithHashFile()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var testDirectory = Path.Combine(directory, Guid.NewGuid().ToString());
            if (!Directory.Exists(testDirectory))
            {
                Directory.CreateDirectory(testDirectory);
            }

            var hashFile = Path.Combine(testDirectory, "hashfile.txt");
            var fileName = "FileToHash.txt";
            using (var writer = new StreamWriter(hashFile))
            {
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1}",
                        "B5D4379F6B3E960EA12132B34E8E65C9",
                        fileName));
            }

            var filePath = Path.Combine(directory, fileName);

            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "md5";
            task.HashFile = new TaskItem(hashFile);
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 2);
        }

        [Test]
        public void ExecuteWithInvalidHashAlgorithm()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "FileToHash.txt");

            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "stuff";
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 2, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithMD5()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "FileToHash.txt");

            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "md5";
            task.Hash = "B5D4379F6B3E960EA12132B34E8E65C9";
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithNonExistingPath()
        {
            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "md5";
            task.Path = new TaskItem(@"c:\this\path\does\not\exist.txt");

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithNullPath()
        {
            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "md5";
            task.Path = null;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithSHA1()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "FileToHash.txt");

            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "sha1";
            task.Hash = "FFC0F8E69E3753E3A4087E197C160261F3EF11D5";
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithSHA256()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "FileToHash.txt");

            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "sha256";
            task.Hash = "A65235E41B8072B726FDEBB63DEEF3EBDFE4FED516F510608DA6BB1497ED11BA";
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithSHA512()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "FileToHash.txt");

            InitializeBuildEngine();

            var task = new ValidateHash();
            task.BuildEngine = BuildEngine.Object;
            task.Algorithm = "sha512";
            task.Hash = "9E340E6C4B70FA6E9F1F5D25859F57E1FCC7A0B3A49B94AE5B3E64090FCC4CA4E2490E10556C85E0B84F634B68EC6C7312F834A875A76EC5996ABDED70C214F1";
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }
    }
}
