//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the executes a Powershell command.
    /// </summary>
    public sealed class InvokePowershellCommand : PowershellCommandLineToolTask
    {
        /// <summary>
        /// Gets or sets the command that should be executed.
        /// </summary>
        [Required]
        public string Command
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (string.IsNullOrEmpty(Command))
            {
                Log.LogError("No command was provided.");
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

            InvokePowershellCommand(Command, standardOutputHandler: standardOutputHandler);

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
    }
}
