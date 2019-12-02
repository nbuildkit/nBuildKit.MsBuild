//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using NBuildKit.MsBuild.Tasks.Properties;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines utility methods for dealing with resources stored in the assembly.
    /// </summary>
    internal static class EmbeddedResourceExtracter
    {
        /// <summary>
        /// Extracts an embedded stream out of a given assembly.
        /// </summary>
        /// <param name="assembly">The assembly in which the embedded resource can be found.</param>
        /// <param name="filePath">The name of the file to extract.</param>
        /// <returns>A stream containing the file data.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="assembly"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="filePath"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="filePath"/> is an empty string.
        /// </exception>
        /// <exception cref="TemplateLoadException">
        /// Thrown if the embedded text file either could not be loaded or was empty.
        /// </exception>
        public static Stream LoadEmbeddedStream(Assembly assembly, string filePath)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(
                    Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString,
                    nameof(filePath));
            }

            Stream str;
            try
            {
                str = assembly.GetManifestResourceStream(filePath);
            }
            catch (FileNotFoundException e)
            {
                throw new TemplateLoadException(Resources.Exceptions_Messages_CouldNotLoadTemplate, e);
            }
            catch (FileLoadException e)
            {
                throw new TemplateLoadException(Resources.Exceptions_Messages_CouldNotLoadTemplate, e);
            }

            if (str == null)
            {
                throw new TemplateLoadException();
            }

            return str;
        }

        /// <summary>
        /// Extracts an embedded file out of a given assembly.
        /// </summary>
        /// <param name="assembly">The assembly in which the embedded resource can be found.</param>
        /// <param name="filePath">The name of the file to extract.</param>
        /// <returns>A string containing the file data.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="assembly"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="filePath"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="filePath"/> is an empty string.
        /// </exception>
        /// <exception cref="TemplateLoadException">
        /// Thrown if the embedded text file either could not be loaded or was empty.
        /// </exception>
        public static string LoadEmbeddedTextFile(Assembly assembly, string filePath)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(
                    Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString,
                    nameof(filePath));
            }

            Stream str;
            try
            {
                str = assembly.GetManifestResourceStream(filePath);
            }
            catch (FileNotFoundException e)
            {
                throw new TemplateLoadException(Resources.Exceptions_Messages_CouldNotLoadTemplate, e);
            }
            catch (FileLoadException e)
            {
                throw new TemplateLoadException(Resources.Exceptions_Messages_CouldNotLoadTemplate, e);
            }

            if (str == null)
            {
                throw new TemplateLoadException();
            }

            string result;
            using (var reader = new StreamReader(str))
            {
                result = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(result))
            {
                throw new TemplateLoadException();
            }

            return result;
        }
    }
}
