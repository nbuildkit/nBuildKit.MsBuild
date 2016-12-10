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
    /// Defines a <see cref="ITask"/> that performs a Git clone.
    /// </summary>
    public sealed class GitClone : GitCommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the branch that should be checked out after the clone operation.
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
                arguments.Add(string.Format("clone \"{0}\" ", Repository.TrimEnd('\\')));

                if (!string.IsNullOrWhiteSpace(Branch))
                {
                    arguments.Add(string.Format("--branch \"{0}\" ", Branch.TrimEnd('\\')));
                }

                arguments.Add(string.Format("\"{0}\" ", GetAbsolutePath(Workspace).TrimEnd('\\')));
                arguments.Add("--quiet ");
            }

            InvokeGit(arguments);
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the URI of the repository.
        /// </summary>
        [Required]
        public string Repository
        {
            get;
            set;
        }
    }
}
