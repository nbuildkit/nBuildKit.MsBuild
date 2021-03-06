﻿//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Validation
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes FxCop for a given project file.
    /// </summary>
    public sealed class FxCopViaProject : FxCopCommandLineToolTask
    {
        private const string ErrorIdNoProjectFile = "NBuildKit.FxCop.NoProjectFileDefined";

        /// <summary>
        /// Initializes a new instance of the <see cref="FxCopViaProject"/> class.
        /// </summary>
        public FxCopViaProject()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FxCopViaProject"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public FxCopViaProject(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var projectPath = GetAbsolutePath(ProjectFile).TrimEnd('\\');
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoProjectFile),
                    ErrorIdNoProjectFile,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No project file was provided.");
                return false;
            }

            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/project:\"{0}\" ", projectPath));
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
