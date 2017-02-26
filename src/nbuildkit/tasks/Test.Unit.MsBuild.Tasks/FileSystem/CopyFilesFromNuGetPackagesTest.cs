//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class CopyFilesFromNuGetPackagesTest : TaskTest
    {
        [Test]
        public void ExecuteWithMultipleNuGetPackagesAndMultipleDestinations()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.B.C.1.0.0",
                "A.B.D.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "z:\\mock\\packages";
            var destination1 = "x:\\mock\\destination";
            var destination2 = "y:\\mock\\destination";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddFile(
                        Path.Combine(
                            packagesDirectory,
                            package,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}.txt",
                                package)),
                        new MockFileData(package));
                }

                fileSystem.AddDirectory(destination1);
                fileSystem.AddDirectory(destination2);
            }

            var task = new CopyFilesFromNuGetPackages(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.Items = new ITaskItem[]
            {
                new TaskItem(
                    "A.B",
                    new Hashtable
                    {
                        {
                            "Include",
                            "*.*"
                        },
                        {
                            "Destinations",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0};{1}",
                                destination1,
                                destination2)
                        }
                    }),
                new TaskItem(
                    "A.B.C",
                    new Hashtable
                    {
                        {
                            "Include",
                            "*.*"
                        },
                        {
                            "Destinations",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0};{1}",
                                destination1,
                                destination2)
                        }
                    }),
            };

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[2])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination1,
                    knownPackages[2]));
            Assert.AreEqual(
                knownPackages[2],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[2])));
            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[2])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination2,
                    knownPackages[2]));
            Assert.AreEqual(
                knownPackages[2],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[2])));

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[3])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination1,
                    knownPackages[3]));
            Assert.AreEqual(
                knownPackages[3],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[3])));
            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[3])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination2,
                    knownPackages[3]));
            Assert.AreEqual(
                knownPackages[3],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[3])));
        }

        [Test]
        public void ExecuteWithMultipleNuGetPackagesAndSingleDestination()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.B.C.1.0.0",
                "A.B.D.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "z:\\mock\\packages";
            var destination1 = "x:\\mock\\destination";
            var destination2 = "y:\\mock\\destination";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddFile(
                        Path.Combine(
                            packagesDirectory,
                            package,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}.txt",
                                package)),
                        new MockFileData(package));
                }

                fileSystem.AddDirectory(destination1);
                fileSystem.AddDirectory(destination2);
            }

            var task = new CopyFilesFromNuGetPackages(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.Items = new ITaskItem[]
            {
                new TaskItem(
                    "A.B",
                    new Hashtable
                    {
                        {
                            "Include",
                            "*.*"
                        },
                        {
                            "Destinations",
                            destination1
                        }
                    }),
                new TaskItem(
                    "A.B.C",
                    new Hashtable
                    {
                        {
                            "Include",
                            "*.*"
                        },
                        {
                            "Destinations",
                            destination2
                        }
                    }),
            };

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[2])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination1,
                    knownPackages[2]));
            Assert.AreEqual(
                knownPackages[2],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[2])));

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[3])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination2,
                    knownPackages[3]));
            Assert.AreEqual(
                knownPackages[3],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[3])));
        }

        [Test]
        public void ExecuteWithSingleNuGetPackageAndMultipleDestinations()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.B.C.1.0.0",
                "A.B.D.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "z:\\mock\\packages";
            var destination1 = "x:\\mock\\destination";
            var destination2 = "y:\\mock\\destination";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddFile(
                        Path.Combine(
                            packagesDirectory,
                            package,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}.txt",
                                package)),
                        new MockFileData(package));
                }

                fileSystem.AddDirectory(destination1);
                fileSystem.AddDirectory(destination2);
            }

            var task = new CopyFilesFromNuGetPackages(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.Items = new ITaskItem[]
            {
                new TaskItem(
                    "A.B",
                    new Hashtable
                    {
                        {
                            "Include",
                            "*.*"
                        },
                        {
                            "Destinations",
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0};{1}",
                                destination1,
                                destination2)
                        }
                    }),
            };

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[2])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination1,
                    knownPackages[2]));
            Assert.AreEqual(
                knownPackages[2],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination1,
                        knownPackages[2])));

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[2])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination2,
                    knownPackages[2]));
            Assert.AreEqual(
                knownPackages[2],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination2,
                        knownPackages[2])));
        }

        [Test]
        public void ExecuteWithSingleNuGetPackageAndSingleDestination()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.B.C.1.0.0",
                "A.B.D.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "z:\\mock\\packages";
            var destination = "y:\\mock\\destination";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddFile(
                        Path.Combine(
                            packagesDirectory,
                            package,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}.txt",
                                package)),
                        new MockFileData(package));
                }

                fileSystem.AddDirectory(destination);
            }

            var task = new CopyFilesFromNuGetPackages(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.Items = new ITaskItem[]
            {
                new TaskItem(
                    "A.B",
                    new Hashtable
                    {
                        {
                            "Include",
                            "*.*"
                        },
                        {
                            "Destinations",
                            destination
                        }
                    }),
            };

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsTrue(
                fileSystem.File.Exists(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination,
                        knownPackages[2])),
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Expected a file to exist at: {0}\{1}.txt",
                    destination,
                    knownPackages[2]));
            Assert.AreEqual(
                knownPackages[2],
                fileSystem.File.ReadAllText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"{0}\{1}.txt",
                        destination,
                        knownPackages[2])));
        }
    }
}
