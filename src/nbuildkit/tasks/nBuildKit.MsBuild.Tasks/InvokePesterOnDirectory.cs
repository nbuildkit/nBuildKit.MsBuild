//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the Pester powershell unit testing framework on a given directory.
    /// </summary>
    public sealed class InvokePesterOnDirectory : CommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            try
            {
                // Create the script
                var scriptPath = Path.Combine(
                    GetAbsolutePath(TempDirectory),
                    string.Format(
                        "{0}.ps1",
                        Guid.NewGuid().ToString()));
                using (var writer = new StreamWriter(scriptPath, false, Encoding.Unicode))
                {
                    // Stop if anything goes wrong
                    writer.WriteLine("$ErrorActionPreference = 'Stop'");

                    // Add the pester directory to the module path
                    writer.WriteLine(
                        string.Format(
                            "$env:PSModulePath = $env:PSModulePath + ';' + '{0}'",
                            GetAbsolutePath(PesterModulePath)));

                    // Import pester
                    writer.WriteLine(
                        string.Format(
                            "& Import-Module '{0}\\Pester.psm1' ",
                            GetAbsolutePath(PesterModulePath)));

                    // Execute pester tests
                    writer.WriteLine(
                        string.Format(
                            "$result = Invoke-Pester -Path '{0}' -OutputFormat NUnitXml -OutputFile '{1}' -EnableExit -Verbose",
                            GetAbsolutePath(TestsDirectory),
                            GetAbsolutePath(ReportFile)));
                }

                var arguments = new List<string>();
                {
                    arguments.Add("-NonInteractive ");
                    arguments.Add("-NoProfile ");
                    arguments.Add("-ExecutionPolicy Bypass ");
                    arguments.Add(
                        string.Format(
                            "-File \"{0}\"",
                            scriptPath));
                }

                var powershellPath = GetFullToolPath(PowershellExePath);
                DataReceivedEventHandler standardErrorOutput =
                    (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            if (IgnoreErrors)
                            {
                                Log.LogWarning(e.Data);
                            }
                            else
                            {
                                Log.LogError(e.Data);
                            }
                        }
                    };

                var exitCode = InvokeCommandlineTool(
                    powershellPath,
                    arguments,
                    standardErrorHandler: standardErrorOutput);

                if (exitCode != 0)
                {
                    var text = string.Format(
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        Path.GetFileName(powershellPath),
                        exitCode);
                    if (IgnoreExitCode)
                    {
                        Log.LogWarning(text);
                    }
                    else
                    {
                        Log.LogError(text);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(e.ToString());
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets a value indicating whether error should be ignored.
        /// </summary>
        [Required]
        public bool IgnoreErrors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the exit code should be ignored.
        /// </summary>
        [Required]
        public bool IgnoreExitCode
        {
            get;
            set;
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
        /// Gets or sets the full path to the Powershell executable.
        /// </summary>
        [Required]
        public ITaskItem PowershellExePath
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
        public ITaskItem TempDirectory
        {
            get;
            set;
        }
    }
}
