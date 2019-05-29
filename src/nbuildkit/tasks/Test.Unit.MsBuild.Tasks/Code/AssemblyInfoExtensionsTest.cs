// <copyright file="AssemblyInfoExtensionsTest.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Nuclei;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Code
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class AssemblyInfoExtensionsTest
    {
        [Test]
        public void UpdateAssemblyAttributeForCSharpWithExistingAttribute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "CSharpWithExistingAttribute.cs");

            var attributeName = "TestAttribute";
            var value = "\"TestValue\"";
            File.WriteAllText(
                filePath,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "[assembly: {0}(\"{1}\")]",
                    attributeName,
                    "not-the-correct-value"));

            AssemblyInfoExtensions.UpdateAssemblyAttribute(filePath, attributeName, value, Encoding.Unicode, (i, m) => { }, false);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "[assembly: {0}({1})]",
                    attributeName,
                    value),
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateAssemblyAttributeForCSharpWithNewAttribute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "CSharpWithNewAttribute.cs");

            var attributeName = "TestAttribute";
            var value = "\"TestValue\"";

            AssemblyInfoExtensions.UpdateAssemblyAttribute(filePath, attributeName, value, Encoding.Unicode, (i, m) => { }, false);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "[assembly: {0}({1})]",
                    attributeName,
                    value),
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateAssemblyAttributeForVBWithExistingAttribute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "VbWithExistingAttribute.vb");

            var attributeName = "TestAttribute";
            var value = "\"TestValue\"";
            File.WriteAllText(
                filePath,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "<Assembly: {0}(\"{1}\")>",
                    attributeName,
                    "not-the-correct-value"));

            AssemblyInfoExtensions.UpdateAssemblyAttribute(filePath, attributeName, value, Encoding.Unicode, (i, m) => { }, false);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "<Assembly: {0}({1})>",
                    attributeName,
                    value),
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateAssemblyAttributeForVBWithNewAttribute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "VbWithNewAttribute.vb");

            var attributeName = "TestAttribute";
            var value = "\"TestValue\"";

            AssemblyInfoExtensions.UpdateAssemblyAttribute(filePath, attributeName, value, Encoding.Unicode, (i, m) => { }, false);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "<Assembly: {0}({1})>",
                    attributeName,
                    value),
                File.ReadAllText(filePath));
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
