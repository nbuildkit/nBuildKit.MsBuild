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
    /// Defines a <see cref="ITask"/> that performs a Git commit.
    /// </summary>
    public sealed class GitCommit : GitCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add("commit ");
                arguments.Add("--all ");
                arguments.Add(string.Format("--message=\"{0}\" ", Message.TrimEnd('\\')));
                arguments.Add("--quiet ");
            }

            InvokeGit(arguments);

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the commit message.
        /// </summary>
        [Required]
        public string Message
        {
            get;
            set;
        }
    }
}
