//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class NuGetHelpersTest
    {
        [Test]
        public void HighestPackageVersionDirectoryForWithMultipleVersions()
        {
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

            var result = NugetHelpers.HighestPackageVersionDirectoryFor("A.B", packagesDirectory, fileSystem, (i, m) => { });
            Assert.AreEqual(Path.Combine(packagesDirectory, knownPackages[2]), result);
        }

        [Test]
        public void HighestPackageVersionDirectoryForWithoutPackageInPath()
        {
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

            var result = NugetHelpers.HighestPackageVersionDirectoryFor("A.D", packagesDirectory, fileSystem, (i, m) => { });
            Assert.IsNull(result);
        }

        [Test]
        public void HighestPackageVersionDirectoryForWithoutVersionInPath()
        {
            var knownPackages = new[]
            {
                "A.B",
                "A.C",
                "D.E",
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

            var result = NugetHelpers.HighestPackageVersionDirectoryFor("A.B", packagesDirectory, fileSystem, (i, m) => { });
            Assert.AreEqual(Path.Combine(packagesDirectory, knownPackages[0]), result);
        }

        [Test]
        public void HighestPackageVersionDirectoryForWithSingleVersion()
        {
            var knownPackages = new[]
            {
                "A.B.1.0.0",
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

            var result = NugetHelpers.HighestPackageVersionDirectoryFor("A.B", packagesDirectory, fileSystem, (i, m) => { });
            Assert.AreEqual(Path.Combine(packagesDirectory, knownPackages[0]), result);
        }
    }
}
