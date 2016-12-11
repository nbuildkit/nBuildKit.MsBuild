//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that searches a given directory for a specific tool and returns the full path to the directory containing the tool.
    /// </summary>
    public sealed class SearchPackagesDirectoryForToolDirectory : NBuildKitMsBuildTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            Path = Directory.EnumerateFiles(GetAbsolutePath(PackagesDir), FileToLocate, SearchOption.AllDirectories)
                .OrderBy(k => System.IO.Path.GetDirectoryName(k))
                .Select(k => new TaskItem(System.IO.Path.GetDirectoryName(k)))
                .LastOrDefault();

            return true;
        }

        /// <summary>
        /// Gets or sets the name of the file that should be located.
        /// </summary>
        [Required]
        public string FileToLocate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the packages directory.
        /// </summary>
        [Required]
        public ITaskItem PackagesDir
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
