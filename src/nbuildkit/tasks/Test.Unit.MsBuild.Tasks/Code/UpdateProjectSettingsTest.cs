// <copyright file="UpdateProjectSettingsTest.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithNewCSharpProjectWithAssemblyInfo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "NewSolutionWithAssemblyInfo", "CSharpLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "CSharpLibrary.csproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "AssemblyInfo.cs");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);

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
[assembly: AssemblyInformationalVersion(""VersionProduct"")]
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithNewCSharpProjectWithInternalsVisibleTo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "NewSolutionWithAssemblyInfo", "CSharpLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "CSharpLibrary.csproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "AssemblyInfo.cs");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.InternalsVisibleTo = new ITaskItem[]
                {
                new TaskItem(
                    "SomeProject",
                    new Hashtable
                    {
                        { "Projects", "CSharpLibrary" },
                    }),
                };
                task.InternalsVisibleToCompilerDirective = "COMPILERDIRECTIVE";
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 18);

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
[assembly: AssemblyInformationalVersion(""VersionProduct"")]
#if COMPILERDIRECTIVE
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""SomeProject"")]
#endif
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithNewCSharpProjectWithoutAssemblyInfo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "NewSolution", "CSharpLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "CSharpLibrary.csproj");

            using (var undo = new UndoFileChanges(testProjectPath))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);

                var expectedProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <PropertyGroup>
    <Version>VersionSemantic</Version>
    <AssemblyVersion>VersionAssembly</AssemblyVersion>
    <FileVersion>VersionAssemblyFile</FileVersion>
    <InformationalVersion>VersionProduct</InformationalVersion>
    <Company>CompanyName</Company>
    <Copyright>CopyrightLong</Copyright>
  </PropertyGroup>
</Project>";
                Assert.AreEqual(expectedProjectContent, File.ReadAllText(testProjectPath));
            }
        }

        [Test]
        public void ExecuteWithNewVBProjectWithAssemblyInfo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "NewSolutionWithAssemblyInfo", "VBNetLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "VBNetLibrary.vbproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "AssemblyInfo.vb");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);

                var expectedContent = @"Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes

<Assembly: AssemblyTitle(""VBNetLibrary"")>
<Assembly: AssemblyDescription("""")>
<Assembly: AssemblyCompany(""CompanyName"")>
<Assembly: AssemblyProduct(""VBNetLibrary"")>
<Assembly: AssemblyCopyright(""CopyrightLong"")>
<Assembly: AssemblyTrademark("""")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid(""f6c43178-bd7d-4d1f-85d2-5a37fcb3794d"")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion(""1.0.*"")>

<Assembly: AssemblyVersion(""VersionAssembly"")>
<Assembly: AssemblyFileVersion(""VersionAssemblyFile"")>
<Assembly: AssemblyInformationalVersion(""VersionProduct"")>
<Assembly: AssemblyConfiguration(""Configuration"")>
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithNewVBProjectWithInternalsVisibleTo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "NewSolutionWithAssemblyInfo", "VBNetLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "VBNetLibrary.vbproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "AssemblyInfo.vb");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.InternalsVisibleTo = new ITaskItem[]
                     {
                    new TaskItem(
                        "SomeProject",
                        new Hashtable
                        {
                            { "Projects", "VBNetLibrary" },
                        }),
                     };
                task.InternalsVisibleToCompilerDirective = "COMPILERDIRECTIVE";
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 18);

                var expectedContent = @"Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes

<Assembly: AssemblyTitle(""VBNetLibrary"")>
<Assembly: AssemblyDescription("""")>
<Assembly: AssemblyCompany(""CompanyName"")>
<Assembly: AssemblyProduct(""VBNetLibrary"")>
<Assembly: AssemblyCopyright(""CopyrightLong"")>
<Assembly: AssemblyTrademark("""")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid(""f6c43178-bd7d-4d1f-85d2-5a37fcb3794d"")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion(""1.0.*"")>

<Assembly: AssemblyVersion(""VersionAssembly"")>
<Assembly: AssemblyFileVersion(""VersionAssemblyFile"")>
<Assembly: AssemblyInformationalVersion(""VersionProduct"")>
<Assembly: AssemblyConfiguration(""Configuration"")>
#If COMPILERDIRECTIVE
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""SomeProject"")>
#End If
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithNewVBProjectWithoutAssemblyInfo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "NewSolution", "VBNetLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "VBNetLibrary.vbproj");

            using (var undo = new UndoFileChanges(testProjectPath))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 1);

                var expectedProjectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RootNamespace>VBNetLibrary</RootNamespace>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <PropertyGroup>
    <Version>VersionSemantic</Version>
    <AssemblyVersion>VersionAssembly</AssemblyVersion>
    <FileVersion>VersionAssemblyFile</FileVersion>
    <InformationalVersion>VersionProduct</InformationalVersion>
    <Company>CompanyName</Company>
    <Copyright>CopyrightLong</Copyright>
  </PropertyGroup>
