//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that calculates the hash of a given file.
    /// </summary>
    public sealed class CalculateFileHash : FileHashTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            if ((Path == null) || string.IsNullOrWhiteSpace(Path.ItemSpec))
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
                    "The file path is not defined. Unable to calculate the hash.");
                return false;
            }

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

            Hash = ConvertHashToString(hashValue);

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the hash of the given file.
        /// </summary>
        [Output]
        public string Hash
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the file from which the hash should be calculated.
        /// </summary>
        [Required]
        public ITaskItem Path
        {
            get;
            set;
        }
    }
}
