//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> which validates that the cryptographic hash of a file matches the
    /// hash provided.
    /// </summary>
    public sealed class ValidateHash : FileHashTask
    {
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
                    "The file was expected to be found at '{0}' but that path does not exist. Unable to calculate the hash of a non-existent file.",
                    filePath);
                return false;
            }

            var hashValue = ComputeHash(filePath);
            if (hashValue == null)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdHashCalculationFailure),
                    Core.ErrorInformation.ErrorIdHashCalculationFailure,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Failed to calculate a hash for the file at '{0}' with the specified hash algorithm of '{0}'.",
                    filePath,
                    Algorithm);
                return false;
            }

            var hash = ConvertHashToString(hashValue);

            var expectedHash = Hash;
            if ((HashFile != null) && !string.IsNullOrWhiteSpace(HashFile.ItemSpec))
            {
                var hashFilePath = GetAbsolutePath(HashFile);
                var fileName = System.IO.Path.GetFileName(filePath).ToLowerInvariant();
                var lineWithHash = File.ReadLines(hashFilePath)
                    .Where(l => l.ToLowerInvariant().Contains(fileName))
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(lineWithHash))
                {
                    Log.LogError(
                        "Could not find hash for {0} in {1}",
                        fileName,
                        HashFile);
                    return false;
                }

                expectedHash = lineWithHash.Substring(0, lineWithHash.IndexOf(" ", StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(expectedHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError(
                    "The provided hash {0} did not match the calculated hash [{1}]",
                    expectedHash,
                    hash);
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the hexidecimal representation of the expected hash value.
        /// </summary>
        public string Hash
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path of the file containing the expected hash. The file is expected to contain
        /// a line with the hash value followed by the name of the file for which the hash is computed.
        /// </summary>
        public ITaskItem HashFile
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
    }
}
