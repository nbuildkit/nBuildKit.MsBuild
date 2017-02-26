//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Core.FileSystem
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class PathUtilitiesTest
    {
        [Test]
        public void AppendDirectorySeparatorChar()
        {
            var pathWithoutSeparator = @"c:\temp";
            var pathWithSeparator = @"c:\temp\";
            Assert.AreEqual(pathWithSeparator, PathUtilities.AppendDirectorySeparatorChar(pathWithoutSeparator));
            Assert.AreEqual(pathWithSeparator, PathUtilities.AppendDirectorySeparatorChar(pathWithSeparator));
        }

        [Test]
        public void BaseDirectory()
        {
            var expectedPath = @"c:\temp";
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\file.txt"));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\**\file.txt"));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\**\*.*"));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\**\bin\**\file.txt"));
        }

        [Test]
        public void GetAbsolutePathWithBasePath()
        {
            var expectedPath = @"c:\temp\file.txt";
            Assert.AreEqual(expectedPath, PathUtilities.GetAbsolutePath(expectedPath, @"c:\temp"));
            Assert.AreEqual(expectedPath, PathUtilities.GetAbsolutePath("file.txt", @"c:\temp"));
            Assert.AreEqual(expectedPath, PathUtilities.GetAbsolutePath(@"..\file.txt", @"c:\temp\subpath"));

            Assert.AreEqual(string.Empty, PathUtilities.GetAbsolutePath(null, @"c:\temp"));
            Assert.AreEqual(string.Empty, PathUtilities.GetAbsolutePath(string.Empty, @"c:\temp"));
        }

        [Test]
        public void GetAbsolutePathWithoutBasePath()
        {
            var expectedPath = @"c:\temp\file.txt";
            Assert.AreEqual(expectedPath, PathUtilities.GetAbsolutePath(expectedPath));

            Assert.AreEqual(string.Empty, PathUtilities.GetAbsolutePath(null));
            Assert.AreEqual(string.Empty, PathUtilities.GetAbsolutePath(string.Empty));
        }

        [Test]
        public void GetRelativeDirectoryPath()
        {
            Assert.AreEqual(@"temp\", PathUtilities.GetRelativeDirectoryPath(@"c:\temp\", @"c:\"));
            Assert.AreEqual(@"..\temp\", PathUtilities.GetRelativeDirectoryPath(@"c:\temp\", @"c:\other\"));
            Assert.AreEqual(@"..\other\temp\", PathUtilities.GetRelativeDirectoryPath(@"c:\other\temp\", @"c:\sub"));
        }

        [Test]
        public void GetFilePathRelativeToDirectory()
        {
            Assert.AreEqual(@"file.txt", PathUtilities.GetFilePathRelativeToDirectory(@"c:\temp\file.txt", @"c:\temp"));
            Assert.AreEqual(@"..\file.txt", PathUtilities.GetFilePathRelativeToDirectory(@"c:\temp\file.txt", @"c:\temp\other"));
            Assert.AreEqual(@"..\other\file.txt", PathUtilities.GetFilePathRelativeToDirectory(@"c:\temp\other\file.txt", @"c:\temp\sub"));
        }

        [Test]
        public void IncludedPaths()
        {
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddFile(@"c:\temp\file.txt", new MockFileData("a"));
                fileSystem.AddFile(@"c:\other\temp\file.txt", new MockFileData("b"));
            }

            Assert.That(
                PathUtilities.IncludedPaths(@"c:\temp\file.txt", fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt"
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(@"c:\temp\*.txt", fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt"
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(@"c:\**\*.txt", fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt",
                        @"c:\other\temp\file.txt"
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(@"c:\other\**\*.txt", fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\other\temp\file.txt"
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(@"c:\**\temp\*.txt", fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt",
                        @"c:\other\temp\file.txt"
                    }));
        }

        [Test]
        public void IncludedPathsWithExclusions()
        {
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddFile(@"c:\temp\file.txt", new MockFileData("a"));
                fileSystem.AddFile(@"c:\temp\other.txt", new MockFileData("a"));
                fileSystem.AddFile(@"c:\other\temp\file.txt", new MockFileData("b"));
                fileSystem.AddFile(@"c:\other\temp\other.txt", new MockFileData("b"));
            }

            Assert.That(
                PathUtilities.IncludedPaths(@"c:\**\*.txt", Enumerable.Empty<string>(), fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt",
                        @"c:\temp\other.txt",
                        @"c:\other\temp\file.txt",
                        @"c:\other\temp\other.txt",
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(@"c:\**\*.txt", new[] { @"c:\other" }, fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt",
                        @"c:\temp\other.txt",
                    }));
        }

        [Test]
        [Ignore("Ignoring files without directories doesn't work yet.")]
        public void IncludedPathsWithFileExclusions()
        {
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddFile(@"c:\temp\file.txt", new MockFileData("a"));
                fileSystem.AddFile(@"c:\temp\other.txt", new MockFileData("a"));
                fileSystem.AddFile(@"c:\other\temp\file.txt", new MockFileData("b"));
                fileSystem.AddFile(@"c:\other\temp\other.txt", new MockFileData("b"));
            }

            // This doesn't work yet
            Assert.That(
                PathUtilities.IncludedPaths(@"c:\**\*.txt", new[] { "other.txt" }, fileSystem),
                Is.EquivalentTo(
                    new[]
                    {
                        @"c:\temp\file.txt",
                        @"c:\other\temp\file.txt",
                    }));
        }
    }
}
