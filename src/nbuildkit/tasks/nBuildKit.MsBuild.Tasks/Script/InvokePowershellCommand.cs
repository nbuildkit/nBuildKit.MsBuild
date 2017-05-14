//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the executes a Powershell command.
    /// </summary>
    public sealed class InvokePowershellCommand : PowershellCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvokePowershellCommand"/> class.
        /// </summary>
        public InvokePowershellCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokePowershellCommand"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public InvokePowershellCommand(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

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
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdApplicationMissingArgument),
                    ErrorIdApplicationMissingArgument,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No command was provided.");
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
