//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts version numbers from a version file written by the <see cref="CalculateSemanticVersionWithGitVersion"/> task.
    /// </summary>
    public sealed class GetSemanticVersionFromFile : NBuildKitMsBuildTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            string text;
            using (var reader = new StreamReader(GetAbsolutePath(VersionFile)))
            {
                text = reader.ReadToEnd();
            }

            const string fullSemVersionStart = "\"FullSemVer\": \"";
            var index = text.IndexOf(fullSemVersionStart, StringComparison.OrdinalIgnoreCase);
            VersionSemanticFull = text.Substring(
                index + fullSemVersionStart.Length,
                text.IndexOf("\"", index + fullSemVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + fullSemVersionStart.Length));

            const string nugetSemVersionStart = "\"NuGetSemVer\": \"";
            index = text.IndexOf(nugetSemVersionStart, StringComparison.OrdinalIgnoreCase);
            VersionSemanticNuGet = text.Substring(
                index + nugetSemVersionStart.Length,
                text.IndexOf("\"", index + nugetSemVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + nugetSemVersionStart.Length));

            const string semVersionStart = "\"SemVer\": \"";
            index = text.IndexOf(semVersionStart, StringComparison.OrdinalIgnoreCase);
            VersionSemantic = text.Substring(
                index + semVersionStart.Length,
                text.IndexOf("\"", index + semVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + semVersionStart.Length));

            const string majorVersionStart = "\"Major\": \"";
            index = text.IndexOf(majorVersionStart, StringComparison.OrdinalIgnoreCase);
            var versionMajorText = text.Substring(
                index + majorVersionStart.Length,
                text.IndexOf("\"", index + majorVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + majorVersionStart.Length));
            VersionMajor = int.Parse(versionMajorText, CultureInfo.InvariantCulture);
            VersionMajorNext = VersionMajor + 1;

            const string minorVersionStart = "\"Minor\": \"";
            index = text.IndexOf(minorVersionStart, StringComparison.OrdinalIgnoreCase);
            var versionMinorText = text.Substring(
                index + minorVersionStart.Length,
                text.IndexOf("\"", index + minorVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + minorVersionStart.Length));
            VersionMinor = int.Parse(versionMinorText, CultureInfo.InvariantCulture);
            VersionMinorNext = VersionMinor + 1;

            const string patchVersionStart = "\"Patch\": \"";
            index = text.IndexOf(patchVersionStart, StringComparison.OrdinalIgnoreCase);
            var versionPatchText = text.Substring(
                index + patchVersionStart.Length,
                text.IndexOf("\"", index + patchVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + patchVersionStart.Length));
            VersionPatch = int.Parse(versionPatchText, CultureInfo.InvariantCulture);
            VersionPatchNext = VersionPatch + 1;

            const string buildVersionStart = "\"Build\": \"";
            index = text.IndexOf(buildVersionStart, StringComparison.OrdinalIgnoreCase);
            var versionBuildText = text.Substring(
                index + buildVersionStart.Length,
                text.IndexOf("\"", index + buildVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + buildVersionStart.Length));
            VersionBuild = int.Parse(versionBuildText, CultureInfo.InvariantCulture);
            VersionBuildNext = VersionBuild + 1;

            const string prereleaseVersionStart = "\"PreRelease\": \"";
            index = text.IndexOf(prereleaseVersionStart, StringComparison.OrdinalIgnoreCase);
            VersionPrerelease = text.Substring(
                index + prereleaseVersionStart.Length,
                text.IndexOf("\"", index + prereleaseVersionStart.Length, StringComparison.OrdinalIgnoreCase) - (index + prereleaseVersionStart.Length));

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the build number of the version.
        /// </summary>
        [Output]
        public int VersionBuild
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the next build number relative to the current <see cref="VersionBuild"/>.
        /// </summary>
        [Output]
        public int VersionBuildNext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the file that contains the version information.
        /// </summary>
        [Required]
        public ITaskItem VersionFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the major number of the version.
        /// </summary>
        [Output]
        public int VersionMajor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the next major number relative to the current <see cref="VersionMajor"/>.
        /// </summary>
        [Output]
        public int VersionMajorNext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the minor number of the version.
        /// </summary>
        [Output]
        public int VersionMinor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the next minor number relative to the current <see cref="VersionMinor"/>.
        /// </summary>
        [Output]
        public int VersionMinorNext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the patch number of the version.
        /// </summary>
        [Output]
        public int VersionPatch
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the next patch number relative to the current <see cref="VersionPatch"/>.
        /// </summary>
        [Output]
        public int VersionPatchNext
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the prerelease information of the semantic version.
        /// </summary>
        [Output]
        public string VersionPrerelease
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
