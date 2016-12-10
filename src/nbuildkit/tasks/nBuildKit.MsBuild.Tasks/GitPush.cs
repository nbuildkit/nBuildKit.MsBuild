//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git push.
    /// </summary>
    public sealed class GitPush : GitCommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the branch that should be pushed to the remote repository.
        /// </summary>
        public string Branch
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add("push origin ");
                if (!string.IsNullOrWhiteSpace(Branch))
                {
                    arguments.Add(string.Format("\"{0}\" ", Branch.TrimEnd('\\')));
                }
                else
                {
                    arguments.Add("--all ");
                }

                if (PushTags && !string.IsNullOrWhiteSpace(Branch))
                {
                    arguments.Add("--tags ");
                }

                arguments.Add("--porcelain --atomic ");
            }

            InvokeGit(arguments);

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets a value indicating whether tags should be pushed to the remote repository.
        /// </summary>
        public bool PushTags
        {
            get;
            set;
        }
    }
}
