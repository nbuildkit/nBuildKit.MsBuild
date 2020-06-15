// <copyright file="SolutionExtensionsTest.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Code
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class SolutionExtensionsTest
    {
        [Test]
        public void GetProjects()
        {
            // The current assembly will live in the bin folder
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();

            var solutionFile = Path.Combine(directory, "TestFiles", "OldSolution", "OldSolution.sln");

            var projects = SolutionExtensions.GetProjects(solutionFile);
            Assert.IsNotNull(projects);
            Assert.AreEqual(2, projects.Count());
        }

        [Test]
        public void GetProjectsWithEmptySolution()
        {
            // The current assembly will live in the bin folder
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();

            var solutionFile = Path.Combine(directory, "TestFiles", "EmptySolution", "EmptySolution.sln");

            var projects = SolutionExtensions.GetProjects(solutionFile);
            Assert.IsNotNull(projects);
            Assert.AreEqual(0, projects.Count());
        }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // nUnit3 doesn't set the current directory anymore:
            // https://github.com/nunit/nunit/issues/1072
            // Le sigh ...
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }
    }
}
