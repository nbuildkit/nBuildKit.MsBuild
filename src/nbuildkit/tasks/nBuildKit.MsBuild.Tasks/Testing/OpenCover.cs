//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Testing
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes OpenCover with a given unit testing framework.
    /// </summary>
    public sealed class OpenCover : CommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCover"/> class.
        /// </summary>
        public OpenCover()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCover"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public OpenCover(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the full path to the directory containing the binaries.
        /// </summary>
        [Required]
        public ITaskItem BinDirectory
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var arguments = new List<string>();
            {
                // arguments.Add(string.Format(CultureInfo.InvariantCulture, "-register:user "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-register:Path64 "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-returntargetcode:3000 "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-target:\"{0}\" ", UnitTestExe));

                // Make sure we remove the back-slash because if we don't then
                // the closing quote will be eaten by the command line parser. Note that
                // this is only necessary because we're dealing with a directory
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-targetdir:\"{0}\" ", GetAbsolutePath(BinDirectory).TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-targetargs:\"{0}\" ", UnitTestArguments.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-output:\"{0}\" ", GetAbsolutePath(OpenCoverOutput).TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-filter:\"{0}\" ", OpenCoverFilters.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-excludebyattribute:{0} ", OpenCoverExcludeAttributes));
            }

            var exitCode = InvokeCommandLineTool(
                OpenCoverExe,
                arguments,
                standardErrorHandler: DefaultErrorHandler);
            if (exitCode != 0)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode),
                    Core.ErrorInformation.ErrorIdApplicationNonzeroExitCode,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    System.IO.Path.GetFileName(GetFullToolPath(OpenCoverExe)),
                    exitCode);
                return false;
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the OpenCover command line executable.
        /// </summary>
        [Required]
        public ITaskItem OpenCoverExe
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the filters for OpenCover.
        /// </summary>
        [Required]
        public string OpenCoverFilters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory into which OpenCover should write the output.
        /// </summary>
        [Required]
        public ITaskItem OpenCoverOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full names of the attributes which mark code as excluded from code coverage.
        /// </summary>
        [Required]
        public string OpenCoverExcludeAttributes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the command line executable for the unit testing framework.
        /// </summary>
        [Required]
        public ITaskItem UnitTestExe
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments that should be passed to the unit testing framework.
        /// </summary>
        [Required]
        public string UnitTestArguments
        {
            get;
            set;
        }
    }
}
