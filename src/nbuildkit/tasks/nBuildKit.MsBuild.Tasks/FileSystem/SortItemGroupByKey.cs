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
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.FileSystem
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that sort a set of items by a given key.
    /// </summary>
    public sealed class SortItemGroupByKey : BaseTask
    {
        /// <summary>
        /// Gets or sets the collection of items.
        /// </summary>
        [Required]
        public ITaskItem[] Items
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataOrderTag = "Order";

            var list = new SortedList<int, ITaskItem>();
            if (Items != null)
            {
                ITaskItem[] processedItems = Items;
                for (int i = 0; i < processedItems.Length; i++)
                {
                    ITaskItem item = processedItems[i];
                    if (!string.IsNullOrEmpty(item.ItemSpec))
                    {
                        var index = int.Parse(item.GetMetadata(MetadataOrderTag), CultureInfo.InvariantCulture);
                        list.Add(index, item);
                    }
                }
            }

            SortedItems = list.Values.ToArray();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of sorted items.
        /// </summary>
        [Output]
        public ITaskItem[] SortedItems
        {
            get;
            set;
        }
    }
}
