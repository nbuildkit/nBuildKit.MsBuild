//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a NuGet install.
    /// </summary>
    public sealed class NuGetInstall : NuGetCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add(string.Format("install \"{0}\" ", PackageName));
                if (!string.IsNullOrWhiteSpace(PackageVersion))
                {
                    arguments.Add(string.Format("-Version \"{0}\" ", PackageVersion));
                }

                arguments.Add("-NonInteractive -Verbosity detailed -NoCache ");

                // Make sure we remove the back-slash because if we don't then
                // the closing quote will be eaten by the command line parser. Note that
                // this is only necessary because we're dealing with a directory
                arguments.Add(string.Format("-OutputDirectory \"{0}\" ", GetAbsolutePath(PackageDirectory).TrimEnd('\\')));

                // If the user has specified any sources to install from then only search those sources.
                if (Sources != null)
                {
                    foreach (var source in Sources)
                    {
                        // Make sure we remove the back-slash because if we don't then
                        // the closing quote will be eaten by the command line parser. Note that
                        // this is only necessary because we're dealing with a directory
                        arguments.Add(string.Format("-Source \"{0}\" ", source.ItemSpec.TrimEnd('\\')));
                    }
                }
            }

            var exitCode = InvokeNuGet(arguments);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        Path.GetFileName(NuGetExecutablePath.ItemSpec),
                        exitCode));
                return false;
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the name of the package that should be installed.
        /// </summary>
        [Required]
        public string PackageName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the version of the package that should be installed.
        /// </summary>
        [Required]
        public string PackageVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory into which the package should be installed.
        /// </summary>
        [Required]
        public ITaskItem PackageDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the NuGet sources from which the package may be taken.
        /// </summary>
        public ITaskItem[] Sources
        {
            get;
            set;
        }
    }
}
