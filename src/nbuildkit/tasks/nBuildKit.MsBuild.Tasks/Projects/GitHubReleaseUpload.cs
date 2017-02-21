//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Projects
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that uploads binaries to a release on GitHub.
    /// </summary>
    public sealed class GitHubReleaseUpload : CommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubReleaseUpload"/> class.
        /// </summary>
        public GitHubReleaseUpload()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubReleaseUpload"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GitHubReleaseUpload(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var gitHubToken = Environment.GetEnvironmentVariable("GitHubToken");
            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "upload --security-token {0} ", gitHubToken.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "--user \"{0}\" ", UserName.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "--repo \"{0}\" ", Repository.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "--tag \"{0}\" ", Tag.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "--name \"{0}\" ", FileName.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "--file \"{0}\"", GetAbsolutePath(FilePath).TrimEnd('\\')));
            }

            Log.LogMessage(MessageImportance.Normal, "Uploading file to GitHub release");
            DataReceivedEventHandler standardOutputHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };
            DataReceivedEventHandler standardErrorHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogError(e.Data);
                    }
                };

            var exitCode = InvokeCommandLineTool(
                GitHubReleasePath,
                arguments,
                WorkingDirectory,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);

            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        Path.GetFileName(GetFullToolPath(GitHubReleasePath)),
                        exitCode));
                return false;
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the name of the file as it should be linked in the release.
        /// </summary>
        [Required]
        public string FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path of the file that should be uploaded.
        /// </summary>
        [Required]
        public ITaskItem FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the GitHubRelease command line application.
        /// </summary>
        [Required]
        public ITaskItem GitHubReleasePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the repository to which the release is related.
        /// </summary>
        [Required]
        public string Repository
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tag to which the release is related.
        /// </summary>
        [Required]
        public string Tag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user name for the user who is performing the release.
        /// </summary>
        [Required]
        public string UserName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the working directory.
        /// </summary>
        [Required]
        public ITaskItem WorkingDirectory
        {
            get;
            set;
        }
    }
}
