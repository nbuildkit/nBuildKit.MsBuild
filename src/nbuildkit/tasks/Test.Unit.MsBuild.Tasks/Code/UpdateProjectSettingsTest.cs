// <copyright file="UpdateProjectSettingsTest.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Code
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class UpdateProjectSettingsTest : TaskTest
    {
        private static ITaskItem[] GenerateTokens()
        {
            return new ITaskItem[]
            {
                new TaskItem(
                    "VersionSemantic",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "VersionSemantic"
                        },
                    }),
                new TaskItem(
                    "VersionAssembly",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "VersionAssembly"
                        },
                    }),
                new TaskItem(
                    "VersionAssemblyFile",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "VersionAssemblyFile"
                        },
                    }),
                new TaskItem(
                    "CompanyName",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "CompanyName"
                        },
                    }),
                new TaskItem(
                    "CopyrightLong",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "CopyrightLong"
                        },
                    }),
                new TaskItem(
                    "VersionProduct",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "VersionProduct"
                        },
                    }),
                new TaskItem(
                    "Configuration",
                    new Hashtable
                    {
                        {
                            "ReplacementValue",
                            "Configuration"
                        },
                    }),
            };
        }

        [Test]
        public void ExecuteWithEmptyProjectPath()
        {
            InitializeBuildEngine();

            var task = new UpdateProjectSettings();
            task.BuildEngine = BuildEngine.Object;
            task.Project = new TaskItem(string.Empty);

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithNewCSharpProjectWithAssemblyInfo()
        {
            Assert.Fail();
        }

        [Test]
        public void ExecuteWithNewProject()
        {
            Assert.Fail();
        }

        [Test]
        public void ExecuteWithNewVBProjectWithAssemblyInfo()
        {
            Assert.Fail();
        }

        [Test]
        public void ExecuteWithNonExistingProject()
        {
            InitializeBuildEngine();

            var task = new UpdateProjectSettings();
            task.BuildEngine = BuildEngine.Object;
            task.Project = new TaskItem("c:\\this\\path\\does\\not\\exist.csproj");

            var result = task.Execute();
            Assert.IsFalse(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);
        }

        [Test]
        public void ExecuteWithOldCSharpProjects()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "OldSolution", "CSharpLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "CSharpLibrary.csproj");

            InitializeBuildEngine();

            var task = new UpdateProjectSettings();
            task.BuildEngine = BuildEngine.Object;
            task.GenerateBuildInformation = false;
            task.Project = new TaskItem(testProjectPath);
            task.Tokens = GenerateTokens();

            var result = task.Execute();
            Assert.IsTrue(result);

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 0);

            var expectedContent = @"using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle(""CSharpLibrary"")]
[assembly: AssemblyDescription("""")]
[assembly: AssemblyConfiguration(""Configuration"")]
[assembly: AssemblyCompany(""CompanyName"")]
[assembly: AssemblyProduct(""CSharpLibrary"")]
[assembly: AssemblyCopyright(""CopyrightLong"")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid(""bfa4fae5-a773-4a35-9928-9f50f9acf8c6"")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion(""1.0.*"")]
[assembly: AssemblyVersion(""VersionAssembly"")]
[assembly: AssemblyFileVersion(""VersionAssemblyFile"")]
[assembly: AssemblyInformationalVersion(""VersionProduct"")]";

            Assert.AreEqual(expectedContent, File.ReadAllText(Path.Combine(testProjectDirectory, "Properties", "AssemblyInfo.cs")));
        }

        [Test]
        public void ExecuteWithOldVBProjects()
        {
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
