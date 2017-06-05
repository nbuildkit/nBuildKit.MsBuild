//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a base class for <see cref="ITask"/> implementations that calculate file hashes.
    /// </summary>
    public abstract class FileHashTask : BaseTask
    {
        /// <summary>
        /// Defines the error ID for an error that occurs when a file hash cannot be calculated.
        /// </summary>
        protected const string ErrorIdCalculationFailure = "nBuildKit.CalculateHash.Failure";

        /// <summary>
        /// Converts a byte array containing a hash to a hexadecimal string.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns>A string containing the hash in hexidecimal format.</returns>
        protected static string ConvertHashToString(byte[] hash)
        {
            if (hash == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.AppendFormat("{0:X2}", hash[i]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets or sets the algorithm that should be used to calculate the file hash.
        /// </summary>
        public string Algorithm
        {
            get;
            set;
        }

        /// <summary>
        /// Calculates the file has for the given file path with the selected algorithm.
        /// </summary>
        /// <param name="filePath">The full path to the file that should be hashed.</param>
        /// <returns>A byte array indicating the hash of the file.</returns>
        protected byte[] ComputeHash(string filePath)
        {
            var algorithm = Algorithm.ToUpperInvariant();
            using (var fileStream = System.IO.File.OpenRead(filePath))
            {
                // Be sure it's positioned to the beginning of the stream.
                fileStream.Position = 0;

                switch (algorithm)
                {
                    case "MD5":
                        // Compute the MD5 hash of the fileStream.
                        return MD5.Create().ComputeHash(fileStream);
                    case "SHA1":
                        // Compute the SHA1 hash of the fileStream.
                        return SHA1.Create().ComputeHash(fileStream);
                    case "SHA256":
                        // Compute the SHA256 hash of the fileStream.
                        return SHA256.Create().ComputeHash(fileStream);
                    case "SHA512":
                        // Compute the SHA512 hash of the fileStream.
                        return SHA512.Create().ComputeHash(fileStream);
                    default:
                        Log.LogError(
                            "The specified hash algorithm of '{0}' is not valid. Please select on of: MD5, SHA1, SHA256, SHA384 or SHA512.",
                            algorithm);
                        return null;
                }
            }
        }
    }
}
