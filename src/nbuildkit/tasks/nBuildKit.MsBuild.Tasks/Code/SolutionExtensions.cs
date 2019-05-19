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
using Microsoft.Build.Evaluation;
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
        /// <returns>A collection containing the full paths to the projects in the solution.</returns>
        public static IEnumerable<ITaskItem> GetProjects(string path)
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

            var parseSolutionMethod = solutionParserType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static);
            dynamic solutionFile = parseSolutionMethod.Invoke(null, new object[] { path });

            var projects = (IReadOnlyList<dynamic>)solutionFile.ProjectsInOrder;
            return projects
                .Where(
                    p =>
                    {
                        var projectType = ((object)p.ProjectType).ToString();
                        return string.Equals(projectType, "KnownToBeMSBuildFormat", StringComparison.OrdinalIgnoreCase);
                    })
                .Select(
                    p =>
                    {
                        var projectRelativePath = (string)p.RelativePath;
                        var projectAbsolutePath = Path.Combine(Path.GetDirectoryName(path), projectRelativePath);
                        return new Project(projectAbsolutePath);
                    })
                .Select(
                    p =>
                    {
                        var item = new TaskItem(p.FullPath);
                        return item;
                    })
                .ToArray();
        }
    }
}
