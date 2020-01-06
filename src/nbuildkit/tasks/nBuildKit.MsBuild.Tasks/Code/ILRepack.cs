//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that uses the ILRepack command line tool to merge multiple assemblies into a single assembly.
    /// </summary>
    public sealed class ILRepack : CommandLineToolTask
    {
        private static Verbosity ToVerbosity(string verbosity)
        {
            if (string.Equals(verbosity, "q", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "quiet", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Quiet;
            }

            if (string.Equals(verbosity, "m", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "minimal", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Minimal;
            }

            if (string.Equals(verbosity, "n", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "normal", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Normal;
            }

            if (string.Equals(verbosity, "d", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "detailed", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Detailed;
            }

            if (string.Equals(verbosity, "diag", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "diagnostic", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Diagnostic;
            }

            // Unknown verbosity. Just assume normal
            return Verbosity.Normal;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ILRepack"/> class.
        /// </summary>
        public ILRepack()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ILRepack"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public ILRepack(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the collection of assemblies that should be merged into the primary assembly.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] AssembliesToMerge
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add("/union ");
                arguments.Add("/internalize ");
                arguments.Add("/wildcards ");

                var verbosity = ToVerbosity(VerbosityForCurrentMsBuildInstance());
                if (verbosity > Verbosity.Normal)
                {
                    arguments.Add("/verbose ");
                }

                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/log:\"{0}\"",
                        GetAbsolutePath(LogFile).TrimEnd('\\')));
                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/ver:{0}",
                        Version));

                var keyFile = GetAbsolutePath(KeyFile);
                if (!string.IsNullOrEmpty(keyFile))
                {
                    arguments.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "/keyfile:\"{0}\"",
                            keyFile.TrimEnd('\\')));
                }

                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "/out:\"{0}\"",
                        GetAbsolutePath(MergedAssembly).TrimEnd('\\')));

                var primaryAssemblyPath = GetAbsolutePath(PrimaryAssembly).TrimEnd('\\');
                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "\"{0}\" ",
                        primaryAssemblyPath));

                foreach (var assemblyToMerge in AssembliesToMerge)
                {
                    var assemblyPath = GetAbsolutePath(assemblyToMerge).TrimEnd('\\');
                    if (!string.Equals(primaryAssemblyPath, assemblyPath, StringComparison.OrdinalIgnoreCase))
                    {
                        arguments.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "\"{0}\" ",
                                assemblyPath));
                    }
                }
            }

            var exitCode = InvokeCommandLineTool(
                ILRepackExe,
                arguments,
                workingDirectory: WorkingDirectory);

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
                    Path.GetFileName(ILRepackExe.ItemSpec),
                    exitCode);
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the ILRepack command line executable.
        /// </summary>
        [Required]
        public ITaskItem ILRepackExe
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to an assembly signing key file.
        /// </summary>
        public ITaskItem KeyFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the log file.
        /// </summary>
        [Required]
        public ITaskItem LogFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the location where the merged assembly file should be placed.
        /// </summary>
        [Required]
        public ITaskItem MergedAssembly
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path of the assembly into which all the other assemblies should be merged.
        /// </summary>
        [Required]
        public ITaskItem PrimaryAssembly
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the version of the assembly.
        /// </summary>
        [Required]
        public string Version
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

        /// <summary>
        /// Defines the verbosity levels for MsBuild.
        /// </summary>
        private enum Verbosity
        {
            Quiet = 0,
            Minimal = 1,
            Normal = 2,
            Detailed = 4,
            Diagnostic = 8,
        }
    }
}
