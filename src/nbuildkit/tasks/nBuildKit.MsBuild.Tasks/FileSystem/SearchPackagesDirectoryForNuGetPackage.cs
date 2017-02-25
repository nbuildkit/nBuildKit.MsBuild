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
            var packagesInfo = _fileSystem.DirectoryInfo.FromDirectoryName(GetAbsolutePath(PackagesDirectory));
            var potentialPaths = packagesInfo.EnumerateDirectories(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}.*",
                        PackageToLocate),
                    SearchOption.TopDirectoryOnly);
            Log.LogMessage(
                MessageImportance.Low,
                "Searching for {0} located the following potential directories: {1}",
                PackageToLocate,
                string.Join(", ", potentialPaths.Select(i => i.FullName)));

            string selectedPath = null;
            var selectedVersion = new Version();
            foreach (var path in potentialPaths)
            {
                var versionText = path.Name.Substring(PackageToLocate.Length).Trim('.').Trim();

                Version packageVersion;
                if (!Version.TryParse(versionText, out packageVersion))
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        "Path {0} is not a match for package {1}",
                        path.FullName,
                        PackageToLocate);

                    continue;
                }

                if (packageVersion > selectedVersion)
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        "Path {0} is a better match for package {1} than {2}",
                        path.FullName,
                        PackageToLocate,
                        selectedPath);

                    selectedVersion = packageVersion;
                    selectedPath = path.FullName;
                }
            }

            Path = (selectedPath != null) ? new TaskItem(selectedPath) : null;

            return true;
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
