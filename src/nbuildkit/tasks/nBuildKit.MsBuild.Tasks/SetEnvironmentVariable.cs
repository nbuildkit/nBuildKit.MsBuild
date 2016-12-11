//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that sets an environment variable.
    /// </summary>
    public sealed class SetEnvironmentVariable : NBuildKitMsBuildTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            Environment.SetEnvironmentVariable(Name, Value, EnvironmentVariableTarget.Process);
            return true;
        }

        /// <summary>
        /// Gets or sets the name of the environment variable.
        /// </summary>
        [Required]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the environment variable.
        /// </summary>
        [Required]
        public string Value
        {
            get;
            set;
        }
    }
}
