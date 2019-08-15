//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
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
    public sealed class InvokeStepsTest : TaskTest
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
        public void ExecuteWithAfterDependencyOnMultipleSteps()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");
            using (var writer = new StreamWriter(scriptPath3, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteAfter", scriptPath3 + ";" + scriptPath2 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath3,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(3);
            CollectionAssert.AreEqual(new[] { scriptPath3, scriptPath2, scriptPath1 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithBeforeDependencyOnMultipleSteps()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");
            using (var writer = new StreamWriter(scriptPath3, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath3 + ";" + scriptPath2 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath3,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(3);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath3, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithBetweenDependencyOnMultipleSteps()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");
            using (var writer = new StreamWriter(scriptPath3, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                        { "ExecuteAfter", scriptPath3 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath3,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(3);
            CollectionAssert.AreEqual(new[] { scriptPath3, scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithCyclicDependencies()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");
            using (var writer = new StreamWriter(scriptPath3, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteAfter", scriptPath3 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath3,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath1 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(0);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 5);
        }

        [Test]
        public void ExecuteWithDependencyOnNonExistingStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteAfter", scriptPath3 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(0);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 4);
        }

        [Test]
        public void ExecuteWithDependencyThatCannotBePlaced()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");
            using (var writer = new StreamWriter(scriptPath3, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath3 },
                        { "ExecuteAfter", scriptPath2 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath3,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(0);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithFailingGlobalPostStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            GeneratePassingScript(scriptPath1);

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            GenerateFailingScript(scriptPath2);

            InitializeBuildEngine(new[] { true, false });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                    }),
            };
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "Path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "Path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);

            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithFailingGlobalPostStepAndContinue()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingGlobalPostStepAndContinue");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingGlobalPostStepAndContinue");
            }

            InitializeBuildEngine(new[] { true, false });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                    }),
            };
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "Path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "Path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = false;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithFailingGlobalPreStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingGlobalPreStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingGlobalPreStep");
            }

            InitializeBuildEngine(new[] { false, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                    }),
            };
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(1);
            CollectionAssert.AreEqual(new[] { scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 15);
        }

        [Test]
        public void ExecuteWithFailingGlobalPreStepAndContinue()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingGlobalPreStepAndContinue");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingGlobalPreStepAndContinue");
            }

            InitializeBuildEngine(new[] { false, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                    }),
            };
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = false;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);

            CollectionAssert.AreEqual(new[] { scriptPath2, scriptPath1 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithFailingLocalPostStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingLocalPostStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingLocalPostStep");
            }

            InitializeBuildEngine(new[] { true, false });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "Path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "Path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 16);
        }

        [Test]
        public void ExecuteWithFailingLocalPostStepAndContinue()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingLocalPostStepAndContinue");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingLocalPostStepAndContinue");
            }

            InitializeBuildEngine(new[] { true, false });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "Path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "Path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = false;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 16);
        }

        [Test]
        public void ExecuteWithFailingLocalPreStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPreStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPreStep");
            }

            InitializeBuildEngine(new[] { false, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", scriptPath2 },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(1);
            CollectionAssert.AreEqual(new[] { scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 12);
        }

        [Test]
        public void ExecuteWithFailingLocalPreStepAndContinue()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingLocalPreStepAndContinue");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingLocalPreStepAndContinue");
            }

            InitializeBuildEngine(new[] { false, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", scriptPath2 },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = false;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath2, scriptPath1 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 16);
        }

        [Test]
        public void ExecuteWithFailingStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath = Path.Combine(
                workingDir,
                "script.msbuild");
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingStep");
            }

            InitializeBuildEngine(new[] { false });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath,
                    new Hashtable
                    {
                        { "Properties", string.Empty },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(1);
            CollectionAssert.AreEqual(new[] { scriptPath }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 5);
        }

        [Test]
        public void ExecuteWithFailingStepAndFailureSteps()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingStepAndFailureSteps");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithFailingStepAndFailureSteps");
            }

            InitializeBuildEngine(new[] { false, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.FailureSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", "e" },
                    }),
            };
            task.GroupsToExecute = new[]
            {
                new TaskItem("E"),
            };
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", "e" },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 11);
        }

        [Test]
        public void ExecuteWithGlobalPostStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPostStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPostStep");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                    }),
            };
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "Path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "Path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithGlobalPreStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithGlobalPreStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithGlobalPreStep");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new[]
            {
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                    }),
            };
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath2, scriptPath1 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithGroups()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithMultipleSteps");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithMultipleSteps");
            }

            InitializeBuildEngine(new[] { true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new[]
            {
                new TaskItem("B"),
            };
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", "a" },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", "b" },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(1);
            CollectionAssert.AreEqual(new[] { scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 8);
        }

        [Test]
        public void ExecuteWithLocalPostStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPostStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPostStep");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "Path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "Path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 16);
        }

        [Test]
        public void ExecuteWithLocalPreStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPreStep");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithLocalPreStep");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", scriptPath2 },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description1" },
                        { "Id", "id1" },
                        { "Name", "name1" },
                        { "Path", "path1" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description2" },
                        { "Id", "id2" },
                        { "Name", "name2" },
                        { "Path", "path2" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath2, scriptPath1 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 16);
        }

        [Test]
        public void ExecuteWithMultipleDependencies()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath3 = Path.Combine(
                workingDir,
                "script3.msbuild");
            using (var writer = new StreamWriter(scriptPath3, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteAfter", scriptPath3 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath3,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath3),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(3);
            CollectionAssert.AreEqual(new[] { scriptPath3, scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 19);
        }

        [Test]
        public void ExecuteWithMultipleProperties()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath = Path.Combine(
                workingDir,
                "script.msbuild");
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithMultipleProperties");
            }

            InitializeBuildEngine(new[] { true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath,
                    new Hashtable
                    {
                        { "Properties", "a=b;c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(1);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 8);
        }

        [Test]
        public void ExecuteWithMultiplePropertiesWithTrailingSemicolon()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath = Path.Combine(
                workingDir,
                "script.msbuild");
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithMultiplePropertiesWithTrailingSemicolon");
            }

            InitializeBuildEngine(new[] { true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath,
                    new Hashtable
                    {
                        { "Properties", "a=b;c=d;" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(1);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 8);
        }

        [Test]
        public void ExecuteWithMultipleSteps()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithMultipleSteps");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithMultipleSteps");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);
        }

        [Test]
        public void ExecuteWithSingleDependencyInsertedAfter()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteAfter", scriptPath2 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath2, scriptPath1 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);
        }

        [Test]
        public void ExecuteWithSingleDependencyInsertedBefore()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath1 = Path.Combine(
                workingDir,
                "script1.msbuild");
            using (var writer = new StreamWriter(scriptPath1, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            var scriptPath2 = Path.Combine(
                workingDir,
                "script2.msbuild");
            using (var writer = new StreamWriter(scriptPath2, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleDependencyInsertedAfter");
            }

            InitializeBuildEngine(new[] { true, true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath1,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                        { "ExecuteBefore", scriptPath2 },
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath1),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
                new TaskItem(
                    Path.GetFileName(scriptPath2),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(2);
            CollectionAssert.AreEqual(new[] { scriptPath1, scriptPath2 }, ExecutedScripts());
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);
        }

        [Test]
        public void ExecuteWithSingleProperty()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath = Path.Combine(
                workingDir,
                "script.msbuild");
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleProperty");
            }

            InitializeBuildEngine(new[] { true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath,
                    new Hashtable
                    {
                        { "Properties", "a=b" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(1);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 7);
        }

        [Test]
        public void ExecuteWithSinglePropertyWithTrailingSemicolon()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath = Path.Combine(
                workingDir,
                "script.msbuild");
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSinglePropertyWithTrailingSemicolon");
            }

            InitializeBuildEngine(new[] { true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath,
                    new Hashtable
                    {
                        { "Properties", "a=b;" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(1);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 7);
        }

        [Test]
        public void ExecuteWithSingleStep()
        {
            var baseDir = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var workingDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            var scriptPath = Path.Combine(
                workingDir,
                "script.msbuild");
            GeneratePassingScript(scriptPath);

            InitializeBuildEngine(new[] { true });

            var task = new InvokeSteps();
            task.BuildEngine = BuildEngine.Object;
            task.FailOnPostStepFailure = true;
            task.FailOnPreStepFailure = true;
            task.GroupsToExecute = new ITaskItem[0];
            task.PostSteps = new ITaskItem[0];
            task.PreSteps = new ITaskItem[0];
            task.Projects = new[]
            {
                new TaskItem(
                    scriptPath,
                    new Hashtable
                    {
                        { "Properties", string.Empty },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty },
                    }),
            };
            task.Properties = new TaskItem[0];
            task.StepMetadata = new[]
            {
                new TaskItem(
                    Path.GetFileName(scriptPath),
                    new Hashtable
                    {
                        { "Description", "description" },
                        { "Id", "id" },
                        { "Name", "name" },
                        { "Path", "Path" },
                    }),
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfInvocations(1);
            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 5);
        }
    }
}
