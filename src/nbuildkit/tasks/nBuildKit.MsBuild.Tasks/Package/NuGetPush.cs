//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Packaging
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a NuGet push.
    /// </summary>
    public sealed class NuGetPush : NuGetCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPush"/> class.
        /// </summary>
        public NuGetPush()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPush"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public NuGetPush(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var packages = new List<string>();
            if (PackagesToPush != null)
            {
                ITaskItem[] processedTokens = PackagesToPush;
                for (int i = 0; i < processedTokens.Length; i++)
                {
                    ITaskItem taskItem = processedTokens[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        packages.Add(taskItem.ItemSpec);
                    }
                }
            }

            foreach (var package in packages)
            {
                var arguments = new List<string>();
                {
                    arguments.Add(string.Format(CultureInfo.InvariantCulture, "push \"{0}\" ", package.TrimEnd('\\')));
                    arguments.Add("-NonInteractive -Verbosity detailed ");
                    if (!string.IsNullOrEmpty(Source))
                    {
                        arguments.Add(string.Format(CultureInfo.InvariantCulture, "-Source \"{0}\" ", Source.TrimEnd('\\')));
                    }

                    var apiKey = Environment.GetEnvironmentVariable("NuGetApiKey");
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        arguments.Add(string.Format(CultureInfo.InvariantCulture, "-ApiKey \"{0}\" ", apiKey.TrimEnd('\\')));
                    }
                }

                var exitCode = InvokeNuGet(arguments);
                if (exitCode != 0)
                {
                    Log.LogError(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} exited with a non-zero exit code. Exit code was: {1}",
                            Path.GetFileName(NuGetExecutablePath.ItemSpec),
                            exitCode));
                    return false;
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of packages that should be pushed.
        /// </summary>
        [Required]
        public ITaskItem[] PackagesToPush
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source to which the packages should be pushed.
        /// </summary>
        [Required]
        public string Source
        {
            get;
            set;
        }
    }
}
