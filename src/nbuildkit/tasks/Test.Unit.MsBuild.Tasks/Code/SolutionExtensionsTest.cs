// <copyright file="SolutionExtensionsTest.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Code
{
    [TestFixture]
    public sealed class SolutionExtensionsTest
    {
        [Test]
        public void GetProjects()
        {
        }

        [Test]
        public void GetProjectsWithEmptySolution()
        {
            // The current assembly will live in the bin folder
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();

            // Go up 2 directories
            var projectDirectory = Path.GetDirectoryName(Path.GetDirectoryName(directory));
            var solutionFile = Path.Combine(projectDirectory, "TestFiles", "EmptySolution", "EmptySolution.sln");

            var projects = SolutionExtensions.GetProjects(solutionFile);
            Assert.IsNotNull(projects);
            Assert.AreEqual(0, projects.Count());
        }
    }
}
