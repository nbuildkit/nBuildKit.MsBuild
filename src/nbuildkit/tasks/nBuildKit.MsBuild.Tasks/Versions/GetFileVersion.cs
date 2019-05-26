//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Versions
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts the version of a given file.
    /// </summary>
    public sealed class GetFileVersion : BaseTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            FileVersion = FileVersionInfo.GetVersionInfo(GetAbsolutePath(FilePath)).FileVersion.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Gets or sets the full path to the file for which the version should be determined.
        /// </summary>
        [Required]
        public ITaskItem FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the version of the file.
        /// </summary>
        [Output]
        public string FileVersion
        {
            get;
            set;
        }
    }
}
