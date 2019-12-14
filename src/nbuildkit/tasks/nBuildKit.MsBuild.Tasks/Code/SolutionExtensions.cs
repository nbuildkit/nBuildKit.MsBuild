// <copyright file="SolutionExtensions.cs" company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines utility methods for dealing with Visual Studio solutions.
    /// </summary>
    public static class SolutionExtensions
    {
        /// <summary>
        /// Gets the paths for the project files in the solution.
        /// </summary>
        /// <param name="path">The full path to the solution file.</param>
        /// <param name="log">The MsBuild logger.</param>
        /// <returns>A collection containing the full paths to the projects in the solution.</returns>
        public static IEnumerable<ITaskItem> GetProjects(string path, Core.ILogger log = null)
        {
            // The loaded version of Microsoft.Build might not be the version we're compiled against so we need to
            // find the correct assembly first
            var microsoftBuildAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "Microsoft.Build", StringComparison.Ordinal));

            var solutionParserType = Type.GetType(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Microsoft.Build.Construction.SolutionFile, {0}",
                    microsoftBuildAssembly.FullName),
                true,
                false);

            if (solutionParserType == null)
            {
                throw new InvalidOperationException("Can not find solution file type.");
            }

            log?.LogMessage(
                MessageImportance.Low,
                "Using the MsBuild solution parser from '{0}' with type '{1}'",
                microsoftBuildAssembly.FullName,
                solutionParserType);

            var parseSolutionMethod = solutionParserType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
            dynamic solutionFile = parseSolutionMethod.Invoke(null, new object[] { path });

            var projects = (IReadOnlyList<ProjectInSolution>)solutionFile.ProjectsInOrder;
            log?.LogMessage(
                MessageImportance.Low,
                "Found {0} projects. Project paths:",
                projects.Count);
            if (log != null)
            {
                foreach (var project in projects)
                {
                    log?.LogMessage(
                        MessageImportance.Low,
                        (string)project.RelativePath);
                }
            }

            return projects
                .Where(
                    p =>
                    {
                        var projectType = ((object)p.ProjectType).ToString();
                        var result = string.Equals(projectType, "KnownToBeMSBuildFormat", StringComparison.OrdinalIgnoreCase);

                        log?.LogMessage(
                            MessageImportance.Low,
                            result
                                ? "Project {0} is an MsBuild project"
                                : "Project {0} is not an MsBuild project",
                            (string)p.RelativePath);

                        return result;
                    })
                .Select(
                    p =>
                    {
                        var projectRelativePath = (string)p.RelativePath;
                        var projectAbsolutePath = Path.Combine(Path.GetDirectoryName(path), projectRelativePath);

                        log?.LogMessage(
                            MessageImportance.Low,
                            "Project {0} has absolute path {1}",
                            projectRelativePath,
                            projectAbsolutePath);

                        return projectAbsolutePath;
                    })
                .Select(
                    p =>
                    {
                        var item = new TaskItem(p);
                        return item;
                    })
                .ToArray();
        }
    }
}
