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

            var pathSections = pathExpression.Split(new[] { "**" }, StringSplitOptions.RemoveEmptyEntries);
            return pathSections.Length == 1 ? Path.GetDirectoryName(pathSections[0]) : pathSections[0].Trim('\\');
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

        private static IEnumerable<FileInfoBase> GetFilteredFilePaths(string baseDirectory, string fileFilter, bool recurse, IFileSystem fileSystem)
        {
            var dirInfo = fileSystem.DirectoryInfo.FromDirectoryName(baseDirectory);
            return dirInfo.EnumerateFiles(fileFilter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
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
            var relativeDirectoryPath = GetRelativeDirectoryPath(Path.GetDirectoryName(fromPath), directoryPath.Trim());
            return Path.Combine(
                relativeDirectoryPath,
                Path.GetFileName(fromPath));
        }

        /// <summary>
        /// Returns a collection containing all paths that match the path expression.
        /// </summary>
        /// <param name="pathExpression">The expression that describes the desired paths. May contain wild cards.</param>
        /// <param name="fileSystem">The object that abstracts the file system.</param>
        /// <returns>The collection of files that match the expression.</returns>
        public static IEnumerable<string> IncludedPaths(string pathExpression, IFileSystem fileSystem)
        {
            return IncludedPaths(pathExpression, Enumerable.Empty<string>(), fileSystem);
        }

        /// <summary>
        /// Returns a collection containing all paths that match the path expression.
        /// </summary>
        /// <param name="pathExpression">The expression that describes the desired paths. May contain wild cards.</param>
        /// <param name="excludedPathExpressions">
        /// The expressions that describe the paths that should not be included in the final collection. Each expression may
        /// contain wild cards.
        /// </param>
        /// <param name="fileSystem">The object that abstracts the file system.</param>
        /// <returns>The collection of files that match the expression.</returns>
        public static IEnumerable<string> IncludedPaths(string pathExpression, IEnumerable<string> excludedPathExpressions, IFileSystem fileSystem)
        {
            if (string.IsNullOrWhiteSpace(pathExpression))
            {
                return Enumerable.Empty<string>();
            }

            if (fileSystem == null)
            {
                fileSystem = new System.IO.Abstractions.FileSystem();
            }

            var excludedPaths = new List<string>();
            if (excludedPathExpressions != null)
            {
                foreach (var exclude in excludedPathExpressions)
                {
                    var excludedPathSections = exclude.Split(new[] { "**" }, StringSplitOptions.RemoveEmptyEntries);

                    var directoryToExclude = excludedPathSections[0].Trim('\\');
                    var fileToExcludeFilter = excludedPathSections.Length == 1 ? "*.*" : excludedPathSections[excludedPathSections.Length - 1].Trim('\\');
                    var filesToExclude = GetFilteredFilePaths(directoryToExclude, fileToExcludeFilter, true, fileSystem).Select(f => f.FullName);
                    excludedPaths.AddRange(filesToExclude);
                }
            }

            var pathSections = pathExpression.Split(new[] { "**" }, StringSplitOptions.RemoveEmptyEntries);

            var directory = pathSections.Length == 1 ? fileSystem.Path.GetDirectoryName(pathSections[0]) : pathSections[0].Trim('\\');
            var fileFilter = pathSections.Length == 1 ? fileSystem.Path.GetFileName(pathSections[0]) : pathSections[pathSections.Length - 1].Trim('\\');
            var recurse = pathSections.Length > 1;
            return GetFilteredFilePaths(directory, fileFilter, recurse, fileSystem)
                .Where(f => !excludedPaths.Contains(f.FullName))
                .Select(f => f.FullName);
        }
    }
}
