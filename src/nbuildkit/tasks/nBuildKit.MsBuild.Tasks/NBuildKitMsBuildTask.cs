//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defins the base class for implementations of an MsBuild task.
    /// </summary>
    public abstract class NBuildKitMsBuildTask : Task
    {
        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The absolute path.</returns>
        protected string GetAbsolutePath(ITaskItem path)
        {
            return GetAbsolutePath(path.ToPath());
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The absolute path.</returns>
        protected string GetAbsolutePath(string path)
        {
            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
                    "Searching for full path of {0}",
                    path));

            var result = path;
            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }

            if (!Path.IsPathRooted(result))
            {
                result = Path.GetFullPath(result);
            }

            return result;
        }
    }
}
