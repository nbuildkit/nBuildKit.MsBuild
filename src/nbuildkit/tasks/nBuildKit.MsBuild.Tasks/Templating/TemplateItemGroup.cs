//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a replacement of template variables in a given file.
    /// </summary>
    public sealed class TemplateItemGroup : BaseTask
    {
        private static string ReplaceTemplate(string text, Regex regex, Dictionary<string, string> tokens)
        {
            var value = regex.Replace(
                text,
                m =>
                {
                    var output = m.Value;
                    if (tokens.ContainsKey(m.Groups[2].Value))
                    {
                        output = tokens[m.Groups[2].Value];
                    }

                    return output;
                });

            return value;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataReplacementValueTag = "ReplacementValue";

            if (string.IsNullOrWhiteSpace(SearchExpression))
            {
                SearchExpression = "(?<token>\\$\\{(?<identifier>\\w*)\\})";
            }

            Log.LogMessage(MessageImportance.Low, "Searching for replacement tokens with the regular expression '{0}'", SearchExpression);

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
                        if (!tokenPairs.ContainsKey(taskItem.ItemSpec))
                        {
                            tokenPairs.Add(taskItem.ItemSpec, taskItem.GetMetadata(MetadataReplacementValueTag));
                        }
                        else
                        {
                            Log.LogError(
                                "A template token with the name {0} already exists in the list. Was going to add token: {0} - replacement value: {1}",
                                taskItem.ItemSpec,
                                taskItem.GetMetadata(MetadataReplacementValueTag));
                        }
                    }
                }
            }

            var updatedItems = new List<ITaskItem>();
            foreach (var item in Items)
            {
                var value = ReplaceTemplate(item.ItemSpec, regex, tokenPairs);
                var updatedItem = new TaskItem(value);

                var dict = item.CloneCustomMetadata();
                foreach (DictionaryEntry pair in dict)
                {
                    updatedItem.SetMetadata(
                        pair.Key.ToString(),
                        ReplaceTemplate(pair.Value.ToString(), regex, tokenPairs));
                }

                updatedItems.Add(updatedItem);
            }

            UpdatedItems = updatedItems.ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of items that need to have templates replaced by values.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Items
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

        /// <summary>
        /// Gets or sets the collection of items with their templates replaced.
        /// </summary>
        [Output]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] UpdatedItems
        {
            get;
            set;
        }
    }
}
