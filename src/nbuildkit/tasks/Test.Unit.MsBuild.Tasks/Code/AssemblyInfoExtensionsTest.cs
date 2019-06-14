// <copyright file="AssemblyInfoExtensionsTest.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Moq;
using Nuclei;
using Nuclei.Diagnostics.Logging;
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

            AssemblyInfoExtensions.UpdateAssemblyAttribute(
                filePath,
                attributeName,
                value,
                Encoding.Unicode,
                new Mock<ILogger>().Object,
                false);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "[assembly: {0}({1})]" + Environment.NewLine,
                    attributeName,
                    value),
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateAssemblyAttributeForCSharpWithNewAttribute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "CSharpWithNewAttribute.cs");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var attributeName = "TestAttribute";
            var value = "\"TestValue\"";

            AssemblyInfoExtensions.UpdateAssemblyAttribute(
                filePath,
                attributeName,
                value,
                Encoding.Unicode,
                new Mock<ILogger>().Object,
                true);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "[assembly: {0}({1})]" + Environment.NewLine,
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

            AssemblyInfoExtensions.UpdateAssemblyAttribute(
                filePath,
                attributeName,
                value,
                Encoding.Unicode,
                new Mock<ILogger>().Object,
                false);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "<Assembly: {0}({1})>" + Environment.NewLine,
                    attributeName,
                    value),
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateAssemblyAttributeForVBWithNewAttribute()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "VbWithNewAttribute.vb");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var attributeName = "TestAttribute";
            var value = "\"TestValue\"";

            AssemblyInfoExtensions.UpdateAssemblyAttribute(
                filePath,
                attributeName,
                value,
                Encoding.Unicode,
                new Mock<ILogger>().Object,
                true);
            Assert.AreEqual(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "<Assembly: {0}({1})>" + Environment.NewLine,
                    attributeName,
                    value),
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateInternalsVisibleToAttributesForCSharpWithExistingAttributes()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "CSharpWithExistingInternalsVisibleToAttributes.cs");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var attributeName = "System.Runtime.CompilerServices.InternalsVisibleTo";
            File.WriteAllLines(
                filePath,
                new[]
                {
                    "#if NOTACOMPILERDIRECTIVE",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[assembly: {0}(\"{1}\")]",
                        attributeName,
                        "not-the-correct-value"),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[assembly: {0}(\"{1}\")]",
                        attributeName,
                        "another-not-the-correct-value"),
                    "#endif",
                });

            var compilerDirective = "COMPILERDIRECTIVE";
            AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                filePath,
                compilerDirective,
                new List<Tuple<string, string>>
                {
                    Tuple.Create("a", "b"),
                    Tuple.Create("c", (string)null),
                },
                Encoding.Unicode,
                new Mock<ILogger>().Object);
            Assert.AreEqual(
                @"#if COMPILERDIRECTIVE
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""a, PublicKey=b"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""c"")]
#endif
",
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateInternalsVisibleToAttributesForCSharpWithExistingAttributesSpacedToWide()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "CSharpWithExistingInternalsVisibleToAttributesSpacedTooMuch.cs");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var attributeName = "System.Runtime.CompilerServices.InternalsVisibleTo";
            var content = new[]
                {
                    "#if NOTACOMPILERDIRECTIVE",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[assembly: {0}(\"{1}\")]",
                        attributeName,
                        "not-the-correct-value"),
                    string.Empty,
                    string.Empty,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[assembly: {0}(\"{1}\")]",
                        attributeName,
                        "another-not-the-correct-value"),
                    "#endif",
                };
            File.WriteAllLines(
                filePath,
                content);

            var compilerDirective = "COMPILERDIRECTIVE";
            AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                filePath,
                compilerDirective,
                new List<Tuple<string, string>>
                {
                    Tuple.Create("a", "b"),
                    Tuple.Create("c", (string)null),
                },
                Encoding.Unicode,
                new Mock<ILogger>().Object);
            Assert.AreEqual(
                string.Join(Environment.NewLine, content) + Environment.NewLine,
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateInternalsVisibleToAttributesForCSharpWithNewAttributes()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "CSharpWithNewInternalsVisibleToAttributes.cs");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllText(filePath, string.Empty);

            var compilerDirective = "COMPILERDIRECTIVE";
            AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                filePath,
                compilerDirective,
                new List<Tuple<string, string>>
                {
                    Tuple.Create("a", "b"),
                    Tuple.Create("c", (string)null),
                },
                Encoding.Unicode,
                new Mock<ILogger>().Object);
            Assert.AreEqual(
                @"#if COMPILERDIRECTIVE
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""a, PublicKey=b"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""c"")]
#endif
",
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateInternalsVisibleToAttributesForVbWithExistingAttributes()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "VbWithExistingInternalsVisibleToAttributes.vb");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var attributeName = "System.Runtime.CompilerServices.InternalsVisibleTo";
            File.WriteAllLines(
                filePath,
                new[]
                {
                    "#If NOTACOMPILERDIRECTIVE",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "<Assembly: {0}(\"{1}\")>",
                        attributeName,
                        "not-the-correct-value"),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "<Assembly: {0}(\"{1}\")>",
                        attributeName,
                        "another-not-the-correct-value"),
                    "#End If",
                });

            var compilerDirective = "COMPILERDIRECTIVE";
            AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                filePath,
                compilerDirective,
                new List<Tuple<string, string>>
                {
                    Tuple.Create("a", "b"),
                    Tuple.Create("c", (string)null),
                },
                Encoding.Unicode,
                new Mock<ILogger>().Object);
            Assert.AreEqual(
                @"#If COMPILERDIRECTIVE
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""a, PublicKey=b"")>
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""c"")>
#End If
",
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateInternalsVisibleToAttributesForVbWithExistingAttributesToWide()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "VbWithExistingInternalsVisibleToAttributesSpacedTooMuch.vb");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var attributeName = "System.Runtime.CompilerServices.InternalsVisibleTo";
            var content = new[]
                {
                    "#If NOTACOMPILERDIRECTIVE",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "<Assembly: {0}(\"{1}\")>",
                        attributeName,
                        "not-the-correct-value"),
                    string.Empty,
                    string.Empty,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "<Assembly: {0}(\"{1}\")>",
                        attributeName,
                        "another-not-the-correct-value"),
                    "#End If",
                };
            File.WriteAllLines(
                filePath,
                content);

            var compilerDirective = "COMPILERDIRECTIVE";
            AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                filePath,
                compilerDirective,
                new List<Tuple<string, string>>
                {
                    Tuple.Create("a", "b"),
                    Tuple.Create("c", (string)null),
                },
                Encoding.Unicode,
                new Mock<ILogger>().Object);
            Assert.AreEqual(
                string.Join(Environment.NewLine, content) + Environment.NewLine,
                File.ReadAllText(filePath));
        }

        [Test]
        public void UpdateInternalsVisibleToAttributesForVbWithNewAttributes()
        {
            var directory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var filePath = Path.Combine(directory, "VbWithNewInternalsVisibleToAttributes.vb");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllText(filePath, string.Empty);

            var compilerDirective = "COMPILERDIRECTIVE";
            AssemblyInfoExtensions.UpdateInternalsVisibleToAttributes(
                filePath,
                compilerDirective,
                new List<Tuple<string, string>>
                {
                    Tuple.Create("a", "b"),
                    Tuple.Create("c", (string)null),
                },
                Encoding.Unicode,
                new Mock<ILogger>().Object);
            Assert.AreEqual(
                @"#If COMPILERDIRECTIVE
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""a, PublicKey=b"")>
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""c"")>
#End If
",
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
