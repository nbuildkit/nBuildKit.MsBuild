//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
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
        private static void GenerateFailingScript(string path)
        {
            var content =
@"<?xml version='1.0' encoding='utf-8'?>
<Project
    DefaultTargets='InvokeStandaloneMsBuild'
    ToolsVersion='4.0'
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
    <Target 
        Name='InvokeStandaloneMsBuild'
        Returns='FailingScript'>
        <Error Text='Fail' />
    </Target>
</Project>";

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static void GeneratePassingScript(string path)
        {
            var content =
@"<?xml version='1.0' encoding='utf-8'?>
<Project
    DefaultTargets='InvokeStandaloneMsBuild'
    ToolsVersion='4.0'
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
    <Target 
        Name='InvokeStandaloneMsBuild'
        Returns='PassingScript'>
        <Message Text='Pass' />
    </Target>
</Project>";

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, content, Encoding.UTF8);
        }

        [Test]
        public void ExecuteWithFailingProject()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var scriptPath = Path.Combine(
                workingDir,
                "failing.msbuild");
            GenerateFailingScript(scriptPath);

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(scriptPath) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.IsNull(task.TargetOutputs);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1);
        }

        [Test]
        public void ExecuteWithMissingProjects()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var scriptPath = Path.Combine(
                workingDir,
                "failing.msbuild");

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(scriptPath) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.IsNull(task.TargetOutputs);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1);
        }

        [Test]
        public void ExecuteWithMissingProjectsAndSkipNonExistingProjects()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var nonExistingPath = Path.Combine(
                workingDir,
                "nonexisting.msbuild");
            var existingPath = Path.Combine(
                workingDir,
                "passing.msbuild");
            GeneratePassingScript(existingPath);

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(nonExistingPath), new TaskItem(existingPath) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = true;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsNotNull(task.TargetOutputs);
            Assert.AreEqual(1, task.TargetOutputs.Length);
            Assert.AreEqual("PassingScript", task.TargetOutputs[0].ItemSpec);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0);
        }

        [Test]
        public void ExecuteWithMultipleProjects()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var scriptPath1 = Path.Combine(
                workingDir,
                "passing1.msbuild");
            GeneratePassingScript(scriptPath1);

            var scriptPath2 = Path.Combine(
                workingDir,
                "passing2.msbuild");
            GeneratePassingScript(scriptPath2);

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(scriptPath1), new TaskItem(scriptPath2) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsNotNull(task.TargetOutputs);
            Assert.AreEqual(2, task.TargetOutputs.Length);
            Assert.AreEqual("PassingScript", task.TargetOutputs[0].ItemSpec);
            Assert.AreEqual("PassingScript", task.TargetOutputs[1].ItemSpec);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0);
        }

        [Test]
        public void ExecuteWithMultipleProjectsAndFailingProjects()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var scriptPath1 = Path.Combine(
                workingDir,
                "failing.msbuild");
            GenerateFailingScript(scriptPath1);

            var scriptPath2 = Path.Combine(
                workingDir,
                "passing.msbuild");
            GeneratePassingScript(scriptPath2);

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(scriptPath1), new TaskItem(scriptPath2) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
            task.StopOnFirstFailure = false;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.IsNull(task.TargetOutputs);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1);
        }

        [Test]
        public void ExecuteWithMultipleProjectsAndFailingProjectsAndStopOnFirstFailure()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var scriptPath1 = Path.Combine(
                workingDir,
                "failing.msbuild");
            GenerateFailingScript(scriptPath1);

            var scriptPath2 = Path.Combine(
                workingDir,
                "passing.msbuild");
            GeneratePassingScript(scriptPath2);

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(scriptPath1), new TaskItem(scriptPath2) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsFalse(result);

            Assert.IsNull(task.TargetOutputs);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1);
        }

        [Test]
        public void ExecuteWithSingleMissingProjectAndSkipNonExistingProjects()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var nonexistingPath = Path.Combine(
                workingDir,
                "nonexisting.msbuild");

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(nonexistingPath) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = true;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsNotNull(task.TargetOutputs);
            Assert.AreEqual(0, task.TargetOutputs.Length);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0);
        }

        [Test]
        public void ExecuteWithSingleProject()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            var tempDir = Path.Combine(workingDir, "temp");

            var scriptPath = Path.Combine(
                workingDir,
                "passing.msbuild");
            GeneratePassingScript(scriptPath);

            InitializeBuildEngine();

            var task = new InvokeStandaloneMsBuild();
            task.BuildEngine = BuildEngine.Object;
            task.Projects = new[] { new TaskItem(scriptPath) };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
            task.StopOnFirstFailure = true;
            task.Targets = string.Empty;
            task.TemporaryDirectory = new TaskItem(tempDir);
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.IsNotNull(task.TargetOutputs);
            Assert.AreEqual(1, task.TargetOutputs.Length);
            Assert.AreEqual("PassingScript", task.TargetOutputs[0].ItemSpec);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0);
        }
    }
}
