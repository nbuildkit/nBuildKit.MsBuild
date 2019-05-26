//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> which validates that the GPG signature of a file matches the
    /// expected signature.
    /// </summary>
    public sealed class ValidateGpgSignature : CommandLineToolTask
    {
        private const string ErrorIdNoGpgKey = "NBuildKit.ValidateGpgSignature.NoGpgKey";
        private const string ErrorIdNoGpgServers = "NBuildKit.ValidateGpgSignature.NoGpgServers";
        private const string ErrorIdFailure = "NBuildKit.ValidateGpgSignature.Failure";

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateGpgSignature"/> class.
        /// </summary>
        public ValidateGpgSignature()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateGpgSignature"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public ValidateGpgSignature(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var filePath = GetAbsolutePath(Path);
            if (!File.Exists(filePath))
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
                    "The file was expected to be found at '{0}' but that path does not exist. Unable to verify the signature of a non-existent file.",
                    filePath);
                return false;
            }

            var signatureFilePath = GetAbsolutePath(SignatureFile);
            if (!File.Exists(signatureFilePath))
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
                    "The signature file was expected to be found at '{0}' but that path does not exist. Unable to verify the signature of a file without a signature file.",
                    signatureFilePath);
                return false;
            }

            if ((GpgKey == null) || string.IsNullOrWhiteSpace(GpgKey.ItemSpec))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoGpgKey),
                    ErrorIdNoGpgKey,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No GPG key was provided. Cannot verify the signature of the file at '{0}' without a GPG key.",
                    filePath);
                return false;
            }

            if ((KeyServers == null) || (KeyServers.Length == 0))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoGpgServers),
                    ErrorIdNoGpgServers,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No GPG key server addresses were provided. Cannot verify the signature of the file at '{0}' without GPG keys servers.",
                    filePath);
                return false;
            }

            var keyServers = KeyServers
                .Where(k => !string.IsNullOrWhiteSpace(k.ItemSpec))
                .Select(k => k.ItemSpec)
                .ToList();

            var exitCode = -1;
            foreach (var keyServer in keyServers)
            {
                var collectSignatureArguments = new List<string>();
                {
                    collectSignatureArguments.Add(string.Format(CultureInfo.InvariantCulture, "--keyserver \"{0}\" ", keyServer));
                    collectSignatureArguments.Add(string.Format(CultureInfo.InvariantCulture, "--recv-keys \"{0}\" ", GpgKey));
                }

                exitCode = InvokeGpg(collectSignatureArguments);
                if (exitCode == 0)
                {
                    break;
                }
                else
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        "Failed to get GPG signatures from {0}. Will attempt to use the next GPG key server ...",
                        keyServer);
                }
            }

            if (exitCode != 0)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdFailure),
                    ErrorIdFailure,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "The provided key servers [{0}] were not able to provide the GPG signature for the specified GPG key: {1}",
                    string.Join(",", keyServers),
                    GpgKey);
                return false;
            }

            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "--verify \"{0}\" ", SignatureFile));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "\"{0}\" ", filePath));
            }

            exitCode = InvokeGpg(arguments);

            if (exitCode != 0)
            {
                Log.LogError(
                    "The signature on the provided file {0} was not generated with the provided GPG key",
                    filePath,
                    GpgKey);
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the path to the GIT command line executable.
        /// </summary>
        [Required]
        public ITaskItem GpgExecutablePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the GPG keys for which the signature should be verified.
        /// </summary>
        [Required]
        public ITaskItem GpgKey
        {
            get;
            set;
        }

        /// <summary>
        /// Invokes the GPG command line tool with the given arguments in the provided workspace directory.
        /// </summary>
        /// <param name="arguments">The collection of arguments.</param>
        /// <param name="standardOutputHandler">
        ///     The event handler that handles the standard output stream of the command line application. If no value is provided
        ///     then all messages are logged.
        /// </param>
        /// <returns>The output of the GPG process.</returns>
        private int InvokeGpg(IEnumerable<string> arguments, DataReceivedEventHandler standardOutputHandler = null)
        {
            if (standardOutputHandler == null)
            {
                standardOutputHandler = (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };
            }

            DataReceivedEventHandler standardErrorHandler = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Log.LogWarning(e.Data);
                }
            };

            var exitCode = InvokeCommandLineTool(
                GpgExecutablePath,
                arguments,
                standardOutputHandler: standardOutputHandler,
                standardErrorHandler: standardErrorHandler);
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
                    System.IO.Path.GetFileName(GpgExecutablePath.ItemSpec),
                    exitCode);
            }

            return exitCode;
        }

        /// <summary>
        /// Gets or sets an array containing the URLs for the GPG key servers that should be used to verify the file signature.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] KeyServers
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the file for which the hash should be validated.
        /// </summary>
        [Required]
        public ITaskItem Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the file containing the expected signatures.
        /// </summary>
        [Required]
        public ITaskItem SignatureFile
        {
            get;
            set;
        }
    }
}
