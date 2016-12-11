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
using System.Text;
using Microsoft.Build.Framework;
using NuGet.Versioning;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that calculates the dependencies for a NuGet package specification.
    /// </summary>
    public sealed class GatherNuGetDependenciesForProject : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the string containing the dependency XML for the nuspec.
        /// </summary>
        [Output]
        public string Dependencies
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection containing the names of the NuGet packages which are considered
        /// design time packages, and should thus not be referenced in the dependencies list.
        /// </summary>
        public ITaskItem[] DesignTimePackages
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var excludedDependencies = new List<string>();
            if (DesignTimePackages != null)
            {
                foreach (var token in DesignTimePackages)
                {
                    var packagePartialName = token.ToString().ToLowerInvariant();
                    excludedDependencies.Add(packagePartialName);
                }
            }

            var knownPackageFiles = new List<string>();

            // See if there is a packages.config file in the same directory as the nuspec file. Grab that too
            var localPackagesPath = Path.Combine(Path.GetDirectoryName(GetAbsolutePath(NuGetSpec)), "packages.config");
            if (File.Exists(localPackagesPath))
            {
                knownPackageFiles.Add(localPackagesPath);
            }

            if (Packages != null)
            {
                foreach (var token in Packages)
                {
                    var filePath = token.ToString();
                    if (!File.Exists(filePath))
                    {
                        Log.LogMessage(MessageImportance.High, "File does not exist: {0}", filePath);
                        continue;
                    }

                    if (!knownPackageFiles.Contains(filePath))
                    {
                        knownPackageFiles.Add(filePath);
                    }
                }
            }

            var knownDependencies = new List<string>();
            var builder = new StringBuilder();
            foreach (var packageFile in knownPackageFiles)
            {
                System.Xml.Linq.XDocument xDoc = null;
                try
                {
                    xDoc = System.Xml.Linq.XDocument.Load(packageFile);
                }
                catch (Exception)
                {
                    Log.LogError("Failed to load document {0}.", packageFile);
                    throw;
                }

                var packages = from package in xDoc.Element("packages").Descendants("package")
                               select new
                               {
                                   Id = package.Attribute("id").Value,
                                   Version = package.Attribute("version").Value,
                               };

                foreach (var package in packages)
                {
                    if (excludedDependencies.Any(p => package.Id.ToLowerInvariant().Contains(p)))
                    {
                        Log.LogMessage("Ignoring design time package: {0}", package.Id);
                        continue;
                    }

                    if (knownDependencies.Contains(package.Id))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(Environment.NewLine);
                    }

                    var packageVersion = new NuGetVersion(package.Version);
                    var versionRange = package.Version;
                    if (!string.IsNullOrEmpty(VersionRangeType) && !"none".Equals(VersionRangeType.ToLowerInvariant()))
                    {
                        switch (VersionRangeType.ToLowerInvariant())
                        {
                            case "major":
                                versionRange = string.Format(CultureInfo.InvariantCulture, "[{0}, {1})", package.Version, ((int)packageVersion.Major) + 1);
                                break;
                            case "minor":
                                versionRange = string.Format(CultureInfo.InvariantCulture, "[{0}, {1}.{2})", package.Version, packageVersion.Major, ((int)packageVersion.Minor) + 1);
                                break;
                            case "patch":
                                versionRange = string.Format(CultureInfo.InvariantCulture, "[{0}, {1}.{2}.{3})", package.Version, packageVersion.Major, packageVersion.Minor, ((int)packageVersion.Patch) + 1);
                                break;
                        }
                    }

                    builder.Append(string.Format(CultureInfo.InvariantCulture, "<dependency id='{0}' version='{1}' />", package.Id, versionRange));
                    knownDependencies.Add(package.Id);
                }
            }

            Dependencies = builder.ToString();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the nuspec file for which the dependencies should be calculated.
        /// </summary>
        [Required]
        public ITaskItem NuGetSpec
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of package.config files from which additional dependencies should be calculated.
        /// </summary>
        [Required]
        public ITaskItem[] Packages
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the switch indicating how the version range is calculated. Valid options are: 'none', 'major', 'minor', 'patch'.
        /// </summary>
        public string VersionRangeType
        {
            get;
            set;
        }
    }
}
