//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.IO.Compression;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Packaging
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts a ZIP archive to a given directory.
    /// </summary>
    public sealed class Unzip : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the full path to the directory into which the ZIP archive should be expanded.
        /// </summary>
        [Required]
        public ITaskItem DestinationDirectory
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Low, "Uncompressing archive in: " + DestinationDirectory);
            ZipFile.ExtractToDirectory(GetAbsolutePath(InputFileName), GetAbsolutePath(DestinationDirectory));

            return true;
        }

        /// <summary>
        /// Gets or sets the full path to the ZIP archive.
        /// </summary>
        [Required]
        public ITaskItem InputFileName
        {
            get;
            set;
        }
    }
}
