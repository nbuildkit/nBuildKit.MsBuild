//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Packaging
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a NuGet install.
    /// </summary>
    public sealed class NuGetInstall : NuGetCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetInstall"/> class.
        /// </summary>
        public NuGetInstall()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetInstall"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public NuGetInstall(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the version of the package should be included in the
        /// install directory.
        /// </summary>
        public bool ExcludeVersion
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "install \"{0}\" ", PackageName));
                if (!string.IsNullOrWhiteSpace(PackageVersion))
                {
                    arguments.Add(string.Format(CultureInfo.InvariantCulture, "-Version \"{0}\" ", PackageVersion));
                }

                if (ExcludeVersion)
                {
                    arguments.Add("-ExcludeVersion ");
                }

                arguments.Add("-NonInteractive ");
                arguments.Add("-Verbosity detailed ");
                arguments.Add("-NoCache ");

                // Make sure we remove the back-slash because if we don't then
                // the closing quote will be eaten by the command line parser. Note that
                // this is only necessary because we're dealing with a directory
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-OutputDirectory \"{0}\" ", GetAbsolutePath(PackagesDirectory).TrimEnd('\\')));

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
                    ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                    Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
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
        public string PackageVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory into which the package should be installed.
        /// </summary>
        [Required]
        public ITaskItem PackagesDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the NuGet sources from which the package may be taken.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Sources
        {
            get;
            set;
        }
    }
}
