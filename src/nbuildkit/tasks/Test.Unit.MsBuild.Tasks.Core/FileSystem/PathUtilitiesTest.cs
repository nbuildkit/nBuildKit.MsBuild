﻿//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static string CreateTempDirectory()
        {
            var assemblyDirectory = LocalDirectoryPath(Assembly.GetExecutingAssembly());
            var path = Path.Combine(assemblyDirectory, Guid.NewGuid().ToString());
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);

            return path;
        }

        private static string LocalDirectoryPath(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            // Get the location of the assembly before it was shadow-copied
            // Note that Assembly.Codebase gets the path to the manifest-containing
            // file, not necessarily the path to the file that contains a
            // specific type.
            var uncPath = new Uri(assembly.CodeBase);

            // Get the local path. This may not work if the assembly isn't
            // local. For now we assume it is.
            return Path.GetDirectoryName(uncPath.LocalPath);
        }

        private static void CreateTempFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, Path.GetFileName(path));
        }

        [Test]
        public void AppendDirectorySeparatorCharToDirectory()
        {
            var pathWithoutSeparator = @"c:\temp";
            var pathWithSeparator = @"c:\temp\";
            Assert.AreEqual(pathWithSeparator, PathUtilities.AppendDirectorySeparatorCharToDirectory(pathWithoutSeparator));
            Assert.AreEqual(pathWithSeparator, PathUtilities.AppendDirectorySeparatorCharToDirectory(pathWithSeparator));
        }

        [Test]
        public void BaseDirectory()
        {
            var expectedPath = @"c:\temp";
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\file.txt", true));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\"));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp"));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\**\file.txt", true));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\**\*.*", true));
            Assert.AreEqual(expectedPath, PathUtilities.BaseDirectory(@"c:\temp\**\bin\**\file.txt", true));
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
        public void GetDirectoryPathRelativeToDirectory()
        {
            Assert.AreEqual(@"temp\", PathUtilities.GetDirectoryPathRelativeToDirectory(@"c:\temp\", @"c:\"));
            Assert.AreEqual(@"..\temp\", PathUtilities.GetDirectoryPathRelativeToDirectory(@"c:\temp\", @"c:\other\"));
            Assert.AreEqual(@"..\other\temp\", PathUtilities.GetDirectoryPathRelativeToDirectory(@"c:\other\temp\", @"c:\sub"));
        }

        [Test]
        public void GetFilePathRelativeToDirectory()
        {
            Assert.AreEqual(@"file.txt", PathUtilities.GetFilePathRelativeToDirectory(@"c:\temp\file.txt", @"c:\temp"));
            Assert.AreEqual(@"..\file.txt", PathUtilities.GetFilePathRelativeToDirectory(@"c:\temp\file.txt", @"c:\temp\other"));
            Assert.AreEqual(@"..\other\file.txt", PathUtilities.GetFilePathRelativeToDirectory(@"c:\temp\other\file.txt", @"c:\temp\sub"));
        }

        [Test]
        public void IncludedPathsWithAbsolutePaths()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "other path", "temp", "file.txt");
            CreateTempFile(file2);

            var file3 = Path.Combine(directory, "file.txt");
            CreateTempFile(file3);

            Assert.That(
                PathUtilities.IncludedPaths(file3, directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file3,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(file1, directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\\temp\\*.txt",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\\**\\*.txt",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                        file3,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\\other*\\**\\*.txt",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\\other\\**\\*.txt",
                        directory),
                    directory),
                Is.EquivalentTo(
                    Array.Empty<string>()));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\\**\\temp\\*.txt",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
        }

        [Test]
        public void IncludedPathsWithAbsolutePathsAndUnnecessaryWhiteSpace()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "other path", "temp", "file.txt");
            CreateTempFile(file2);

            var file3 = Path.Combine(directory, "file.txt");
            CreateTempFile(file3);

            Assert.That(
                PathUtilities.IncludedPaths(file3 + " ", directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file3,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(" " + file1, directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        " {0}\\temp\\*.txt ",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "  {0}\\**\\*.txt  ",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                        file3,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        " {0}\\other*\\**\\*.txt ",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        " {0}\\other\\**\\*.txt ",
                        directory),
                    directory),
                Is.EquivalentTo(
                    Array.Empty<string>()));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "      {0}\\**\\temp\\*.txt  ",
                        directory),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
        }

        [Test]
        public void IncludedPathsWithExclusionsAndAbsolutePaths()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "temp", "other.txt");
            CreateTempFile(file2);

            var file3 = Path.Combine(directory, "other path", "temp", "file.txt");
            CreateTempFile(file3);

            var file4 = Path.Combine(directory, "other", "temp", "other.txt");
            CreateTempFile(file4);

            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\**\*.txt",
                        directory),
                    Enumerable.Empty<string>(),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                        file3,
                        file4,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\**\*.txt",
                        directory),
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\other*\**\*.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\**\*.txt",
                        directory),
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\temp\other.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                        file4,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\**\*.txt",
                        directory),
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\**\other.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                    }));
        }

        [Test]
        public void IncludedPathsWithExclusionsAndAbsolutePathsAndUnnecessaryWhiteSpace()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "temp", "other.txt");
            CreateTempFile(file2);

            var file3 = Path.Combine(directory, "other path", "temp", "file.txt");
            CreateTempFile(file3);

            var file4 = Path.Combine(directory, "other", "temp", "other.txt");
            CreateTempFile(file4);

            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @" {0}\**\*.txt ",
                        directory),
                    Enumerable.Empty<string>(),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                        file3,
                        file4,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\**\*.txt ",
                        directory),
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @" {0}\other*\**\*.* ",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @" {0}\**\*.txt",
                        directory),
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\temp\other.* ",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                        file4,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\**\*.txt   ",
                        directory),
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"  {0}\**\other.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                    }));
        }

        [Test]
        public void IncludedPathsWithExclusionsAndRelativePaths()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "temp", "other.txt");
            CreateTempFile(file2);

            var file3 = Path.Combine(directory, "other", "temp", "file.txt");
            CreateTempFile(file3);

            var file4 = Path.Combine(directory, "other", "temp", "other.txt");
            CreateTempFile(file4);

            Assert.That(
                PathUtilities.IncludedPaths(
                    @"**\*.txt",
                    Enumerable.Empty<string>(),
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                        file3,
                        file4,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    @"**\*.txt",
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\other\**\*.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    @"**\*.txt",
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\temp\other.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                        file4,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths(
                    @"**\*.txt",
                    new[]
                        {
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"{0}\**\other.*",
                                directory),
                        },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                    }));
        }

        [Test]
        public void IncludedPathsWithFileExclusions()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "temp", "other.txt");
            CreateTempFile(file2);

            var file3 = Path.Combine(directory, "other", "temp", "file.txt");
            CreateTempFile(file3);

            var file4 = Path.Combine(directory, "other", "temp", "other.txt");
            CreateTempFile(file4);

            Assert.That(
                PathUtilities.IncludedPaths(
                    @"**\*.txt",
                    new[] { "**\\other.txt" },
                    directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file3,
                    }));
        }

        [Test]
        public void IncludedPathsWithRelativePaths()
        {
            var directory = CreateTempDirectory();
            var file1 = Path.Combine(directory, "temp", "file.txt");
            CreateTempFile(file1);

            var file2 = Path.Combine(directory, "other", "temp", "file.txt");
            CreateTempFile(file2);

            Assert.That(
                PathUtilities.IncludedPaths(PathUtilities.GetFilePathRelativeToDirectory(file1, directory), directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths("temp\\*.txt", directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths("**\\*.txt", directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths("other\\**\\*.txt", directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file2,
                    }));
            Assert.That(
                PathUtilities.IncludedPaths("**\\temp\\*.txt", directory),
                Is.EquivalentTo(
                    new[]
                    {
                        file1,
                        file2,
                    }));
        }
    }
}
