//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Groups
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that determines if a specific item  or items exist in a collection.
    /// </summary>
    [SuppressMessage(
        "Microsoft.Naming",
        "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "Task name indicates that it finds out if an element is in a collection.")]
    public sealed class IsInCollection : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the collection.
        /// </summary>
        [Required]
        public ITaskItem[] Collection
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            IsInList = false;
            var listOfItems = new List<string>();
            if (Items != null)
            {
                ITaskItem[] processedItems = Items;
                for (int i = 0; i < processedItems.Length; i++)
                {
                    ITaskItem taskItem = processedItems[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        listOfItems.Add(taskItem.ItemSpec);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Item.ItemSpec))
                {
                    listOfItems.Add(Item.ItemSpec);
                }
            }

            var listOfCollectionItems = new List<string>();
            if (Collection != null)
            {
                ITaskItem[] processedItems = Collection;
                for (int i = 0; i < processedItems.Length; i++)
                {
                    ITaskItem taskItem = processedItems[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        listOfCollectionItems.Add(taskItem.ItemSpec);
                    }
                }
            }

            IsInList = listOfCollectionItems.Intersect(listOfItems).Any();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the given item or items are present in the collection.
        /// </summary>
        [Output]
        public bool IsInList
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the item for which the collection should be checked.
        /// </summary>
        public ITaskItem Item
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a collection of items for which the collection should be checked.
        /// </summary>
        public ITaskItem[] Items
        {
            get;
            set;
        }
    }
}
