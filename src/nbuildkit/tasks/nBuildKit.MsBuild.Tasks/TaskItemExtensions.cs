//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines extension methods for the <see cref="ITaskItem"/> instances.
    /// </summary>
    public static class TaskItemExtensions
    {
        /// <summary>
        /// Gets the path for the given <see cref="ITaskItem"/>.
        /// </summary>
        /// <param name="item">The task item.</param>
        /// <returns>The path for the task item.</returns>
        public static string ToPath(this ITaskItem item)
        {
            return item?.ItemSpec;
        }
    }
}
