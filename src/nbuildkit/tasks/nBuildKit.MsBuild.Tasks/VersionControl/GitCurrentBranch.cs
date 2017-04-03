//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git query to find the current branch of the workspace.
    /// </summary>
    public sealed class GitCurrentBranch : GitCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitCurrentBranch"/> class.
        /// </summary>
        public GitCurrentBranch()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitCurrentBranch"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GitCurrentBranch(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the current branch.
        /// </summary>
        [Output]
        public string CurrentBranch
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // Get the current branch
            {
                var output = GetGitOutput(new[] { "rev-parse --abbrev-ref HEAD" });
                CurrentBranch = output.Trim();
            }

            if (string.IsNullOrEmpty(CurrentBranch))
            {
                var output = GetGitOutput(new[] { "rev-parse HEAD" });
                var currentRevision = output.Trim();
                if (!string.IsNullOrEmpty(currentRevision))
                {
                    Log.LogMessage(MessageImportance.High, "Current branch not found. Possibly on a detached head. Searching for branches that contain current commit.");

                    // The current HEAD is probably a detached one so we need to find the branch that holds the
                    // current commit
                    var lines = new List<string>();
                    DataReceivedEventHandler standardOutputHandler =
                        (s, e) =>
                        {
                            lines.Add(e.Data);
                        };

                    InvokeGit(new[] { string.Format(CultureInfo.InvariantCulture, "branch --contains {0}", currentRevision) }, standardOutputHandler: standardOutputHandler);

                    // Explicitly ignoring the error codes because GIT is a little silly with error codes
                    // It produces error codes even if there's not really an error.
                    foreach (var line in lines)
                    {
                        if (line.Contains("detached"))
                        {
                            continue;
                        }

                        CurrentBranch = line.Trim(' ', '*');
                        break;
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
