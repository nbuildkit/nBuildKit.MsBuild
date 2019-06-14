//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Core.FileSystem;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a TFS query to find the current branch of the workspace.
    /// </summary>
    public sealed class TfsCurrentBranch : TfCommandLineToolTask
    {
        private const string ErrorIdNoMapping = "NBuildKit.Vcs.Tfs.NoMapping";
        private const string ErrorIdNoWorkspace = "NBuildKit.Vcs.Tfs.NoWorkspace";

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsCurrentBranch"/> class.
        /// </summary>
        public TfsCurrentBranch()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsCurrentBranch"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public TfsCurrentBranch(IApplicationInvoker invoker)
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

        /// <summary>
        /// Gets or sets the name of the user which was used to create the workspace.
        /// </summary>
        public string UserName
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            // For some reason TFS is being silly and it won't actually get any information about the workspace
            // even if we're in it. So we force it to get information about all the workspaces so that it refreshes
            // the machine wide caches
            DataReceivedEventHandler standardOutputHandler = (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Low, e.Data);
                    }
                };
            var exitCode = InvokeTf(
                new[]
                {
                    "workspaces",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/owner:{0}",
                        UserName ?? Environment.UserName),
                },
                standardOutputHandler);
            if (exitCode != 0)
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
                    "No workspace could be found to gather the TFS branch from.");
                return false;
            }

            var output = GetTfOutput(new[] { "workfold" });

            // Expecting the output to be something like:
            //
            // ==============================================
            // Workspace : MyMachine (Darth Vader)
            // Collection: http://tfs:8080/tfs/DeathStar
            // $/: C:\vcs\tfs\deathstar
            //
            // So the item we're looking for is the fourth line
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4)
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
                    "No workspace could be found to gather the TFS branch from.");
                return false;
            }

            var line = lines[3];
            var sections = line.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length < 2)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoMapping),
                    ErrorIdNoMapping,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No change set number could be found. The tf.exe output was '{0}'.",
                    output);
                return false;
            }

            var currentDirectory = GetAbsolutePath(Workspace);
            var relativePath = PathUtilities.GetDirectoryPathRelativeToDirectory(currentDirectory, sections[1].Trim());

            CurrentBranch = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                sections[0].Trim().TrimEnd('/'),
                relativePath.TrimStart('\\').Replace('\\', '/'));

            return !Log.HasLoggedErrors;
        }
    }
}
