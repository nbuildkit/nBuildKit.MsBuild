//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Testing
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the Pester powershell unit testing framework on a given directory.
    /// </summary>
    public sealed class InvokePesterOnDirectory : PowershellCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            // Create the script
            var scriptPath = Path.Combine(
                GetAbsolutePath(TemporaryDirectory),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.ps1",
                    Guid.NewGuid().ToString()));
            using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
            {
                // Stop if anything goes wrong
                writer.WriteLine("$ErrorActionPreference = 'Stop'");

                // Add the pester directory to the module path
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "$env:PSModulePath = $env:PSModulePath + ';' + '{0}'",
                        GetAbsolutePath(PesterModulePath)));

                // Import pester
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "& Import-Module '{0}\\Pester.psm1' ",
                        GetAbsolutePath(PesterModulePath)));

                // Execute pester tests
                writer.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "$result = Invoke-Pester -Path '{0}' -OutputFormat NUnitXml -OutputFile '{1}' -EnableExit",
                        GetAbsolutePath(TestsDirectory),
                        GetAbsolutePath(ReportFile)));
            }

            InvokePowershellFile(scriptPath);
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the Pester module.
        /// </summary>
        [Required]
        public ITaskItem PesterModulePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the report file.
        /// </summary>
        [Required]
        public ITaskItem ReportFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory containing the Pester tests.
        /// </summary>
        [Required]
        public ITaskItem TestsDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to a directory that can be used to write temporary files.
        /// </summary>
        [Required]
        public ITaskItem TemporaryDirectory
        {
            get;
            set;
        }
    }
}
