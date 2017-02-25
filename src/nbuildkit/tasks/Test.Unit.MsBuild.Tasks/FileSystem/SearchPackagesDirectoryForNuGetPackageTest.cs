//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
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
    public sealed class SearchPackagesDirectoryForNuGetPackageTest : TaskTest
    {
        [Test]
        public void ExecuteWithMultipleVersions()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.C.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "d:\\mock\\packages";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddDirectory(Path.Combine(packagesDirectory, package));
                }
            }

            var task = new SearchPackagesDirectoryForNuGetPackage(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.PackageToLocate = "A.B";

            var result = task.Execute();
            Assert.IsTrue(result);

            var output = task.Path;
            Assert.AreEqual(Path.Combine(packagesDirectory, knownPackages[2]), output.ItemSpec);
        }

        [Test]
        public void ExecuteWithoutVersion()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.C.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "d:\\mock\\packages";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddDirectory(Path.Combine(packagesDirectory, package));
                }
            }

            var task = new SearchPackagesDirectoryForNuGetPackage(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.PackageToLocate = "F.G";

            var result = task.Execute();
            Assert.IsTrue(result);

            var output = task.Path;
            Assert.IsNull(output);
        }

        [Test]
        public void ExecuteWithSingleVersion()
        {
            InitializeBuildEngine();

            var knownPackages = new[]
            {
                "A.B.1.0.0",
                "A.B.1.1.0",
                "A.B.1.1.1",
                "A.C.1.0.0",
                "D.E.1.0.0",
            };

            var packagesDirectory = "d:\\mock\\packages";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddDirectory(Path.Combine(packagesDirectory, package));
                }
            }

            var task = new SearchPackagesDirectoryForNuGetPackage(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.PackageToLocate = "A.C";

            var result = task.Execute();
            Assert.IsTrue(result);

            var output = task.Path;
            Assert.AreEqual(Path.Combine(packagesDirectory, knownPackages[3]), output.ItemSpec);
        }

        [Test]
        public void ExecuteWithSubPackages()
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

            var packagesDirectory = "d:\\mock\\packages";
            var fileSystem = new MockFileSystem();
            {
                fileSystem.AddDirectory(packagesDirectory);
                foreach (var package in knownPackages)
                {
                    fileSystem.AddDirectory(Path.Combine(packagesDirectory, package));
                }
            }

            var task = new SearchPackagesDirectoryForNuGetPackage(fileSystem);
            task.BuildEngine = BuildEngine.Object;
            task.PackagesDirectory = new TaskItem(packagesDirectory);
            task.PackageToLocate = "A.B";

            var result = task.Execute();
            Assert.IsTrue(result);

            var output = task.Path;
            Assert.AreEqual(Path.Combine(packagesDirectory, knownPackages[2]), output.ItemSpec);
        }
    }
}
