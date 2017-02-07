//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts the public key from a key file.
    /// </summary>
    public sealed class PublicKeySignatureFromKeyFile : CommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var snExeFileName = Path.GetFileName(GetAbsolutePath(SnExe));

            var publicKeyFile = Path.Combine(GetAbsolutePath(TemporaryDirectory), Path.GetRandomFileName());
            try
            {
                {
                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.InvariantCulture, "Extracting public key file from {0} ...", Input));
                    var arguments = new[] { string.Format(CultureInfo.InvariantCulture, "-p \"{0}\" \"{1}\"", Input, publicKeyFile.TrimEnd('\\')) };
                    var exitCode = InvokeCommandLineTool(
                        SnExe,
                        arguments);
                    if (exitCode != 0)
                    {
                        Log.LogError(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0} exited with a non-zero exit code while trying to extract the public key file from the complete key file. Exit code was: {1}",
                                Path.GetFileName(snExeFileName),
                                exitCode));
                        return false;
                    }
                }

                var text = new StringBuilder();
                {
                    Log.LogMessage(MessageImportance.Normal, "Extracting public key ...");
                    var arguments = new[] { string.Format(CultureInfo.InvariantCulture, "-tp \"{0}\"", publicKeyFile.TrimEnd('\\')) };
                    DataReceivedEventHandler standardOutputHandler = (s, e) =>
                    {
                        text.Append(e.Data);
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            Log.LogMessage(MessageImportance.Low, e.Data);
                        }
                    };
                    var exitCode = InvokeCommandLineTool(
                        SnExe,
                        arguments,
                        standardOutputHandler: standardOutputHandler);
                    if (exitCode != 0)
                    {
                        Log.LogError(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "{0} exited with a non-zero exit code while trying to extract the public key from the public key file. Exit code was: {1}",
                                Path.GetFileName(snExeFileName),
                                exitCode));
                        return false;
                    }
                }

                const string startString = "Public key (hash algorithm: sha1):";
                const string endString = "Public key token is";
                var publicKeyText = text.ToString();
                var startIndex = publicKeyText.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
                var endIndex = publicKeyText.IndexOf(endString, StringComparison.OrdinalIgnoreCase);
                PublicKey = publicKeyText.Substring(startIndex + startString.Length, endIndex - (startIndex + startString.Length));
            }
            finally
            {
                if (File.Exists(publicKeyFile))
                {
                    File.Delete(publicKeyFile);
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the key file.
        /// </summary>
        [Required]
        public ITaskItem Input
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        [Output]
        public string PublicKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the strong name tool (sn.exe).
        /// </summary>
        [Required]
        public ITaskItem SnExe
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to a directory into which temporary files may be created.
        /// </summary>
        [Required]
        public ITaskItem TemporaryDirectory
        {
            get;
            set;
        }
    }
}
