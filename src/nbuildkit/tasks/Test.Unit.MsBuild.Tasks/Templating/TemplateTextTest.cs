//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Tests;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class TemplateTextTest : TaskTest
    {
        [Test]
        public void ExecuteWithTextWithNoTemplates()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var task = new TemplateText();
            task.BuildEngine = BuildEngine.Object;
            task.Template = "c";
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedTemplate = task.Result;
            Assert.AreEqual("c", updatedTemplate);
        }

        [Test]
        public void ExecuteWithTextWithTemplates()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var task = new TemplateText();
            task.BuildEngine = BuildEngine.Object;
            task.Template = "${a}";
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedTemplate = task.Result;
            Assert.AreEqual("b", updatedTemplate);
        }

        [Test]
        public void ExecuteWithSearchExpression()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var task = new TemplateText();
            task.BuildEngine = BuildEngine.Object;
            task.SearchExpression = "(?<token>\\$(?<identifier>\\w*)\\$)";
            task.Template = "$a$";
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedTemplate = task.Result;
            Assert.AreEqual("b", updatedTemplate);
        }
    }
}
