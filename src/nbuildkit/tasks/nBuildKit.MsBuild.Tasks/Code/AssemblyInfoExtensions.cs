// <copyright file="AssemblyInfoExtensions.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Properties;
using Nuclei.Diagnostics.Logging;
using ILogger = Nuclei.Diagnostics.Logging.ILogger;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines extension and helper method for dealing with AssemblyInfo files.
    /// </summary>
    public static class AssemblyInfoExtensions
    {
        private const string InternalsVisbleToAttributeName = "System.Runtime.CompilerServices.InternalsVisibleTo";

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
            ILogger log,
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
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }

            var found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i];

                if (System.Text.RegularExpressions.Regex.IsMatch(text, assemblyAttributeMatcher))
                {
                    log.Log(
                        new LogMessage(
                            LevelToLog.Debug,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Replacing in file: {0}. Old line \"{1}\". New line: \"{2}\"",
                                filePath,
                                lines[i],
                                attribute)));
                    lines[i] = attribute;

                    found = true;
                    break;
                }
            }

            if (!found && addIfNotFound)
            {
                log.Log(
                    new LogMessage(
                        LevelToLog.Debug,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Adding to file: {0}. Line: \"{1}\"",
                            filePath,
                            attribute)));
                lines.Add(attribute);
            }

            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            log.Log(
                new LogMessage(
                    LevelToLog.Debug,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "File at: {0}. Exists: \"{1}\"",
                        filePath,
                        File.Exists(filePath))));
            using (var writer = new StreamWriter(filePath, false, encoding ?? Encoding.Default))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }
        }

        /// <summary>
        /// Adds one or more <see cref="InternalsVisibleToAttribute"/> instances to the AssemblyInfo file.
        /// </summary>
        /// <param name="filePath">The path to the AssemblyInfo file.</param>
        /// <param name="compilerDirectives">The compiler directives that indicate when the InternalsVisibleToAttributes should be enabled.</param>
        /// <param name="internalsVisibleToAttributeParameters">
        ///     The collection that contains the assembly names and optional public keys for which the InternalsVisibleTo attributes
        ///     should be generated.
        /// </param>
        /// <param name="encoding">The text encoding.</param>
        /// <param name="log">The object used to write information to the log.</param>
        public static void UpdateInternalsVisibleToAttributes(
            string filePath,
            string compilerDirectives,
            IEnumerable<Tuple<string, string>> internalsVisibleToAttributeParameters,
            Encoding encoding,
            ILogger log)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString, nameof(filePath));
            }

            if (internalsVisibleToAttributeParameters is null)
            {
                throw new ArgumentNullException(nameof(internalsVisibleToAttributeParameters));
            }

            if (internalsVisibleToAttributeParameters.Any())
            {
                return;
            }

            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var ext = Path.GetExtension(filePath).TrimStart('.');

            var assemblyAttributeMatcher = "UNDEFINED";
            switch (ext)
            {
                case "cs":
                    assemblyAttributeMatcher = AttributeMatcherForCSharp(InternalsVisbleToAttributeName);
                    break;
                case "vb":
                    assemblyAttributeMatcher = AttributeMatcherForVb(InternalsVisbleToAttributeName);
                    break;
            }

            var lines = new List<string>();
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }

            var shouldContinue = RemoveInternalsVisibleToAttributes(lines, assemblyAttributeMatcher, log);
            if (!shouldContinue)
            {
                return;
            }

            // Add the new attribute lines
            switch (ext)
            {
                case "cs":
                    AddInternalsVisibleToAttributesForCSharp(lines, compilerDirectives, internalsVisibleToAttributeParameters);
                    break;
                case "vb":
                    AddInternalsVisibleToAttributesForVb(lines, compilerDirectives, internalsVisibleToAttributeParameters);
                    break;
            }

            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }

            log.Log(
                new LogMessage(
                    LevelToLog.Debug,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "File at: {0}. Exists: \"{1}\"",
                        filePath,
                        File.Exists(filePath))));
            using (var writer = new StreamWriter(filePath, false, encoding ?? Encoding.Default))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }
        }

        private static void AddInternalsVisibleToAttributesForCSharp(
            List<string> lines,
            string compilerDirectives,
            IEnumerable<Tuple<string, string>> attributeValues)
        {
            if (!string.IsNullOrWhiteSpace(compilerDirectives))
            {
                lines.Add(CompilerStartDirectiveForCSharp(compilerDirectives));
            }

            foreach (var internalsVisibleTo in attributeValues)
            {
                var attributeText = string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}{1}\"",
                    internalsVisibleTo.Item1,
                    !string.IsNullOrWhiteSpace(internalsVisibleTo.Item2)
                        ? string.Format(
                            CultureInfo.InvariantCulture,
                            ", PublicKey={0}",
                            internalsVisibleTo.Item2)
                        : string.Empty);
                var attribute = AttributeTextForCSharp(
                    InternalsVisbleToAttributeName,
                    attributeText);
            }

            if (!string.IsNullOrWhiteSpace(compilerDirectives))
            {
                lines.Add(CompilerEndDirectiveForCSharp());
            }
        }

        private static void AddInternalsVisibleToAttributesForVb(
            List<string> lines,
            string compilerDirectives,
            IEnumerable<Tuple<string, string>> attributeValues)
        {
            if (!string.IsNullOrWhiteSpace(compilerDirectives))
            {
                lines.Add(CompilerStartDirectiveForVb(compilerDirectives));
            }

            foreach (var internalsVisibleTo in attributeValues)
            {
                var attributeText = string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}{1}\"",
                    internalsVisibleTo.Item1,
                    !string.IsNullOrWhiteSpace(internalsVisibleTo.Item2)
                        ? string.Format(
                            CultureInfo.InvariantCulture,
                            ", PublicKey={0}",
                            internalsVisibleTo.Item2)
                        : string.Empty);
                var attribute = AttributeTextForVb(
                    InternalsVisbleToAttributeName,
                    attributeText);
            }

            if (!string.IsNullOrWhiteSpace(compilerDirectives))
            {
                lines.Add(CompilerEndDirectiveForVb());
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

        private static string CompilerEndDirectiveForCSharp()
        {
            return "#endif";
        }

        private static string CompilerEndDirectiveForVb()
        {
            return "#End If";
        }

        private static string CompilerStartDirectiveForCSharp(string condition)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "#if {0}",
                condition);
        }

        private static string CompilerStartDirectiveForVb(string condition)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "#If {0}",
                condition);
        }

        private static bool RemoveInternalsVisibleToAttributes(List<string> lines, string assemblyAttributeMatcher, ILogger log)
        {
            var first = -1;
            var last = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i];

                if (System.Text.RegularExpressions.Regex.IsMatch(text, assemblyAttributeMatcher))
                {
                    if (first == -1)
                    {
                        first = i;
                    }

                    // If the previous line isn't an InternalsVisibleToAttribute, then we're in trouble
                    if ((last > -1) && (last < (i - 1)))
                    {
                        log.Log(
                            new LogMessage(
                                LevelToLog.Error,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Found multiple InternalsVisibleTo attributes on non-neighbouring lines." +
                                        " This will cause failures as all the lines should be replaced in one operation." +
                                        " Attributes starting at line {0}.",
                                    first)));
                        return false;
                    }

                    if (last < i)
                    {
                        last = i;
                    }
                }
            }

            // Figure out if first-1 and last+1 contain the pre-processor directives
            if ((first - 1) > -1)
            {
                if (lines[first - 1].StartsWith("#if", StringComparison.OrdinalIgnoreCase))
                {
                    // Compiler directive. Assume it's ours and include it in the list of lines to
                    // be nuked
                    first -= 1;
                }
            }

            if ((last + 1) < lines.Count)
            {
                if (lines[last + 1].StartsWith("#end", StringComparison.OrdinalIgnoreCase))
                {
                    // Compiler directive. Assume it's ours and include it in the list of lines to
                    // be nuked
                    last += 1;
                }
            }

            // Delete the attribute lines
            for (int i = last; i >= first; i--)
            {
                lines.RemoveAt(i);
            }

            return true;
        }
    }
}
