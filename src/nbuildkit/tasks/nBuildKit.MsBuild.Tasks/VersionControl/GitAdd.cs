//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Core.FileSystem;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git add.
    /// </summary>
    public sealed class GitAdd : GitCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitAdd"/> class.
        /// </summary>
        public GitAdd()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitAdd"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GitAdd(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

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
                                    PathUtilities.GetFilePathRelativeToDirectory(GetAbsolutePath(taskItem), workingDirectory)));
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
