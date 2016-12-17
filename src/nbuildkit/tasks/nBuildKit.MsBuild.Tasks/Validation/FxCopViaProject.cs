//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Validation
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes FxCop for a given project file.
    /// </summary>
    public sealed class FxCopViaProject : FxCopCommandLineToolTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/project:\"{0}\" ", GetAbsolutePath(ProjectFile).TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/out:\"{0}\" ", GetAbsolutePath(OutputFile).TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/ignoregeneratedcode "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/searchgac "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/forceoutput "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/successfile "));
            }

            InvokeFxCop(arguments);

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the project file.
        /// </summary>
        [Required]
        public ITaskItem ProjectFile
        {
            get;
            set;
        }
    }
}
