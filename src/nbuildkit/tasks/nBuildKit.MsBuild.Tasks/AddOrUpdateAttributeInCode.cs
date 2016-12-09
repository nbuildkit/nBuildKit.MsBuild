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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="Task"/> that adds or updates an <see cref="Attribute"/> in a code file.
    /// </summary>
    public sealed class AddOrUpdateAttributeInCode : Task
    {
        /// <summary>
        /// Gets or sets the name of the attribute that should be updated.
        /// </summary>
        [Required]
        public string AttributeName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the encoding for the text.
        /// </summary>
        public string Encoding
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (string.IsNullOrEmpty(InputFile.ItemSpec))
            {
                Log.LogError("No input file provided");
                return false;
            }

            var filePath = Path.GetFullPath(InputFile.ItemSpec);
            if (!File.Exists(filePath))
            {
                Log.LogError("Input File '{0}' cannot be found", InputFile);
                return false;
            }

            var ext = Path.GetExtension(filePath).TrimStart('.');

            var attribute = string.Empty;
            var assemblyAttributeMatcher = "UNDEFINED";
            switch (ext)
            {
                case "cs":
                    attribute = string.Format("[assembly: {0}({1})]", AttributeName, Value);
                    assemblyAttributeMatcher = string.Format("(^\\s*\\[assembly:\\s*{0})(.*$)", AttributeName);
                    break;
                case "vb":
                    attribute = string.Format("<Assembly: {0}({1})>", AttributeName, Value);
                    assemblyAttributeMatcher = string.Format("(^\\s*<Assembly:\\s*{0})(.*$)", AttributeName);
                    break;
            }

            var lines = new List<string>();
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            var found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i];

                if (System.Text.RegularExpressions.Regex.IsMatch(text, assemblyAttributeMatcher))
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Replacing in file: {0}. Old line \"{1}\". New line: \"{2}\"",
                            filePath,
                            lines[i],
                            attribute));
                    lines[i] = attribute;

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Adding to file: {0}. Line: \"{1}\"",
                        filePath,
                        attribute));
                lines.Add(attribute);
            }

            var encoding = System.Text.Encoding.ASCII;
            if (!string.IsNullOrWhiteSpace(Encoding))
            {
                encoding = System.Text.Encoding.GetEncoding(Encoding);
            }

            using (var writer = new StreamWriter(filePath, false, encoding))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    writer.WriteLine(lines[i]);
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the file path information for the input file.
        /// </summary>
        [Required]
        public ITaskItem InputFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value to which the attribute whould be updated.
        /// </summary>
        [Required]
        public string Value
        {
            get;
            set;
        }
    }
}
