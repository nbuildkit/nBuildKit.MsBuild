//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a replacement of template variables in a given string.
    /// </summary>
    public sealed class TemplateText : BaseTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataReplacmentValueTag = "ReplacementValue";

            if (string.IsNullOrWhiteSpace(SearchExpression))
            {
                SearchExpression = "(?<token>\\$\\{(?<identifier>\\w*)\\})";
            }

            var regex = new Regex(
                SearchExpression,
                RegexOptions.IgnoreCase
                | RegexOptions.Multiline
                | RegexOptions.Compiled
                | RegexOptions.Singleline);

            var tokenPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Tokens != null)
            {
                ITaskItem[] processedTokens = Tokens;
                for (int i = 0; i < processedTokens.Length; i++)
                {
                    ITaskItem taskItem = processedTokens[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        tokenPairs.Add(taskItem.ItemSpec, taskItem.GetMetadata(MetadataReplacmentValueTag));
                    }
                }
            }

            Result = regex.Replace(
                Template,
                m =>
                {
                    var output = m.Value;
                    if (tokenPairs.ContainsKey(m.Groups[2].Value))
                    {
                        output = tokenPairs[m.Groups[2].Value];
                    }

                    return output;
                });

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the resulting string, after the template variables were replaced in the template string.
        /// </summary>
        [Output]
        public string Result
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
        public string Template
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of replacement tokens.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Tokens
        {
            get;
            set;
        }
    }
}
