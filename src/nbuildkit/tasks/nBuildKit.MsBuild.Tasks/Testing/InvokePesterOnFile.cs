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
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Testing
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the Pester powershell unit testing framework on a given set of test files.
    /// </summary>
    public sealed class InvokePesterOnFile : PowershellCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var testArguments = new Dictionary<string, string>();
            if (TestArguments != null)
            {
                for (int i = 0; i < TestArguments.Length; i++)
                {
                    var parameter = TestArguments[i];
                    var value = parameter.GetMetadata("Value");

                    // Expecting that the taskItems have:
                    // - parameter.ItemSpec: Name of the parameter to pass
                    // - parameter.Value:    Value of the parameter
                    if (!string.IsNullOrEmpty(parameter.ItemSpec) && !string.IsNullOrEmpty(value))
                    {
                        testArguments.Add(parameter.ItemSpec, value);
                    }
                }
            }

            var parameterText = string.Empty;
            foreach (var pair in testArguments)
            {
                parameterText += string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} = \"{1}\";",
                    pair.Key,
                    pair.Value);
            }

            // Create the script
            var scriptPath = Path.Combine(
                GetAbsolutePath(TempDirectory),
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
                        "$result = Invoke-Pester -Script @{{ Path = '{0}'; Parameters = @{{ {1} }} }} -OutputFormat NUnitXml -OutputFile '{2}' -EnableExit -Verbose",
                        GetAbsolutePath(TestFile),
                        parameterText,
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
        /// Gets or sets the full path to the file containing the Pester tests.
        /// </summary>
        [Required]
        public ITaskItem TestFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection containing the test arguments.
        /// </summary>
        public ITaskItem[] TestArguments
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
