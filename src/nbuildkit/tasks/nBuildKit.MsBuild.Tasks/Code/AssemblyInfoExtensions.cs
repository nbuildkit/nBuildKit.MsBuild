// <copyright file="AssemblyInfoExtensions.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Properties;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines extension and helper method for dealing with AssemblyInfo files.
    /// </summary>
    public static class AssemblyInfoExtensions
    {
        /// <summary>
        /// Adds an attribute to the given AssemblyInfo file if it doesn't exist, otherwise updates it.
        /// </summary>
        /// <param name="filePath">The full path to the AssemblyInfo file.</param>
        /// <param name="attributeName">The name of the attribute that should be updated.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <param name="encoding">The encoding used for the AssemblyInfo file.</param>
        /// <param name="log">The function used to log messages.</param>
        /// <param name="addIfNotFound">A flag that indicates that the attribute should be added if it was not found.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="filePath"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="filePath"/> is an empty string.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="attributeName"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="attributeName"/> is an empty string.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="log"/> is <see langword="null" />.
        /// </exception>
        public static void UpdateAssemblyAttribute(
            string filePath,
            string attributeName,
            string value,
            Encoding encoding,
            Action<MessageImportance, string> log,
            bool addIfNotFound = false)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString, nameof(filePath));
            }

            if (attributeName is null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new ArgumentException(Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString, nameof(attributeName));
            }

            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var ext = Path.GetExtension(filePath).TrimStart('.');

            var attribute = string.Empty;
            var assemblyAttributeMatcher = "UNDEFINED";
            switch (ext)
            {
                case "cs":
                    attribute = AttributeTextForCSharp(attributeName, value);
                    assemblyAttributeMatcher = AttributeMatcherForCSharp(attributeName);
                    break;
                case "vb":
                    attribute = AttributeTextForVb(attributeName, value);
                    assemblyAttributeMatcher = AttributeMatcherForVb(attributeName);
                    break;
            }

            var lines = new List<string>();
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            var found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i];

                if (System.Text.RegularExpressions.Regex.IsMatch(text, assemblyAttributeMatcher))
                {
                    log(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Replacing in file: {0}. Old line \"{1}\". New line: \"{2}\"",
                            filePath,
                            lines[i],
                            attribute));
                    lines[i] = attribute;

                    found = true;
                    break;
                }
            }

            if (!found && addIfNotFound)
            {
                log(
                    MessageImportance.Low,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Adding to file: {0}. Line: \"{1}\"",
                        filePath,
                        attribute));
                lines.Add(attribute);
            }

            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            log(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "File at: {0}. Exists: \"{1}\"",
                    filePath,
                    File.Exists(filePath)));
            using (var writer = new StreamWriter(filePath, false, encoding ?? Encoding.Default))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }
        }

        private static string AttributeMatcherForCSharp(string attributeName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "(^\\s*\\[assembly:\\s*{0})(.*$)",
                attributeName);
        }

        private static string AttributeMatcherForVb(string attributeName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "(^\\s*<Assembly:\\s*{0})(.*$)",
                attributeName);
        }

        private static string AttributeTextForCSharp(string attributeName, string value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[assembly: {0}({1})]",
                attributeName,
                value ?? string.Empty);
        }

        private static string AttributeTextForVb(string attributeName, string value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "<Assembly: {0}({1})>",
                attributeName,
                value ?? string.Empty);
        }
    }
}
