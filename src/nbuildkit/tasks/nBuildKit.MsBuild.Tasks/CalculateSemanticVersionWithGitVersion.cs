//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that gets the semantic version information for the workspace via the
    /// GitVersion command line tool.
    /// </summary>
    public sealed class CalculateSemanticVersionWithGitVersion : CommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add("/nofetch ");

                if (!string.IsNullOrEmpty(UserName))
                {
                    arguments.Add(string.Format("/u \"{0}\" ", UserName));
                    arguments.Add("/p \"%GitPassWord%\" ");
                }

                if (!string.IsNullOrEmpty(RemoteRepositoryUrl) && !string.IsNullOrEmpty(UserName))
                {
                    arguments.Add(string.Format("/url \"{0}\" ", RemoteRepositoryUrl));
                }

                arguments.Add(string.Format("/l \"{0}\" ", LogPath));
            }

            var text = new StringBuilder();
            DataReceivedEventHandler standardOutputHandler = (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        text.Append(e.Data);
                    }
                };

            var exitCode = InvokeCommandlineTool(
                ExePath,
                arguments,
                standardOutputHandler: standardOutputHandler);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        System.IO.Path.GetFileName(ExePath.ItemSpec),
                        exitCode));
                Log.LogError(string.Format("Output was: {0}", text));

                return false;
            }

            try
            {
                string versionText = text.ToString();
                const string fullSemVersionStart = "\"FullSemVer\":";
                var index = versionText.IndexOf(fullSemVersionStart);
                VersionSemanticFull = versionText.Substring(
                        index + fullSemVersionStart.Length,
                        versionText.IndexOf(",", index + fullSemVersionStart.Length) - (index + fullSemVersionStart.Length))
                    .Trim('"');

                const string nugetSemVersionStart = "\"NuGetVersionV2\":";
                index = versionText.IndexOf(nugetSemVersionStart);
                VersionSemanticNuGet = versionText.Substring(
                        index + nugetSemVersionStart.Length,
                        versionText.IndexOf(",", index + nugetSemVersionStart.Length) - (index + nugetSemVersionStart.Length))
                    .Trim('"');

                const string semVersionStart = "\"MajorMinorPatch\":";
                index = versionText.IndexOf(semVersionStart);
                VersionSemantic = versionText.Substring(
                        index + semVersionStart.Length,
                        versionText.IndexOf(",", index + semVersionStart.Length) - (index + semVersionStart.Length))
                    .Trim('"');

                const string majorVersionStart = "\"Major\":";
                index = versionText.IndexOf(majorVersionStart);
                VersionMajor = versionText.Substring(
                        index + majorVersionStart.Length,
                        versionText.IndexOf(",", index + majorVersionStart.Length) - (index + majorVersionStart.Length))
                    .Trim('"');

                const string minorVersionStart = "\"Minor\":";
                index = versionText.IndexOf(minorVersionStart);
                VersionMinor = versionText.Substring(
                        index + minorVersionStart.Length,
                        versionText.IndexOf(",", index + minorVersionStart.Length) - (index + minorVersionStart.Length))
                    .Trim('"');

                const string patchVersionStart = "\"Patch\":";
                index = versionText.IndexOf(patchVersionStart);
                VersionPatch = versionText.Substring(
                        index + patchVersionStart.Length,
                        versionText.IndexOf(",", index + patchVersionStart.Length) - (index + patchVersionStart.Length))
                    .Trim('"');

                const string buildVersionStart = "\"BuildMetaData\":";
                index = versionText.IndexOf(buildVersionStart);
                VersionBuild = versionText.Substring(
                        index + buildVersionStart.Length,
                        versionText.IndexOf(",", index + buildVersionStart.Length) - (index + buildVersionStart.Length))
                    .Trim('"');

                const string tagVersionStart = "\"PreReleaseTag\":";
                index = versionText.IndexOf(tagVersionStart);
                VersionPreRelease = versionText.Substring(
                        index + tagVersionStart.Length,
                        versionText.IndexOf(",", index + tagVersionStart.Length) - (index + tagVersionStart.Length))
                    .Trim('"');
                if (VersionPreRelease.IndexOf(".") > -1)
                {
                    VersionPreRelease = VersionPreRelease.Substring(0, VersionPreRelease.IndexOf("."));
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString());
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path of the command line executable.
        /// </summary>
        [Required]
        public ITaskItem ExePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path of the log file.
        /// </summary>
        [Required]
        public ITaskItem LogPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the URL of the remote repository.
        /// </summary>
        [Required]
        public string RemoteRepositoryUrl
        {
            get;
            set;
        }

        /// <summary>
        /// Updates the environment variables for the application prior to execution.
        /// </summary>
        /// <param name="environmentVariables">
        ///     The environment variables for the application. The environment variables for the process can be
        ///     changed by altering the collection.
        /// </param>
        protected override void UpdateEnvironmentVariables(StringDictionary environmentVariables)
        {
            // GitVersion does all kinds of magic when it detects that it is running on a build server.
            // That magic can stuff up any changes we make to the git workspace because if we change branches
            // (e.g. during a merge) then GitVersion may change back to the original branch. So we remove any
            // indication that GitVersion is running on a buildserver. Until GitVersion has a flag to do so
            // we do this the hard way by removing all the environment variables linked to build servers
            var knownBuildServerEnvironmentKeys = new List<string>
                            {
                                "APPVEYOR",
                                "BUILD",
                                "BuildRunner",
                                "CI",
                                "GIT",
                                "GITLAB",
                                "JENKINS",
                                "TEAMCITY",
                                "TF",
                                "TRAVIS",
                            };

            var variablesToRemove = new List<string>();
            foreach (DictionaryEntry pair in environmentVariables)
            {
                var key = pair.Key as string;
                if (knownBuildServerEnvironmentKeys.Any(s => key.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    variablesToRemove.Add(key);
                }
            }

            foreach (var keyToRemove in variablesToRemove)
            {
                environmentVariables.Remove(keyToRemove);
            }
        }

        /// <summary>
        /// Gets or sets the name of the user that should be used to connect to the remote repository. If a username
        /// is provied then the password should be made available via the 'GitPassWord' environment variable for the
        /// process.
        /// </summary>
        public string UserName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the build number of the version.
        /// </summary>
        [Output]
        public string VersionBuild
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the major number of the version.
        /// </summary>
        [Output]
        public string VersionMajor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the minor number of the version.
        /// </summary>
        [Output]
        public string VersionMinor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the patch number of the version.
        /// </summary>
        [Output]
        public string VersionPatch
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the prerelease information of the semantic version.
        /// </summary>
        [Output]
        public string VersionPreRelease
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the semantic version.
        /// </summary>
        [Output]
        public string VersionSemantic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the complete semantic version, including the prerelease information.
        /// </summary>
        [Output]
        public string VersionSemanticFull
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the complete semantic version following the NuGet specification.
        /// </summary>
        [Output]
        public string VersionSemanticNuGet
        {
            get;
            set;
        }
    }
}
