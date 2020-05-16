//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that replaces tokens in a string with values from a token collection.
    /// </summary>
    public sealed class FindAndReplaceInText : BaseTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataReplacementValueTag = "ReplacementValue";

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

            Output = Input;
            foreach (var pair in tokenPairs)
            {
                if (Output.Contains(pair.Key))
                {
                    Log.LogMessage(MessageImportance.Low, "Replacing [" + pair.Key + "] with [" + pair.Value + "]");
                    Output = Output.Replace(pair.Key, pair.Value);
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the input text.
        /// </summary>
        [Required]
        public string Input
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the output text.
        /// </summary>
        [Output]
        public string Output
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of tokens.
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
