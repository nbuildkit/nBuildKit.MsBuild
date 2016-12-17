//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git query to find all new files.
    /// </summary>
    public sealed class GitNewFiles : GitCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var output = GetGitOutput(new[] { "status --porcelain --untracked-files" });

            var list = new List<ITaskItem>();
            foreach (var line in output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var fileStatus = line.Trim();
                if (fileStatus.StartsWith("??", StringComparison.OrdinalIgnoreCase))
                {
                    var newItem = new TaskItem(System.IO.Path.Combine(GetAbsolutePath(Workspace), fileStatus.Trim('?').Trim()));
                    list.Add(newItem);
                }
            }

            NewFiles = list.ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection containing all the new files.
        /// </summary>
        [Output]
        public ITaskItem[] NewFiles
        {
            get;
            set;
        }
    }
}
