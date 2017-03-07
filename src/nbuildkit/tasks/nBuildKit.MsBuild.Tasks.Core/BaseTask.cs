//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core.FileSystem;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the base class for implementations of an MsBuild task.
    /// </summary>
    public abstract class BaseTask : Task
    {
        /// <summary>
        /// Gets the verbosity that the current MsBuild instance is running at.
        /// </summary>
        /// <returns>The verbosity of the MsBuild logger.</returns>
        protected static string VerbosityForCurrentMsBuildInstance()
        {
            var regex = new Regex(
                @"(\/|:|;)(v|verbosity)(:|=)(\w*)",
                RegexOptions.IgnoreCase
                | RegexOptions.Multiline
                | RegexOptions.Compiled
                | RegexOptions.Singleline);

            var commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                var argument = commandLineArguments[i];

                var hasSwitch = false;
                var remainder = string.Empty;
                var verbosityMatch = regex.Match(argument);
                if (verbosityMatch.Success)
                {
                    hasSwitch = true;
                    remainder = verbosityMatch.Groups[4].Value;
                }

                if (hasSwitch)
                {
                    if (!string.IsNullOrEmpty(remainder))
                    {
                        return remainder;
                    }
                    else
                    {
                        if (commandLineArguments.Length > i + 1)
                        {
                            return commandLineArguments[i + 1].Trim();
                        }
                    }
                }
            }

            return "normal";
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>The absolute path.</returns>
        protected string GetAbsolutePath(ITaskItem path)
        {
            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Searching for absolute path of {0}",
                    path));

            return PathUtilities.GetAbsolutePath(path.ToPath());
        }

        /// <summary>
        /// Returns the absolute path for the given path item.
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="basePath">The full path to the base directory.</param>
        /// <returns>The absolute path.</returns>
        protected string GetAbsolutePath(ITaskItem path, ITaskItem basePath)
        {
            Log.LogMessage(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Searching for full path of {0}",
                    path));

            return PathUtilities.GetAbsolutePath(path?.ItemSpec, basePath?.ItemSpec);
        }
    }
}
