//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Provides the error IDs known in nBuildKit and methods to map error IDs to error codes.
    /// </summary>
    public static class ErrorInformation
    {
        /// <summary>
        /// Defines the error ID for an error describing an application writing to the error stream.
        /// </summary>
        public const string ErrorIdApplicationErrorStream = "NBuildKit.Application.WroteToErrorStream";

        /// <summary>
        /// Defines the error ID for an error indicating that a required application argument has not been provided.
        /// </summary>
        public const string ErrorIdApplicationInvalidArgument = "NBuildKit.Application.InvalidArgument";

        /// <summary>
        /// Defines the error ID for an error indicating that a required application argument has not been provided.
        /// </summary>
        public const string ErrorIdApplicationMissingArgument = "NBuildKit.Application.MissingArgument";

        /// <summary>
        /// Defines the error ID for an error describing an application exiting with a non-zero exit code.
        /// </summary>
        public const string ErrorIdApplicationNonzeroExitCode = "NBuildKit.Application.NonzeroExitCode";

        /// <summary>
        /// Defines the error ID for an error indicating that the path to the selected tool was not found.
        /// </summary>
        public const string ErrorIdApplicationPathNotFound = "NBuildKit.FileNotFound.ExecutableTool";

        /// <summary>
        /// Defines the error ID for an error that occurs when a directory cannot be found.
        /// </summary>
        public const string ErrorIdDirectoryNotFound = "NBuildKit.DirectoryNotFound";

        /// <summary>
        /// Defines the error ID for an error that occurs when a file cannot be loaded.
        /// </summary>
        public const string ErrorIdFileLoad = "NBuildKit.FileLoad";

        /// <summary>
        /// Defines the error ID for an error that occurs when a file cannot be found.
        /// </summary>
        public const string ErrorIdFileNotFound = "NBuildKit.FileNotFound";

        /// <summary>
        /// Defines the error ID for an error that occurs when a file cannot be read.
        /// </summary>
        public const string ErrorIdFileRead = "NBuildKit.FileRead";

        /// <summary>
        /// Defines the error ID for an error that occurs when a file hash cannot be calculated.
        /// </summary>
        public const string ErrorIdHashCalculationFailure = "nBuildKit.CalculateHash.Failure";

        private const string MetadataCodeTag = "Code";

        /// <summary>
        /// Returns the error code for the given ID.
        /// </summary>
        /// <param name="errorId">The error ID.</param>
        /// <param name="errorInformation">The collection containing the error information.</param>
        /// <returns>The error code that matches the given ID.</returns>
        public static string ErrorCodeById(string errorId, ITaskItem[] errorInformation)
        {
            var result = string.Empty;
            if (errorInformation != null)
            {
                var code = errorInformation.Where(t => string.Equals(errorId, t.ItemSpec, StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.GetMetadata(MetadataCodeTag))
                    .FirstOrDefault();
                result = code ?? string.Empty;
            }

            return result;
        }
    }
}
