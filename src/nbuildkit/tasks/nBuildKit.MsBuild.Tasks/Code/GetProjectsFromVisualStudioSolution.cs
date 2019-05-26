//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts the projects from a given Visual Studio solution file.
    /// </summary>
    public sealed class GetProjectsFromVisualStudioSolution : BaseTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var solutionPath = GetAbsolutePath(SolutionFile);
            if (string.IsNullOrEmpty(solutionPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No solution file path provided.");
                return false;
            }

            if (!File.Exists(solutionPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Expected the solution to be at: '{0}' but no such file exists",
                    solutionPath);
                return false;
            }

            Projects = SolutionExtensions.GetProjects(solutionPath).ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of projects for the given solution file.
        /// </summary>
        [Output]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Projects
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the solution file.
        /// </summary>
        [Required]
        public ITaskItem SolutionFile
        {
            get;
            set;
        }
    }
}
