//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    [TestFixture]
    public sealed class ReadHashesFromFileTest : TaskTest
    {
        [Test]
        public void ExecuteWithEmptyPath()
        {
            InitializeBuildEngine();

            var task = new ReadHashesFromFile();
            task.BuildEngine = BuildEngine.Object;
            task.Path = new TaskItem(string.Empty);

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithMultipleEntries()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.json",
                    Guid.NewGuid().ToString()));

            var text =
@"[
{ ""IsEndValue"": false, ""Algorithm"": ""SHA256"", ""File"": ""c:\\temp\\myfile1.txt"", ""Hash"":""AB"" },
{ ""IsEndValue"": false, ""Algorithm"": ""SHA384"", ""File"": ""c:\\temp\\myfile2.txt"", ""Hash"":""CD"" },
{ ""IsEndValue"": false, ""Algorithm"": ""SHA512"", ""File"": ""c:\\temp\\myfile3.txt"", ""Hash"":""EF"" },
{ ""IsEndValue"": true }
]
";
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine(text);
            }

            InitializeBuildEngine();

            var task = new ReadHashesFromFile();
            task.BuildEngine = BuildEngine.Object;
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            var hashes = task.Hashes;
            Assert.AreEqual(3, hashes.Length);
            Assert.AreEqual(@"c:\temp\myfile1.txt", hashes[0].ItemSpec);
            Assert.AreEqual("SHA256", hashes[0].GetMetadata("Algorithm"));
            Assert.AreEqual("AB", hashes[0].GetMetadata("Hash"));

            Assert.AreEqual(@"c:\temp\myfile2.txt", hashes[1].ItemSpec);
            Assert.AreEqual("SHA384", hashes[1].GetMetadata("Algorithm"));
            Assert.AreEqual("CD", hashes[1].GetMetadata("Hash"));

            Assert.AreEqual(@"c:\temp\myfile3.txt", hashes[2].ItemSpec);
            Assert.AreEqual("SHA512", hashes[2].GetMetadata("Algorithm"));
            Assert.AreEqual("EF", hashes[2].GetMetadata("Hash"));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithNonExistingPath()
        {
            InitializeBuildEngine();

            var task = new ReadHashesFromFile();
            task.BuildEngine = BuildEngine.Object;
            task.Path = new TaskItem(@"c:\this\path\does\not\exist.txt");

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithNullPath()
        {
            InitializeBuildEngine();

            var task = new ReadHashesFromFile();
            task.BuildEngine = BuildEngine.Object;
            task.Path = null;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithoutEntries()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.json",
                    Guid.NewGuid().ToString()));

            var text =
@"[
{ ""IsEndValue"": true }
]
";
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine(text);
            }

            InitializeBuildEngine();

            var task = new ReadHashesFromFile();
            task.BuildEngine = BuildEngine.Object;
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            var hashes = task.Hashes;
            Assert.AreEqual(0, hashes.Length);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithSingleEntry()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(
                directory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.json",
                    Guid.NewGuid().ToString()));

            var text =
@"[
{ ""IsEndValue"": false, ""Algorithm"": ""SHA256"", ""File"": ""c:\\temp\\myfile.txt"", ""Hash"":""ABCDEFGH"" },
{ ""IsEndValue"": true }
]
";
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine(text);
            }

            InitializeBuildEngine();

            var task = new ReadHashesFromFile();
            task.BuildEngine = BuildEngine.Object;
            task.Path = new TaskItem(filePath);

            var result = task.Execute();
            Assert.IsTrue(result);

            var hashes = task.Hashes;
            Assert.AreEqual(1, hashes.Length);
            Assert.AreEqual(@"c:\temp\myfile.txt", hashes[0].ItemSpec);
            Assert.AreEqual("SHA256", hashes[0].GetMetadata("Algorithm"));
            Assert.AreEqual("ABCDEFGH", hashes[0].GetMetadata("Hash"));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }
    }
}
