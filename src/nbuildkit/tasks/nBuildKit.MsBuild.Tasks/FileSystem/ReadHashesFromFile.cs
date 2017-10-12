//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core;
using Newtonsoft.Json;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that reads a hash from a file.
    /// </summary>
    public sealed class ReadHashesFromFile : BaseTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            if ((Path == null) || string.IsNullOrWhiteSpace(Path.ItemSpec))
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
                    "The file path is not defined. Unable to load hash information.");
                return false;
            }

            var filePath = GetAbsolutePath(Path);
            if (!File.Exists(filePath))
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
                    "The file was expected to be found at '{0}' but that path does not exist. Unable to load hash information.",
                    filePath);
                return false;
            }

            string text;
            using (var reader = new StreamReader(filePath))
            {
                text = reader.ReadToEnd();
            }

            dynamic json = JsonConvert.DeserializeObject(text);

            var items = new List<ITaskItem>();
            foreach (var obj in json)
            {
                if ((bool)obj.IsEndValue)
                {
                    break;
                }

                items.Add(
                    new TaskItem(
                        (string)obj.File,
                        new Hashtable
                        {
                            {
                                "Algorithm",
                                (string)obj.Algorithm
                            },
                            {
                                "Hash",
                                (string)obj.Hash
                            }
                        }));
            }

            Hashes = items.ToArray();

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the selected file hashes.
        /// </summary>
        [Output]
        public ITaskItem[] Hashes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the file that contains the hash information.
        /// </summary>
        [Required]
        public ITaskItem Path
        {
            get;
            set;
        }
    }
}
