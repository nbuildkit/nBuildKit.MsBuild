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
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes the executes a Powershell script from a file.
    /// </summary>
    public sealed class InvokePowershellFile : PowershellCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvokePowershellFile"/> class.
        /// </summary>
        public InvokePowershellFile()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokePowershellFile"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public InvokePowershellFile(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var scriptPath = GetAbsolutePath(Script);
            if (string.IsNullOrEmpty(scriptPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No script file was provided.");
                return false;
            }

            if (!File.Exists(scriptPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
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
