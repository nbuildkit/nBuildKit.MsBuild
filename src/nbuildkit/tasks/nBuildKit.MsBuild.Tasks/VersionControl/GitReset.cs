//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.VersionControl
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a Git reset.
    /// </summary>
    public sealed class GitReset : GitCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitReset"/> class.
        /// </summary>
        public GitReset()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitReset"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GitReset(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the commit ID to which the current workspace should be reset.
        /// </summary>
        [Required]
        public string Commit
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add("reset ");
                arguments.Add("--hard ");
                arguments.Add("--quiet ");
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "{0} ", Commit));
            }

            InvokeGit(arguments);

            return !Log.HasLoggedErrors;
        }
    }
}
