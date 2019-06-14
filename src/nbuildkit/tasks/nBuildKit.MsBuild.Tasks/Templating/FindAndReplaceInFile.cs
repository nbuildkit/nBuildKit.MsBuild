//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that replaces tokens in a file with values from a token collection.
    /// </summary>
    public sealed class FindAndReplaceInFile : BaseTask
    {
        private const string MetadataValueTag = "ReplacementValue";

        /// <inheritdoc/>
        public override bool Execute()
        {
            var inputFile = GetAbsolutePath(Input);
            if (!File.Exists(inputFile))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Input File '{0}' cannot be found",
                    inputFile);
            }
            else
            {
                var toReplace = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (Tokens != null)
                {
                    ITaskItem[] processedTokens = Tokens;
                    for (int i = 0; i < processedTokens.Length; i++)
                    {
                        ITaskItem taskItem = processedTokens[i];
                        if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                        {
                            toReplace.Add(taskItem.ItemSpec, taskItem.GetMetadata(MetadataValueTag));
                        }
                    }
                }

                string text;
                using (var streamReader = new StreamReader(inputFile))
                {
                    text = streamReader.ReadToEnd();
                }

                foreach (var pair in toReplace)
                {
                    if (text.Contains(pair.Key))
                    {
                        Log.LogMessage(MessageImportance.Low, "Replacing [" + pair.Key + "] with [" + pair.Value + "]");
                        text = text.Replace(pair.Key, pair.Value);
                    }
                }

                if (File.Exists(inputFile))
                {
                    File.SetAttributes(inputFile, FileAttributes.Normal);
                }

                using (var streamWriter = new StreamWriter(inputFile))
                {
                    streamWriter.WriteLine(text);
                    streamWriter.Flush();
                }
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the path to the file that should have tokens replaced.
        /// </summary>
        [Required]
        public ITaskItem Input
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
