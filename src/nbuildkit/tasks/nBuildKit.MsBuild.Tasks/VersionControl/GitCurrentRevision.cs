//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git query to find the current revision of the workspace.
    /// </summary>
    public sealed class GitCurrentRevision : GitCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitCurrentRevision"/> class.
        /// </summary>
        public GitCurrentRevision()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitCurrentRevision"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GitCurrentRevision(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

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
