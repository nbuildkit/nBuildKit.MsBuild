//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

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
    /// Defines a <see cref="ITask"/> that extracts the projects from a given Visual Studio solution file.
    /// </summary>
    public sealed class GetProjectsFromVisualStudioSolution : NBuildKitMsBuildTask
    {
        private static IEnumerable<ITaskItem> GetProjects(string path)
        {
            // The loaded version of Microsoft.Build might not be the version we're compiled against so we need to
            // find the correct assembly first
            var microsoftBuildAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "Microsoft.Build", StringComparison.Ordinal));

            // Until VS2015 (i.e. MsBuild 14.0) the Microsoft.Build.Construction.SolutionFile class is internal (eventhough the
            // documentation says it is available in VS2013 (i.e. MsBuild 12.0)). Upgrading everything to support VS2015 up only
            // seems a bit much so we go the nasty route of reflection ....
            if (microsoftBuildAssembly.GetName().Version.Major <= 12)
            {
                return GetProjectsViaMsBuildV12OrOlder(path, microsoftBuildAssembly);
            }
            else
            {
                return GetProjectsViaMsBuildV14OrNewer(path, microsoftBuildAssembly);
            }
        }

        private static IEnumerable<ITaskItem> GetProjectsViaMsBuildV12OrOlder(string path, Assembly microsoftBuildAssembly)
        {
            var solutionParserType = Type.GetType(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Microsoft.Build.Construction.SolutionParser, {0}",
                    microsoftBuildAssembly.FullName),
                true,
                false);

            if (solutionParserType == null)
            {
                throw new InvalidOperationException("Can not find solution parser type.");
            }

            var solutionReaderProperty = solutionParserType.GetProperty("SolutionReader", BindingFlags.NonPublic | BindingFlags.Instance);
            var projectsProperty = solutionParserType.GetProperty("Projects", BindingFlags.NonPublic | BindingFlags.Instance);
            var parseSolutionMethod = solutionParserType.GetMethod("ParseSolution", BindingFlags.NonPublic | BindingFlags.Instance);

            dynamic solutionParser = solutionParserType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(null);
            using (var streamReader = new StreamReader(path))
            {
                solutionReaderProperty.SetValue(solutionParser, streamReader, null);
                parseSolutionMethod.Invoke(solutionParser, null);
            }

            var projects = new List<object>();

            var array = (Array)projectsProperty.GetValue(solutionParser, null);
            for (int i = 0; i < array.Length; i++)
            {
                projects.Add(array.GetValue(i));
            }

            var projectInSolutionType = Type.GetType(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Microsoft.Build.Construction.ProjectInSolution, {0}",
                    microsoftBuildAssembly.FullName),
                true,
                false);

            if (projectInSolutionType == null)
            {
                throw new InvalidOperationException("Can not find project type.");
            }

            var projectTypeProperty = projectInSolutionType.GetProperty("ProjectType", BindingFlags.NonPublic | BindingFlags.Instance);
            var relativePathProperty = projectInSolutionType.GetProperty("RelativePath", BindingFlags.NonPublic | BindingFlags.Instance);

            return projects
                .Where(
                    p =>
                    {
                        var projectType = projectTypeProperty.GetValue(p, null).ToString();
                        return string.Equals(projectType, "KnownToBeMSBuildFormat", StringComparison.OrdinalIgnoreCase);
                    })
                .Select(
                    p =>
                    {
                        var projectRelativePath = (string)relativePathProperty.GetValue(p, null);
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

        private static IEnumerable<ITaskItem> GetProjectsViaMsBuildV14OrNewer(string path, Assembly microsoftBuildAssembly)
        {
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

        /// <inheritdoc/>
        public override bool Execute()
        {
            var solutionPath = GetAbsolutePath(SolutionFile);
            if (string.IsNullOrEmpty(solutionPath))
            {
                Log.LogError("No solution file path provided.");
                return false;
            }

            if (!File.Exists(solutionPath))
            {
                Log.LogError(
                    "Expected the solution to be at: '{0}' but no such file exists",
                    solutionPath);
                return false;
            }

            Projects = GetProjects(solutionPath).ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of projects for the given solution file.
        /// </summary>
        [Output]
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
