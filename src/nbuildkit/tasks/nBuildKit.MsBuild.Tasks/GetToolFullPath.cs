//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that determines the full path of a given command line executable.
    /// </summary>
    public sealed class GetToolFullPath : CommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var path = GetFullToolPath(Tool);
            Path = new TaskItem(path);

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the tool.
        /// </summary>
        [Output]
        public ITaskItem Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the tool.
        /// </summary>
        [Required]
        public ITaskItem Tool
        {
            get;
            set;
        }
    }
}
