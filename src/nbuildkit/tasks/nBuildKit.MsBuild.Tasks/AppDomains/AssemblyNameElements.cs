//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

namespace NBuildKit.MsBuild.Tasks.AppDomains
{
    /// <summary>
    /// Provides constants for use with AssemblyName objects.
    /// </summary>
    internal static class AssemblyNameElements
    {
        /// <summary>
        /// The key for the version number.
        /// </summary>
        public const string Version = "Version";

        /// <summary>
        /// The key for the culture.
        /// </summary>
        public const string Culture = "Culture";

        /// <summary>
        /// The key for the public key token.
        /// </summary>
        public const string PublicKeyToken = "PublicKeyToken";

        /// <summary>
        /// The separator used to separate the key and values in the AssemblyName.
        /// </summary>
        public const string KeyValueSeparator = "=";

        /// <summary>
        /// The string used for the invariant culture.
        /// </summary>
        public const string InvariantCulture = "neutral";

        /// <summary>
        /// The null string used for non-existent public key tokens.
        /// </summary>
        public const string NullString = "null";
    }
}
