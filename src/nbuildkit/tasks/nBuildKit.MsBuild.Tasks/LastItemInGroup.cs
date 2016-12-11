//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that returns the last item in a given collection.
    /// </summary>
    public sealed class LastItemInGroup : NBuildKitMsBuildTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            try
            {
                if (Items != null)
                {
                    ITaskItem[] processedItems = Items;
                    if (processedItems.Length > 0)
                    {
                        var taskItem = processedItems[processedItems.Length - 1];
                        if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                        {
                            Item = new TaskItem(taskItem.ItemSpec);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError(
                    string.Format(
                        "Failed to find the last item in the collection. Error was: {0}",
                        e));
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the item for which the collection should be checked.
        /// </summary>
        [Output]
        public ITaskItem Item
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a collection of items for which the collection should be checked.
        /// </summary>
        [Required]
        public ITaskItem[] Items
        {
            get;
            set;
        }
    }
}
