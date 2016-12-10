//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts the issue numbers from all the commits that have not been merged to the
    /// <see cref="MergeTargetBranch"/>. Issue IDs are parsed using the <see cref="IssueIdRegex"/> or the default regex
    /// of '(?:#)(\d+)' if no regular expression is specified.
    /// </summary>
    public sealed class ExtractIssueIdsFromGitCommitMessages : GitCommandLineToolTask
    {
        // Grab any number that is preceded by a hash sign
        private const string DefaultIssueIdRegex = @"(?:#)(\d+)";

        /// <inheritdoc/>
        public override bool Execute()
        {
            string[] unmergedCommits = UnmergedCommits();
            Log.LogMessage(MessageImportance.Low, "Unmerged commits: ");
            foreach (var commit in unmergedCommits)
            {
                Log.LogMessage(MessageImportance.Low, commit);
            }

            var list = new SortedList<string, ITaskItem>();
            var regex = new System.Text.RegularExpressions.Regex(!string.IsNullOrEmpty(IssueIdRegex) ? IssueIdRegex : DefaultIssueIdRegex);
            foreach (var commit in unmergedCommits)
            {
                var logMessage = InvokeGit(
                    new[]
                    {
                        string.Format(
                            "log -n 1 --pretty=format:%B {0}",
                            commit)
                    });
                var issueIdMatch = regex.Match(logMessage);
                if (issueIdMatch.Success)
                {
                    var issueId = issueIdMatch.Groups[1].Value;
                    Log.LogMessage(MessageImportance.Low, "Issue for commit: [" + commit + "] is: [" + issueId + "]");
                    if (!list.ContainsKey(issueId))
                    {
                        var newItem = new TaskItem(issueId);
                        list.Add(issueId, newItem);
                    }
                }
            }

            IssueIds = list.Values.ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection containing the issue IDs which were extracted from the set of unmerged commits.
        /// </summary>
        [Output]
        public ITaskItem[] IssueIds
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the regular expression that is used to extract the issue ID from a GIT commit message. Defaults
        /// to '(?:#)(\d+)', i.e. a hash sign followed by a number of digits.
        /// </summary>
        public string IssueIdRegex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the branch to which the current set of commits should be merged.
        /// </summary>
        [Required]
        public string MergeTargetBranch
        {
            get;
            set;
        }

        private string[] UnmergedCommits()
        {
            // Get the SHA1 values for all the commits that haven't been merged to the target branch yet
            var gitOutput = InvokeGit(
                new[]
                {
                    string.Format("cherry {0}", MergeTargetBranch)
                });
            return gitOutput.Replace("+ ", Environment.NewLine).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
