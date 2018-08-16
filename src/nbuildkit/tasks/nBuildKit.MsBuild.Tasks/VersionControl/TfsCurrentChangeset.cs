//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a TFS query to find the current changeset of the workspace.
    /// </summary>
    public sealed class TfsCurrentChangeSet : TfCommandLineToolTask
    {
        private const string ErrorIdNoChangesetNumber = "NBuildKit.Vcs.Tfs.NoChangeSet";
        private const string ErrorIdNoWorkspace = "NBuildKit.Vcs.Tfs.NoWorkspace";

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsCurrentChangeSet"/> class.
        /// </summary>
        public TfsCurrentChangeSet()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsCurrentChangeSet"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public TfsCurrentChangeSet(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the current change set.
        /// </summary>
        [Output]
        public string CurrentChangeSet
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var output = GetTfOutput(new[] { "history . /recursive /noprompt /stopafter:1 /version:W" });

            // Expecting the output to be something like:
            // Changeset User              Date       Comment
            // --------- ----------------- ---------- ----------------------------------------
            // 123456    Darth Vader 27/07/2199 Update death star security settings
            //
            // So the item we're looking for is the 123456 number at the start of the 3rd line
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoWorkspace),
                    ErrorIdNoWorkspace,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No workspace could be found to gather the TFS change set number from.");
                return false;
            }

            var line = lines[2];
            var indexOfSpace = line.IndexOf(' ');
            if (indexOfSpace < 0)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoChangesetNumber),
                    ErrorIdNoChangesetNumber,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No change set number could be found. The tf.exe output was '{0}'.",
                    output);
                return false;
            }

            CurrentChangeSet = line.Substring(0, indexOfSpace).Trim();

            return !Log.HasLoggedErrors;
        }
    }
}
