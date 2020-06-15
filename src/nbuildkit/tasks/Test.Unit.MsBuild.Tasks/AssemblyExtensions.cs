//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines extension methods for <see cref="Assembly"/> objects.
    /// </summary>
    internal static class AssemblyExtensions
    {
        /// <summary>
        /// Returns the local directory path from where a specific <see cref="Assembly"/>
        /// was loaded.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>
        /// The local directory path from where the assembly was loaded.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="assembly"/> is <see langword="null" />.
        /// </exception>
        public static string LocalDirectoryPath(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            // Get the location of the assembly before it was shadow-copied
            // Note that Assembly.Codebase gets the path to the manifest-containing
            // file, not necessarily the path to the file that contains a
            // specific type.
            var uncPath = new Uri(assembly.CodeBase);

            // Get the local path. This may not work if the assembly isn't
            // local. For now we assume it is.
            return Path.GetDirectoryName(uncPath.LocalPath);
        }

        /// <summary>
        /// Returns the local file path from where a specific <see cref="Assembly"/>
        /// was loaded.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>
        /// The local file path from where the assembly was loaded.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="assembly"/> is <see langword="null" />.
        /// </exception>
        public static string LocalFilePath(this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            // Get the location of the assembly before it was shadow-copied
            // Note that Assembly.Codebase gets the path to the manifest-containing
            // file, not necessarily the path to the file that contains a
            // specific type.
            var uncPath = new Uri(assembly.CodeBase);

            // Get the local path. This may not work if the assembly isn't
            // local. For now we assume it is.
            return uncPath.LocalPath;
        }
    }
}