</Project>";
                Assert.AreEqual(expectedProjectContent, File.ReadAllText(testProjectPath));
            }
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

            VerifyNumberOfLogMessages(numberOfErrorMessages: 1, numberOfWarningMessages: 0, numberOfNormalMessages: 1);
        }

        [Test]
        public void ExecuteWithOldCSharpProjects()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "OldSolution", "CSharpLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "CSharpLibrary.csproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "Properties", "AssemblyInfo.cs");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);

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
[assembly: AssemblyInformationalVersion(""VersionProduct"")]
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithOldCSharpProjectWithInternalsVisibleTo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "OldSolution", "CSharpLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "CSharpLibrary.csproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "Properties", "AssemblyInfo.cs");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.InternalsVisibleTo = new ITaskItem[]
                {
                    new TaskItem(
                        "SomeProject",
                        new Hashtable
                        {
                            { "Projects", "CSharpLibrary" },
                        }),
                };
                task.InternalsVisibleToCompilerDirective = "COMPILERDIRECTIVE";
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 18);

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
[assembly: AssemblyInformationalVersion(""VersionProduct"")]
#if COMPILERDIRECTIVE
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""SomeProject"")]
#endif
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithOldVBProjects()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "OldSolution", "VBNetLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "VBNetLibrary.vbproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "My Project", "AssemblyInfo.vb");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 13);

                var expectedContent = @"Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes

<Assembly: AssemblyTitle(""VBNetLibrary"")>
<Assembly: AssemblyDescription("""")>
<Assembly: AssemblyCompany(""CompanyName"")>
<Assembly: AssemblyProduct(""VBNetLibrary"")>
<Assembly: AssemblyCopyright(""CopyrightLong"")>
<Assembly: AssemblyTrademark("""")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid(""f6c43178-bd7d-4d1f-85d2-5a37fcb3794d"")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion(""1.0.*"")>

<Assembly: AssemblyVersion(""VersionAssembly"")>
<Assembly: AssemblyFileVersion(""VersionAssemblyFile"")>
<Assembly: AssemblyInformationalVersion(""VersionProduct"")>
<Assembly: AssemblyConfiguration(""Configuration"")>
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [Test]
        public void ExecuteWithOldVBProjectsWithInternalsVisibleTo()
        {
            var currentDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(currentDirectory));
            var testProjectDirectory = Path.Combine(projectPath, "TestFiles", "OldSolution", "VBNetLibrary");
            var testProjectPath = Path.Combine(testProjectDirectory, "VBNetLibrary.vbproj");

            var fileToAlter = Path.Combine(testProjectDirectory, "My Project", "AssemblyInfo.vb");
            using (var undo = new UndoFileChanges(fileToAlter))
            {
                InitializeBuildEngine();

                var task = new UpdateProjectSettings();
                task.BuildEngine = BuildEngine.Object;
                task.GenerateBuildInformation = false;
                task.InternalsVisibleTo = new ITaskItem[]
                    {
                        new TaskItem(
                            "SomeProject",
                            new Hashtable
                            {
                                { "Projects", "VBNetLibrary" },
                            }),
                    };
                task.InternalsVisibleToCompilerDirective = "COMPILERDIRECTIVE";
                task.Project = new TaskItem(testProjectPath);
                task.Tokens = GenerateTokens();

                var result = task.Execute();
                Assert.IsTrue(result);

                VerifyNumberOfLogMessages(numberOfErrorMessages: 0, numberOfWarningMessages: 0, numberOfNormalMessages: 18);

                var expectedContent = @"Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

' General Information about an assembly is controlled through the following
' set of attributes. Change these attribute values to modify the information
' associated with an assembly.

' Review the values of the assembly attributes

<Assembly: AssemblyTitle(""VBNetLibrary"")>
<Assembly: AssemblyDescription("""")>
<Assembly: AssemblyCompany(""CompanyName"")>
<Assembly: AssemblyProduct(""VBNetLibrary"")>
<Assembly: AssemblyCopyright(""CopyrightLong"")>
<Assembly: AssemblyTrademark("""")>

<Assembly: ComVisible(False)>

'The following GUID is for the ID of the typelib if this project is exposed to COM
<Assembly: Guid(""f6c43178-bd7d-4d1f-85d2-5a37fcb3794d"")>

' Version information for an assembly consists of the following four values:
'
'      Major Version
'      Minor Version
'      Build Number
'      Revision
'
' You can specify all the values or you can default the Build and Revision Numbers
' by using the '*' as shown below:
' <Assembly: AssemblyVersion(""1.0.*"")>

<Assembly: AssemblyVersion(""VersionAssembly"")>
<Assembly: AssemblyFileVersion(""VersionAssemblyFile"")>
<Assembly: AssemblyInformationalVersion(""VersionProduct"")>
<Assembly: AssemblyConfiguration(""Configuration"")>
#If COMPILERDIRECTIVE
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""SomeProject"")>
#End If
";

                Assert.AreEqual(expectedContent, File.ReadAllText(fileToAlter));
            }
        }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            // nUnit3 doesn't set the current directory anymore:
            // https://github.com/nunit/nunit/issues/1072
            // Le sigh ...
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        private sealed class UndoFileChanges : IDisposable
        {
            private readonly IDictionary<string, string> _pathMaps
                = new Dictionary<string, string>();

            public UndoFileChanges(params string[] filePaths)
                : this((IEnumerable<string>)filePaths)
            {
            }

            public UndoFileChanges(IEnumerable<string> filePaths)
            {
                foreach (var path in filePaths)
                {
                    if (File.Exists(path))
                    {
                        var newPath = Path.Combine(
                            Path.GetDirectoryName(path),
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}.copy",
                                Path.GetFileName(path)));
                        File.Copy(
                            path,
                            newPath);
                        _pathMaps.Add(path, newPath);
                    }
                    else
                    {
                        _pathMaps.Add(path, null);
                    }
                }
            }

            public void Dispose()
            {
                foreach (var pair in _pathMaps)
                {
                    if (pair.Value != null)
                    {
                        File.Copy(pair.Value, pair.Key, true);
                        File.Delete(pair.Value);
                    }
                    else
                    {
                        File.Delete(pair.Key);
                    }
                }
            }
        }
    }
}
