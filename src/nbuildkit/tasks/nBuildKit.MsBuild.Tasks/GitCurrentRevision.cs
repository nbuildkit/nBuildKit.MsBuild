//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git query to find the current revision of the workspace.
    /// </summary>
    public sealed class GitCurrentRevision : GitCommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the current revision.
        /// </summary>
        [Output]
        public string CurrentRevision
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // Get the current revision
            {
                var output = GetGitOutput(
                    new[]
                    {
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "rev-parse {0}",
                            !string.IsNullOrEmpty(RevisionSpec) ? RevisionSpec : "HEAD")
                    });
                CurrentRevision = output.Trim();
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the branch for which the current revision should be obtained.
        /// </summary>
        public string RevisionSpec
        {
            get;
            set;
        }
    }
}
