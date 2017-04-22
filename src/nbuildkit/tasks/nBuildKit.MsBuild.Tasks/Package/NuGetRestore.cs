//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Packaging
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a NuGet restore.
    /// </summary>
    public sealed class NuGetRestore : NuGetCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetRestore"/> class.
        /// </summary>
        public NuGetRestore()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetRestore"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public NuGetRestore(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var packageFile = GetAbsolutePath(PackageFile);
            if (!File.Exists(packageFile))
            {
                Log.LogMessage(MessageImportance.High, "File does not exist: {0}", packageFile);
            }

            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "restore \"{0}\" ", packageFile));
                arguments.Add("-NonInteractive -Verbosity detailed -NoCache ");

                // Make sure we remove the back-slash because if we don't then
                // the closing quote will be eaten by the command line parser. Note that
                // this is only necessary because we're dealing with a directory
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-PackagesDirectory \"{0}\" ", GetAbsolutePath(PackageDirectory).TrimEnd('\\')));

                // If the user has specified any sources to install from then only search those sources.
                if (Sources != null)
                {
                    foreach (var source in Sources)
                    {
                        // Make sure we remove the back-slash because if we don't then
                        // the closing quote will be eaten by the command line parser. Note that
                        // this is only necessary because we're dealing with a directory
                        arguments.Add(string.Format(CultureInfo.InvariantCulture, "-Source \"{0}\" ", source.ItemSpec.TrimEnd('\\')));
                    }
                }
            }

            var exitCode = InvokeNuGet(arguments);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdApplicationNonzeroExitCode),
                    ErrorIdApplicationNonzeroExitCode,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    Path.GetFileName(NuGetExecutablePath.ItemSpec),
                    exitCode);
                return false;
            }

            return !Log.HasLoggedErrors;
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
        /// Gets or sets the full path to the package file that should be installed.
        /// </summary>
        [Required]
        public ITaskItem PackageFile
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
