//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;

namespace NBuildKit.MsBuild.Tasks.Core.FileSystem
{
    /// <summary>
    /// Provides utility methods for handling paths
    /// </summary>
    public static class PathUtilities
    {
        /// <summary>
        /// Appends the directory separator character at the end of the string if it doesn't already exist.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The path with the appended directory separator character.</returns>
        public static string AppendDirectorySeparatorChar(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            path = path.Trim();
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        /// <summary>
        /// Appends the directory separator character at the end of the string if it doesn't already exist.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <returns>The path with the appended directory separator character.</returns>
        public static string AppendDirectorySeparatorCharToDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            path = path.Trim();
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        /// <summary>
        /// Returns the base directory for a given path expression. The base directory is considered to be
        /// the directory of the file if there are no wild cards, or the directory path before the first
        /// set of wild cards.
        /// </summary>
        /// <param name="pathExpression">The path expression.</param>
        /// <returns>
        /// Returns the base directory for the expression.
        /// </returns>
        public static string BaseDirectory(string pathExpression)
        {
            if (string.IsNullOrWhiteSpace(pathExpression))
            {
                return string.Empty;
            }

            var pathSections = pathExpression.Split(new[] { "*" }, StringSplitOptions.None);
            if (pathSections.Length == 1)
            {
                // We got the whole thing back. Either the whole thing describes a file or a directory.
                // We don't really know. So ... eh ...
                return pathSections[0];
            }
            else
            {
                // We got some of it back. Trim it back to the next directory separator so that we don't have
                // half of a path section left, e.g. what we would get if somebody would pass: c:\temp\file*.*
                // or c:\temp\mydirectory*\*.*
                var result = pathSections[0];
                var indexOfLastSeparator = result.LastIndexOf('\\');
                if (indexOfLastSeparator > -1)
                {
                    result = result.Substring(0, indexOfLastSeparator);
                }

                return result.TrimEnd('\\');
            }
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The absolute path.</returns>
        public static string GetAbsolutePath(string path)
        {
            var result = path;
            if (string.IsNullOrWhiteSpace(result))
            {
                return string.Empty;
            }

            result = result.Trim();
            if (!Path.IsPathRooted(result))
            {
                result = Path.GetFullPath(result);
            }

            return result;
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="basePath">The full path to the base directory.</param>
        /// <returns>The absolute path.</returns>
        public static string GetAbsolutePath(string path, string basePath)
        {
            var result = path;
            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }

            if (!Path.IsPathRooted(result))
            {
                result = Path.GetFullPath(Path.Combine(basePath, result));
            }

            return result;
        }

        /// <summary>
        /// Creates a relative path from one directory to another.
        /// </summary>
        /// <remarks>
        /// Original code here: http://stackoverflow.com/a/275749/539846
        /// </remarks>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="fromPath"/> or <paramref name="toPath"/> is <c>null</c>.
        /// </exception>
        public static string GetRelativeDirectoryPath(string fromPath, string toPath)
        {
            if (string.IsNullOrWhiteSpace(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrWhiteSpace(toPath))
            {
                throw new ArgumentNullException("toPath");
            }

            // The Uri class treats paths that are directories but don't end in a directory separator as files.
            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath.Trim()));
            Uri toUri = new Uri(AppendDirectorySeparatorChar(toPath.Trim()));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = toUri.MakeRelativeUri(fromUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        /// <summary>
        /// Creates a relative path for a file based on a given directory.
        /// </summary>
        /// <param name="fromPath">Contains the file path that defines the start of the relative path.</param>
        /// <param name="directoryPath">The path of the base directory.</param>
        /// <returns>The relative path from the start file to the directory.</returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="fromPath"/> or <paramref name="directoryPath"/> is <c>null</c>.
        /// </exception>
        public static string GetFilePathRelativeToDirectory(string fromPath, string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(fromPath))
            {
                throw new ArgumentNullException("fromPath");
            }

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentNullException("directoryPath");
            }

            fromPath = fromPath.Trim();
            var relativeDirectoryPath = GetRelativeDirectoryPath(
                AppendDirectorySeparatorCharToDirectory(Path.GetDirectoryName(fromPath)),
                AppendDirectorySeparatorCharToDirectory(directoryPath));
            return Path.Combine(
                relativeDirectoryPath,
                Path.GetFileName(fromPath));
        }

        /// <summary>
        /// Returns a collection containing all paths that match the path expression.
        /// </summary>
        /// <param name="pathExpression">The expression that describes the desired paths. May contain wild cards.</param>
        /// <param name="baseDirectory">The base directory relative to which the path expressions will be taken.</param>
        /// <returns>The collection of files that match the expression.</returns>
        public static IEnumerable<string> IncludedPaths(string pathExpression, string baseDirectory)
        {
            return IncludedPaths(pathExpression, Enumerable.Empty<string>(), baseDirectory);
        }

        /// <summary>
        /// Returns a collection containing all paths that match the path expression.
        /// </summary>
        /// <param name="pathExpression">The expression that describes the desired paths. May contain wild cards.</param>
        /// <param name="excludedPathExpressions">
        /// The expressions that describe the paths that should not be included in the final collection. Each expression may
        /// contain wild cards.
        /// </param>
        /// <param name="baseDirectory">The base directory relative to which the path expressions will be taken.</param>
        /// <returns>The collection of files that match the expression.</returns>
        public static IEnumerable<string> IncludedPaths(string pathExpression, IEnumerable<string> excludedPathExpressions, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(pathExpression))
            {
                return Enumerable.Empty<string>();
            }

            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            if (excludedPathExpressions != null)
            {
                matcher.AddExcludePatterns(
                    excludedPathExpressions.Select(
                        e =>
                        {
                            return Path.IsPathRooted(e) ? GetRelativeDirectoryPath(e, baseDirectory) : e;
                        }));
            }

            var relativeExpression = pathExpression;
            if (Path.IsPathRooted(pathExpression))
            {
                // If the path has a root drive then we assume the user gave us a full path. The matcher doesn't seem
                // to handle full paths very well so now we need to create a 'relative expression'.
                //
                // First grab the longest part of the expression that doesn't have any '*' characters in it
                var baseExpression = BaseDirectory(pathExpression);

                // Get the left overs of the expression with all the wild cards etc.
                var remainder = pathExpression.Substring(baseExpression.Length).TrimStart('\\');

                // Get the relative directory
                var relativeDirectory = GetRelativeDirectoryPath(baseExpression, baseDirectory);
                relativeExpression = Path.Combine(relativeDirectory, remainder).TrimStart('\\');
            }

            matcher.AddInclude(relativeExpression);
            return matcher.GetResultsInFullPath(baseDirectory);
        }
    }
}
