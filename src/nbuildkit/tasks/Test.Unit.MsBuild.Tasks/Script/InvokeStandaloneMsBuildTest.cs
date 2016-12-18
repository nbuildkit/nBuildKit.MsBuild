//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class InvokeStandaloneMsBuildTest : TaskTest
    {
        [Test]
        public void ExecuteWithFailingProject()
        { }

        [Test]
        public void ExecuteWithMissingProjects()
        { }

        [Test]
        public void ExecuteWithMissingProjectsAndSkipNonexistantProjects()
        { }

        [Test]
        public void ExecuteWithMultipleProjects()
        { }

        [Test]
        public void ExecuteWithMultipleProjectsAndFailingProjects()
        { }

        [Test]
        public void ExecuteWithMultipleProjectsAndFailingProjectsAndStopOnFirstFailure()
        { }

        [Test]
        public void ExecuteWithProjectWithNoOutput()
        { }

        [Test]
        public void ExecuteWithProjectWithOutput()
        { }

        [Test]
        public void ExecuteWithSingleProject()
        { }
    }
}
