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
    /// Defines a <see cref="ITask"/> that performs a Git checkout.
    /// </summary>
    public sealed class GitCheckout : GitCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitCheckout"/> class.
        /// </summary>
        public GitCheckout()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitCheckout"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GitCheckout(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the branch that should be checked out.
        /// </summary>
        [Required]
        public string Branch
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the '--force' argument should be provided to the 'checkout'
        /// command.
        /// </summary>
        public bool Force
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "checkout \"{0}\" ", Branch.TrimEnd('\\')));
                arguments.Add("--quiet ");
                if (Force)
                {
                    arguments.Add("--force");
                }
            }

            InvokeGit(arguments);

            return !Log.HasLoggedErrors;
        }
    }
}
