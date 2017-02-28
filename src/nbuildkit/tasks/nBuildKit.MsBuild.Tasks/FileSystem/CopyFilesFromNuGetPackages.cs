//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;
using NBuildKit.MsBuild.Tasks.Core.FileSystem;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> which copies files from one or more NuGet packages.
    /// </summary>
    public sealed class CopyFilesFromNuGetPackages : BaseTask
    {
        private const string DestinationMetadataName = "Destinations";
        private const string IncludeMetadataName = "Include";

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyFilesFromNuGetPackages"/> class.
        /// </summary>
        public CopyFilesFromNuGetPackages()
            : this(new System.IO.Abstractions.FileSystem())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyFilesFromNuGetPackages"/> class.
        /// </summary>
        /// <param name="fileSystem">The object that provides access to the file system.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="fileSystem"/> is <see langword="null" />.
        /// </exception>
        public CopyFilesFromNuGetPackages(IFileSystem fileSystem)
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
            var packageDirectory = GetAbsolutePath(PackagesDirectory);

            foreach (var item in Items)
            {
                var package = item.ItemSpec;
                Log.LogMessage(
                    MessageImportance.Low,
                    "Copying files from package {0}",
                    package);

                Action<MessageImportance, string> logger = (importance, message) => Log.LogMessage(importance, message);
                var nugetPackagePath = NugetHelpers.HighestPackageVersionDirectoryFor(
                    package,
                    packageDirectory,
                    _fileSystem,
                    logger);

                Log.LogMessage(
                    MessageImportance.Low,
                    "Package {0} was found at: {1}",
                    package,
                    nugetPackagePath);

                var includes = item.GetMetadata(IncludeMetadataName)
                    .Split(';')
                    .SelectMany(
                        e =>
                        {
                            var expression = _fileSystem.Path.Combine(nugetPackagePath, e);
                            var baseDirectory = PathUtilities.BaseDirectory(expression);
                            return PathUtilities.IncludedPaths(e, nugetPackagePath)
                                .Select(p => Tuple.Create(p, PathUtilities.GetFilePathRelativeToDirectory(p, baseDirectory)));
                        })
                    .ToList();

                Log.LogMessage(
                    MessageImportance.Low,
                    "Files to include: ");
                foreach (var path in includes)
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        path.Item1);
                }

                var destinations = item.GetMetadata(DestinationMetadataName)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                Log.LogMessage(
                    MessageImportance.Low,
                    "Destinations to copy to: ");
                foreach (var path in destinations)
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        path);
                }

                foreach (var destination in destinations)
                {
                    foreach (var pair in includes)
                    {
                        var source = pair.Item1;
                        var target = _fileSystem.Path.Combine(destination, pair.Item2);
                        Log.LogMessage(
                            MessageImportance.Low,
                            "Copying {0} to {1}",
                            source,
                            target);

                        var destinationDirectory = _fileSystem.Path.GetDirectoryName(target);
                        if (!_fileSystem.Directory.Exists(destinationDirectory))
                        {
                            _fileSystem.Directory.CreateDirectory(destinationDirectory);
                        }

                        _fileSystem.File.Copy(
                            source,
                            target);
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the items which describe what files need to be copied from which packages
        /// and where they should be dropped.
        /// </summary>
        [Required]
        public ITaskItem[] Items
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the packages directory that contains the NuGet packages.
        /// </summary>
        [Required]
        public ITaskItem PackagesDirectory
        {
            get;
            set;
        }
    }
}
