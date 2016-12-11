//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that converts a ReportGenerator output to a CSV file.
    /// </summary>
    public sealed class ReportGeneratorOutputToCsv : NBuildKitMsBuildTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var doc = XDocument.Load(GetAbsolutePath(InputFile));
            var metrics = (from node in doc
                              .Element("CoverageReport")
                              .Element("Assemblies")
                              .Descendants("Assembly")
                           select new
                           {
                               Name = node.Attribute("name").Value,
                               Coverage = node.Attribute("coverage").Value,
                           }).ToList();

            var builder = new StringBuilder();
            var line = new StringBuilder();
            foreach (var item in metrics)
            {
                if (line.Length > 0)
                {
                    line.Append(",");
                }

                line.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "\"{0}\"",
                        item.Name.TrimEnd('\\')));
            }

            builder.AppendLine(line.ToString());
            line = new StringBuilder();
            foreach (var item in metrics)
            {
                if (line.Length > 0)
                {
                    line.Append(",");
                }

                line.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}",
                        item.Coverage));
            }

            builder.AppendLine(line.ToString());
            using (var writer = new StreamWriter(GetAbsolutePath(OutputFile)))
            {
                writer.Write(builder.ToString());
            }

            return true;
        }

        /// <summary>
        /// Gets or sets the full path to the input file.
        /// </summary>
        [Required]
        public ITaskItem InputFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the output file.
        /// </summary>
        [Required]
        public ITaskItem OutputFile
        {
            get;
            set;
        }
    }
}
