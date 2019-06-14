//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines the contents for dealing with MsBuild loggers.
    /// </summary>
    internal static class MsBuildLog
    {
        /// <summary>
        /// Returns the MsBuild verbosity level as an enum value.
        /// </summary>
        /// <param name="verbosity">The verbosity as text.</param>
        /// <returns>The verbosity.</returns>
        public static Verbosity ToVerbosity(string verbosity)
        {
            if (string.Equals(verbosity, "q", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "quiet", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Quiet;
            }

            if (string.Equals(verbosity, "m", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "minimal", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Minimal;
            }

            if (string.Equals(verbosity, "n", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "normal", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Normal;
            }

            if (string.Equals(verbosity, "d", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "detailed", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Detailed;
            }

            if (string.Equals(verbosity, "diag", StringComparison.OrdinalIgnoreCase) || string.Equals(verbosity, "diagnostic", StringComparison.OrdinalIgnoreCase))
            {
                return Verbosity.Diagnostic;
            }

            // Unknown verbosity. Just assume normal
            return Verbosity.Normal;
        }

        /// <summary>
        /// Defines the verbosity levels for MsBuild.
        /// </summary>
        public enum Verbosity
        {
            Quiet = 0,
            Minimal = 1,
            Normal = 2,
            Detailed = 4,
            Diagnostic = 8,
        }
    }
}
