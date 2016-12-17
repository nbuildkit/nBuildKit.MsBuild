//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts version control information from a file written.
    /// </summary>
    public sealed class GetVcsInfoFromFile : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the name of the branch.
        /// </summary>
        [Output]
        public string Branch
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            string text;
            using (var reader = new StreamReader(GetAbsolutePath(InfoFile)))
            {
                text = reader.ReadToEnd();
            }

            const string revisionStart = "\"revision\": \"";
            var index = text.IndexOf(revisionStart, StringComparison.OrdinalIgnoreCase);
            Revision = text.Substring(
                index + revisionStart.Length,
                text.IndexOf("\"", index + revisionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + revisionStart.Length));

            const string branchStart = "\"branch\": \"";
            index = text.IndexOf(branchStart, StringComparison.OrdinalIgnoreCase);
            Branch = text.Substring(
                index + branchStart.Length,
                text.IndexOf("\"", index + branchStart.Length, StringComparison.OrdinalIgnoreCase) - (index + branchStart.Length));

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path of the VCS information file.
        /// </summary>
        [Required]
        public ITaskItem InfoFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the VCS revision.
        /// </summary>
        [Output]
        public string Revision
        {
            get;
            set;
        }
    }
}
