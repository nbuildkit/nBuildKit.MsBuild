//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    internal static class NugetHelpers
    {
        internal static string HighestPackageVersionDirectoryFor(
            string packageName,
            string packagesDirectory,
            IFileSystem fileSystem,
            Action<MessageImportance, string> logger)
        {
            var packagesInfo = fileSystem.DirectoryInfo.FromDirectoryName(packagesDirectory);
            var potentialPaths = packagesInfo.EnumerateDirectories(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}.*",
                        packageName),
                    SearchOption.TopDirectoryOnly);
            logger(
                MessageImportance.Low,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Searching for {0} located the following potential directories: {1}",
                    packageName,
                    string.Join(", ", potentialPaths.Select(i => i.FullName))));

            string selectedPath = null;
            var selectedVersion = new Version();
            foreach (var path in potentialPaths)
            {
                var versionText = path.Name.Substring(packageName.Length).Trim('.').Trim();

                Version packageVersion;
                if (!Version.TryParse(versionText, out packageVersion))
                {
                    logger(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Path {0} is not a match for package {1}",
                            path.FullName,
                            packageName));

                    continue;
                }

                if (packageVersion > selectedVersion)
                {
                    logger(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Path {0} is a better match for package {1} than {2}",
                            path.FullName,
                            packageName,
                            selectedPath));

                    selectedVersion = packageVersion;
                    selectedPath = path.FullName;
                }
            }

            return selectedPath;
        }
    }
}
