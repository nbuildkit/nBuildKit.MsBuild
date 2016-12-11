//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs invokes the ReportGenerator tool.
    /// </summary>
    public sealed class ReportGenerator : CommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            if ((OpenCoverOutputFiles == null) || (OpenCoverOutputFiles.Length == 0))
            {
                Log.LogError("At least one open cover output file has to be specified");
                return false;
            }

            var arguments = new List<string>();
            {
                var reportFileBuilder = new StringBuilder();
                foreach (var token in OpenCoverOutputFiles)
                {
                    var filePath = token.ToString();
                    if (!File.Exists(filePath))
                    {
                        Log.LogMessage(MessageImportance.High, "File does not exist: {0}", filePath);
                        continue;
                    }

                    if (reportFileBuilder.Length > 0)
                    {
                        reportFileBuilder.Append(";");
                    }

                    reportFileBuilder.Append(string.Format(CultureInfo.InvariantCulture, "\"{0}\"", filePath.TrimEnd('\\')));
                }

                if (reportFileBuilder.Length == 0)
                {
                    Log.LogError("At least one valid open cover output file has to be specified");
                    return false;
                }

                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-reports:{0} ", reportFileBuilder.ToString()));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-reporttypes:Html;HtmlSummary;XmlSummary;TextSummary;Badges "));

                // Make sure we remove the back-slash because if we don't then
                // the closing quote will be eaten by the command line parser. Note that
                // this is only necessary because we're dealing with a directory
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-targetdir:\"{0}\" ", GetAbsolutePath(OutputDirectory).TrimEnd('\\')));
            }

            var exitCode = InvokeCommandLineTool(
                ReportGeneratorExe,
                arguments);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exited with a non-zero exit code. Exit code was: {1}",
                        Path.GetFileName(GetFullToolPath(ReportGeneratorExe)),
                        exitCode));
                return false;
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of OpenCover files that should be transformed.
        /// </summary>
        [Required]
        public ITaskItem[] OpenCoverOutputFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory into which the reports should be placed.
        /// </summary>
        [Required]
        public ITaskItem OutputDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the ReportGenerator command line executable.
        /// </summary>
        [Required]
        public ITaskItem ReportGeneratorExe
        {
            get;
            set;
        }
    }
}
