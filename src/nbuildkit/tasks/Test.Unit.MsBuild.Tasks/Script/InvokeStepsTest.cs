//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using NBuildKit.MsBuild.Tasks.Core;
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
        [Test]
        public void ExecuteWithFailingGlobalPreStep()
        {
            Assert.Ignore();
        }

        [Test]
        public void ExecuteWithFailingGlobalPostStep()
        {
            Assert.Ignore();
        }

        [Test]
        public void ExecuteWithFailingLocalPreStep()
        {
            Assert.Ignore();
        }

        [Test]
        public void ExecuteWithFailingLocalPostStep()
        {
            Assert.Ignore();
        }

        [Test]
        public void ExecuteWithFailingStep()
        {
            Assert.Ignore();
        }

        [Test]
        public void ExecuteWithFailingStepAndFailureSteps()
        {
            Assert.Ignore();
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<List<string>>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.Add(new List<string>(args));
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                    })
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(2, invokedArgs.Count);
            Assert.AreEqual(5, invokedArgs[0].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0][0]);
            Assert.AreEqual("/nologo", invokedArgs[0][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[0][2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[0][3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath1),
                invokedArgs[0][4]);

            Assert.AreEqual(11, invokedArgs[1].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[1][0]);
            Assert.AreEqual("/nologo", invokedArgs[1][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[1][2]);
            Assert.AreEqual("/P:IsFirstStep=\"true\"", invokedArgs[1][3]);
            Assert.AreEqual("/P:IsLastStep=\"true\"", invokedArgs[1][4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/P:StepPath=\"{0}\"",
                    scriptPath1),
                invokedArgs[1][5]);
            Assert.AreEqual("/P:StepDescription=\"description1\"", invokedArgs[1][6]);
            Assert.AreEqual("/P:StepName=\"name1\"", invokedArgs[1][7]);
            Assert.AreEqual("/P:c=\"d\"", invokedArgs[1][8]);
            Assert.AreEqual("/P:StepId=\"id1\"", invokedArgs[1][9]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath2),
                invokedArgs[1][10]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Exactly(2));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 31);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<List<string>>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.Add(new List<string>(args));
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "Properties", "c=d" }
                    })
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(2, invokedArgs.Count);
            Assert.AreEqual(11, invokedArgs[0].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0][0]);
            Assert.AreEqual("/nologo", invokedArgs[0][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[0][2]);
            Assert.AreEqual("/P:IsFirstStep=\"true\"", invokedArgs[0][3]);
            Assert.AreEqual("/P:IsLastStep=\"true\"", invokedArgs[0][4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/P:StepPath=\"{0}\"",
                    scriptPath1),
                invokedArgs[0][5]);
            Assert.AreEqual("/P:StepDescription=\"description1\"", invokedArgs[0][6]);
            Assert.AreEqual("/P:StepName=\"name1\"", invokedArgs[0][7]);
            Assert.AreEqual("/P:c=\"d\"", invokedArgs[0][8]);
            Assert.AreEqual("/P:StepId=\"id1\"", invokedArgs[0][9]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath2),
                invokedArgs[0][10]);

            Assert.AreEqual(5, invokedArgs[1].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[1][0]);
            Assert.AreEqual("/nologo", invokedArgs[1][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[1][2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[1][3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath1),
                invokedArgs[1][4]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Exactly(2));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 31);
        }

        [Test]
        public void ExecuteWithGroups()
        {
            Assert.Ignore();
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<List<string>>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.Add(new List<string>(args));
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", scriptPath2 }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(2, invokedArgs.Count);
            Assert.AreEqual(5, invokedArgs[0].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0][0]);
            Assert.AreEqual("/nologo", invokedArgs[0][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[0][2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[0][3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath1),
                invokedArgs[0][4]);

            Assert.AreEqual(10, invokedArgs[1].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[1][0]);
            Assert.AreEqual("/nologo", invokedArgs[1][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[1][2]);
            Assert.AreEqual("/P:IsFirstStep=\"true\"", invokedArgs[1][3]);
            Assert.AreEqual("/P:IsLastStep=\"true\"", invokedArgs[1][4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/P:StepPath=\"{0}\"",
                    scriptPath1),
                invokedArgs[1][5]);
            Assert.AreEqual("/P:StepDescription=\"description1\"", invokedArgs[1][6]);
            Assert.AreEqual("/P:StepName=\"name1\"", invokedArgs[1][7]);
            Assert.AreEqual("/P:StepId=\"id1\"", invokedArgs[1][8]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath2),
                invokedArgs[1][9]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Exactly(2));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 28);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<List<string>>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.Add(new List<string>(args));
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(2, invokedArgs.Count);
            Assert.AreEqual(10, invokedArgs[0].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0][0]);
            Assert.AreEqual("/nologo", invokedArgs[0][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[0][2]);
            Assert.AreEqual("/P:IsFirstStep=\"true\"", invokedArgs[0][3]);
            Assert.AreEqual("/P:IsLastStep=\"true\"", invokedArgs[0][4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "/P:StepPath=\"{0}\"",
                    scriptPath1),
                invokedArgs[0][5]);
            Assert.AreEqual("/P:StepDescription=\"description1\"", invokedArgs[0][6]);
            Assert.AreEqual("/P:StepName=\"name1\"", invokedArgs[0][7]);
            Assert.AreEqual("/P:StepId=\"id1\"", invokedArgs[0][8]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath2),
                invokedArgs[0][9]);

            Assert.AreEqual(5, invokedArgs[1].Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[1][0]);
            Assert.AreEqual("/nologo", invokedArgs[1][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[1][2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[1][3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath1),
                invokedArgs[1][4]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Exactly(2));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 28);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0]);
            Assert.AreEqual("/nologo", invokedArgs[1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[2]);
            Assert.AreEqual("/P:c=\"d\"", invokedArgs[3]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath),
                invokedArgs[5]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Once());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(6, invokedArgs.Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0]);
            Assert.AreEqual("/nologo", invokedArgs[1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[2]);
            Assert.AreEqual("/P:c=\"d\"", invokedArgs[3]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[4]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath),
                invokedArgs[5]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Once());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<List<string>>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.Add(new List<string>(args));
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    }),
                new TaskItem(
                    scriptPath2,
                    new Hashtable
                    {
                        { "Properties", "c=d" },
                        { "Groups", string.Empty },
                        { "PreSteps", string.Empty },
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(2, invokedArgs.Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0][0]);
            Assert.AreEqual("/nologo", invokedArgs[0][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[0][2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[0][3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath1),
                invokedArgs[0][4]);

            Assert.AreEqual("/nodeReuse:false", invokedArgs[1][0]);
            Assert.AreEqual("/nologo", invokedArgs[1][1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[1][2]);
            Assert.AreEqual("/P:c=\"d\"", invokedArgs[1][3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath2),
                invokedArgs[1][4]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Exactly(2));

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 24);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(5, invokedArgs.Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0]);
            Assert.AreEqual("/nologo", invokedArgs[1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath),
                invokedArgs[4]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Once());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 12);
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

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(5, invokedArgs.Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0]);
            Assert.AreEqual("/nologo", invokedArgs[1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[2]);
            Assert.AreEqual("/P:a=\"b\"", invokedArgs[3]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath),
                invokedArgs[4]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Once());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 12);
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
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                writer.WriteLine("ExecuteWithSingleStep");
            }

            InitializeBuildEngine();

            var invokedPath = string.Empty;
            var invokedArgs = new List<string>();
            var invokedWorkingDirectory = string.Empty;
            Action<StringDictionary> environmentVariableBuilder = null;
            var invoker = new Mock<IApplicationInvoker>();
            {
                invoker.Setup(
                    i => i.Invoke(
                        It.IsAny<string>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<StringDictionary>>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<DataReceivedEventHandler>(),
                        It.IsAny<bool>()))
                    .Callback<string, IEnumerable<string>, string, Action<StringDictionary>, DataReceivedEventHandler, DataReceivedEventHandler, bool>(
                        (path, args, dir, e, o, err, f) =>
                        {
                            invokedPath = path;
                            invokedArgs.AddRange(args);
                            invokedWorkingDirectory = dir;
                            environmentVariableBuilder = e;
                        });
            }

            var task = new InvokeSteps(invoker.Object);
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
                        { "PostSteps", string.Empty }
                    })
            };
            task.Properties = new TaskItem[0];
            task.RunEachTargetSeparately = false;
            task.SkipNonexistentProjects = false;
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
                    })
            };
            task.StopOnFirstFailure = true;
            task.StopOnPostStepFailure = true;
            task.StopOnPreStepFailure = true;
            task.Targets = string.Empty;
            task.WorkingDirectory = new TaskItem(workingDir);

            var result = task.Execute();
            Assert.IsTrue(result);

            Assert.AreEqual(4, invokedArgs.Count);
            Assert.AreEqual("/nodeReuse:false", invokedArgs[0]);
            Assert.AreEqual("/nologo", invokedArgs[1]);
            Assert.AreEqual("/verbosity:normal", invokedArgs[2]);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\"",
                    scriptPath),
                invokedArgs[3]);

            invoker.Verify(
                i => i.Invoke(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<StringDictionary>>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<DataReceivedEventHandler>(),
                    It.IsAny<bool>()),
                Times.Once());

            VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 10);
        }
    }
}
