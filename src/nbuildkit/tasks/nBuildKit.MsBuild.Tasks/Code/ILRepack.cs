//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that uses the ILRepack command line tool to merge multiple assemblies into a single assembly.
    /// </summary>
    public sealed class ILRepack : CommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the collection of assemblies that should be merged into the primary assembly.
        /// </summary>
        [Required]
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
                arguments.Add("/verbose ");

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
                var text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    Path.GetFileName(ILRepackExe.ItemSpec),
                    exitCode);
                Log.LogError(text);
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
        /// Gets or sets the full path to the working directory
        /// </summary>
        [Required]
        public ITaskItem WorkingDirectory
        {
            get;
            set;
        }
    }
}
