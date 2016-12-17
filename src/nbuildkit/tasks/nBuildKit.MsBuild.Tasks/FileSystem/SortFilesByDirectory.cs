//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that sort a set of files based on their parent directory.
    /// </summary>
    public sealed class SortFilesByDirectory : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the collection containing the directories.
        /// </summary>
        [Output]
        public ITaskItem[] Directories
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataCountTag = "Index";
            const string MetadataFilesTag = "Files";

            var list = new SortedList<string, ITaskItem>();
            if (Files != null)
            {
                ITaskItem[] processedItems = Files;
                for (int i = 0; i < processedItems.Length; i++)
                {
                    ITaskItem item = processedItems[i];
                    if (!string.IsNullOrEmpty(item.ItemSpec))
                    {
                        var filePath = System.IO.Path.GetFullPath(item.ItemSpec);
                        var directory = System.IO.Path.GetDirectoryName(filePath);
                        if (!list.ContainsKey(directory))
                        {
                            var newItem = new TaskItem(directory);
                            newItem.SetMetadata(MetadataFilesTag, string.Empty);
                            newItem.SetMetadata(MetadataCountTag, (list.Count + 1).ToString(CultureInfo.InvariantCulture));

                            list.Add(directory, newItem);
                        }

                        var storedItem = list[directory];
                        var files = storedItem.GetMetadata(MetadataFilesTag);
                        files = files + (files.Length > 0 ? ";" : string.Empty) + filePath;

                        storedItem.SetMetadata(MetadataFilesTag, files);
                    }
                }
            }

            Directories = list.Values.ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection containing the files to be sorted.
        /// </summary>
        [Required]
        public ITaskItem[] Files
        {
            get;
            set;
        }
    }
}
