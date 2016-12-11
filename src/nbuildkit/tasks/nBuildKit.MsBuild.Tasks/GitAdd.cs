//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git add.
    /// </summary>
    public sealed class GitAdd : GitCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var workingDirectory = GetAbsolutePath(Workspace);
            var arguments = new List<string>();
            {
                arguments.Add("add");
                if (FilesToAdd != null)
                {
                    ITaskItem[] files = FilesToAdd;
                    for (int i = 0; i < files.Length; i++)
                    {
                        ITaskItem taskItem = files[i];
                        if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                        {
                            arguments.Add(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "\"{0}\" ",
                                    GetRelativePath(GetAbsolutePath(taskItem), workingDirectory)));
                        }
                    }
                }
                else
                {
                    arguments.Add("--all");
                }
            }

            InvokeGit(arguments);

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the files that should be added.
        /// </summary>
        public ITaskItem[] FilesToAdd
        {
            get;
            set;
        }
    }
}
