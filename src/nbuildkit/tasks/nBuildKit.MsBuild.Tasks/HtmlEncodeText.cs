//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Web;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that HTML encodes text.
    /// </summary>
    public sealed class HtmlEncodeText : NBuildKitMsBuildTask
    {
        /// <summary>
        /// Gets or sets the HTML encoded text.
        /// </summary>
        [Output]
        public string EncodedText
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            EncodedText = HttpUtility.HtmlEncode(Text);

            return true;
        }

        /// <summary>
        /// Gets or sets the text that should be HTML encoded.
        /// </summary>
        [Required]
        public string Text
        {
            get;
            set;
        }
    }
}
