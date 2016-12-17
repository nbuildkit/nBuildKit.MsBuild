//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git merge.
    /// </summary>
    public sealed class GitMerge : GitCommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the branch that should be merged into the current branch.
        /// </summary>
        [Required]
        public string BranchToMerge
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add("merge");
                arguments.Add(BranchToMerge);

                if (!FastForwardAllowed)
                {
                    arguments.Add("--no-ff ");
                }

                arguments.Add("--commit");
                arguments.Add("--no-edit");
                arguments.Add("--no-progress");
                arguments.Add("--verbose");
            }

            InvokeGit(arguments);

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets a value indicating whether a fast forward merge is allowe.
        /// </summary>
        public bool FastForwardAllowed
        {
            get;
            set;
        }
    }
}
