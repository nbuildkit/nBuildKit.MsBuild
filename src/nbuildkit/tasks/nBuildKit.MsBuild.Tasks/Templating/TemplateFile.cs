//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a replacement of template variables in a given file.
    /// </summary>
    public sealed class TemplateFile : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the encoding of the file.
        /// </summary>
        public string Encoding
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataValueTag = "ReplacementValue";

            if (string.IsNullOrWhiteSpace(SearchExpression))
            {
                SearchExpression = "(?<token>\\$\\{(?<identifier>\\w*)\\})";
            }

            Log.LogMessage(MessageImportance.Low, "Searching for replacement tokens with the regular expression '{0}'", SearchExpression);

            var regex = new System.Text.RegularExpressions.Regex(
                SearchExpression,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Multiline
                | System.Text.RegularExpressions.RegexOptions.Compiled
                | System.Text.RegularExpressions.RegexOptions.Singleline);

            if (!File.Exists(GetAbsolutePath(Template)))
            {
                Log.LogError("Template File '{0}' cannot be found", Template);
            }
            else
            {
                var tokenPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (Tokens != null)
                {
                    ITaskItem[] processedTokens = Tokens;
                    for (int i = 0; i < processedTokens.Length; i++)
                    {
                        ITaskItem taskItem = processedTokens[i];
                        if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                        {
                            tokenPairs.Add(taskItem.ItemSpec, taskItem.GetMetadata(MetadataValueTag));
                        }
                    }
                }

                var templateLines = new List<string>();
                using (var reader = new StreamReader(GetAbsolutePath(Template)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        templateLines.Add(line);
                    }
                }

                var outputLines = new List<string>();
                for (int i = 0; i < templateLines.Count; i++)
                {
                    var line = templateLines[i];
                    var value = regex.Replace(
                        line,
                        m =>
                        {
                            var output = m.Value;
                            if (tokenPairs.ContainsKey(m.Groups[2].Value))
                            {
                                output = tokenPairs[m.Groups[2].Value];
                            }
                            return output;
                        });
                    outputLines.Add(value);
                }

                var encoding = System.Text.Encoding.ASCII;
                if (!string.IsNullOrWhiteSpace(Encoding))
                {
                    encoding = System.Text.Encoding.GetEncoding(Encoding);
                }

                using (var streamWriter = new StreamWriter(GetAbsolutePath(OutputFileName), false, encoding))
                {
                    for (int i = 0; i < outputLines.Count; i++)
                    {
                        streamWriter.WriteLine(outputLines[i]);
                    }

                    streamWriter.Flush();
                    Log.LogMessage(MessageImportance.Low, "Template replaced and written to '{0}'", OutputFileName);
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the output file.
        /// </summary>
        [Required]
        public ITaskItem OutputFileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the regular expression that will be used to locate the template variables.
        /// </summary>
        public string SearchExpression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the template file.
        /// </summary>
        [Required]
        public ITaskItem Template
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of replacement tokens.
        /// </summary>
        [Required]
        public ITaskItem[] Tokens
        {
            get;
            set;
        }
    }
}
