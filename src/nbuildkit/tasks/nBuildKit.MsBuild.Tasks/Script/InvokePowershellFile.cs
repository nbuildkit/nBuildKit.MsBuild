//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the executes a Powershell script from a file.
    /// </summary>
    public sealed class InvokePowershellFile : PowershellCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var scriptPath = GetAbsolutePath(Script);
            if (string.IsNullOrEmpty(scriptPath))
            {
                Log.LogError("No script file was provided.");
                return false;
            }

            if (!File.Exists(scriptPath))
            {
                Log.LogError(
                    "Expected to find the Powershell script at {0} but it was not found.",
                    scriptPath);
                return false;
            }

            var text = new StringBuilder();
            DataReceivedEventHandler standardOutputHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    text.Append(e.Data);
                    Log.LogMessage(e.Data);
                }
            };

            InvokePowershellFile(scriptPath, standardOutputHandler: standardOutputHandler);

            Output = text.ToString();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the output text from the powershell execution.
        /// </summary>
        [Output]
        public string Output
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the script that should be invoked.
        /// </summary>
        [Required]
        public ITaskItem Script
        {
            get;
            set;
        }
    }
}
