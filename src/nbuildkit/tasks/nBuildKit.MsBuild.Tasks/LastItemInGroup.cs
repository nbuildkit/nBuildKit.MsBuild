//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
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
