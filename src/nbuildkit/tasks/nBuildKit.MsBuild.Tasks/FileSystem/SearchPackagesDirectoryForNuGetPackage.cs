//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that searches a given directory for a specific NuGet package and returns the full path to the directory containing the package.
    /// </summary>
    public sealed class SearchPackagesDirectoryForNuGetPackage : BaseTask
    {
        internal static string HighestPackageVersionDirectoryFor(
            string packageName,
            string packagesDirectory,
            IFileSystem fileSystem,
            Action<MessageImportance, string> logger)
        {
            var packagesInfo = fileSystem.DirectoryInfo.FromDirectoryName(packagesDirectory);
            var potentialPaths = packagesInfo.EnumerateDirectories(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}.*",
                        packageName),
                    SearchOption.TopDirectoryOnly);
            logger(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Searching for {0} located the following potential directories: {1}",
                    packageName,
                    string.Join(", ", potentialPaths.Select(i => i.FullName))));

            string selectedPath = null;
            var selectedVersion = new Version();
            foreach (var path in potentialPaths)
            {
                var versionText = path.Name.Substring(packageName.Length).Trim('.').Trim();

                Version packageVersion;
                if (!Version.TryParse(versionText, out packageVersion))
                {
                    logger(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Path {0} is not a match for package {1}",
                            path.FullName,
                            packageName));

                    continue;
                }

                if (packageVersion > selectedVersion)
                {
                    logger(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Path {0} is a better match for package {1} than {2}",
                            path.FullName,
                            packageName,
                            selectedPath));

                    selectedVersion = packageVersion;
                    selectedPath = path.FullName;
                }
            }

            return selectedPath;
        }

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchPackagesDirectoryForNuGetPackage"/> class.
        /// </summary>
        public SearchPackagesDirectoryForNuGetPackage()
            : this(new System.IO.Abstractions.FileSystem())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchPackagesDirectoryForNuGetPackage"/> class.
        /// </summary>
        /// <param name="fileSystem">The object that provides access to the file system.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="fileSystem"/> is <see langword="null" />.
        /// </exception>
        public SearchPackagesDirectoryForNuGetPackage(IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            _fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            Action<MessageImportance, string> logger = (importance, message) => Log.LogMessage(importance, message);
            var selectedPath = HighestPackageVersionDirectoryFor(
                PackageToLocate,
                GetAbsolutePath(PackagesDirectory),
                _fileSystem,
                logger);
            Path = (selectedPath != null) ? new TaskItem(selectedPath) : null;

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the name of the package that should be located.
        /// </summary>
        [Required]
        public string PackageToLocate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the packages directory.
        /// </summary>
        [Required]
        public ITaskItem PackagesDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory containing the given tool.
        /// </summary>
        [Output]
        public ITaskItem Path
        {
            get;
            set;
        }
    }
}
