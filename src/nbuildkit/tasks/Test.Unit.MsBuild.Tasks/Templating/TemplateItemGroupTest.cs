//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
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
    public sealed class TemplateItemGroupTest : TaskTest
    {
        [Test]
        public void ExecuteWithEmptyItemGroupWithNoTemplates()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var task = new TemplateItemGroup();
            task.BuildEngine = BuildEngine.Object;
            task.Items = Array.Empty<ITaskItem>();
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedItems = task.UpdatedItems;
            Assert.AreEqual(0, updatedItems.Length);
        }

        [Test]
        public void ExecuteWithItemGroupWithNoTemplates()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var item = new TaskItem("d");
            item.SetMetadata("e", "f");

            var items = new ITaskItem[]
            {
                item,
            };

            var task = new TemplateItemGroup();
            task.BuildEngine = BuildEngine.Object;
            task.Items = items;
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedItems = task.UpdatedItems;
            Assert.AreEqual(1, updatedItems.Length);

            Assert.AreEqual("d", updatedItems[0].ItemSpec);

            var customMetadata = updatedItems[0].CloneCustomMetadata();
            Assert.AreEqual(1, customMetadata.Count);
            Assert.IsTrue(customMetadata.Contains("e"));
            Assert.AreEqual("f", customMetadata["e"]);
        }

        [Test]
        public void ExecuteWithItemGroupWithTemplates()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var item = new TaskItem("d");
            item.SetMetadata("e", "${a}");

            var items = new ITaskItem[]
            {
                item,
            };

            var task = new TemplateItemGroup();
            task.BuildEngine = BuildEngine.Object;
            task.Items = items;
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedItems = task.UpdatedItems;
            Assert.AreEqual(1, updatedItems.Length);

            Assert.AreEqual("d", updatedItems[0].ItemSpec);

            var customMetadata = updatedItems[0].CloneCustomMetadata();
            Assert.AreEqual(1, customMetadata.Count);
            Assert.IsTrue(customMetadata.Contains("e"));
            Assert.AreEqual("b", customMetadata["e"]);
        }

        [Test]
        public void ExecuteWithSearchExpression()
        {
            InitializeBuildEngine();

            var token = new TaskItem("a");
            token.SetMetadata("ReplacementValue", "b");

            var tokens = new ITaskItem[] { token };

            var item = new TaskItem("d");
            item.SetMetadata("e", "$a$");

            var items = new ITaskItem[]
            {
                item,
            };

            var task = new TemplateItemGroup();
            task.BuildEngine = BuildEngine.Object;
            task.Items = items;
            task.SearchExpression = "(?<token>\\$(?<identifier>\\w*)\\$)";
            task.Tokens = tokens;

            var result = task.Execute();
            Assert.IsTrue(result, "Expected the task to finish successfully");

            var updatedItems = task.UpdatedItems;
            Assert.AreEqual(1, updatedItems.Length);

            Assert.AreEqual("d", updatedItems[0].ItemSpec);

            var customMetadata = updatedItems[0].CloneCustomMetadata();
            Assert.AreEqual(1, customMetadata.Count);
            Assert.IsTrue(customMetadata.Contains("e"));
            Assert.AreEqual("b", customMetadata["e"]);
        }
    }
}
