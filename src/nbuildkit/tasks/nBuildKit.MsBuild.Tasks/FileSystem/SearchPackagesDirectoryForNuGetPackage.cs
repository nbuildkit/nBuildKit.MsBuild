//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO.Abstractions;
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
            Action<MessageImportance, string> logger = (importance, message) => Log.LogMessage(importance, message);
            var selectedPath = NugetHelpers.HighestPackageVersionDirectoryFor(
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
