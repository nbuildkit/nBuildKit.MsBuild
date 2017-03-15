//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes a tool with the given argument set.
    /// </summary>
    public sealed class ExecWithArguments : CommandLineToolTask
    {
        private const string ArgumentValueMetadataName = "Value";

        private const string DefaultArgumentSeparator = " ";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecWithArguments"/> class.
        /// </summary>
        public ExecWithArguments()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecWithArguments"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public ExecWithArguments(IApplicationInvoker invoker)
            : base(invoker)
        {
            ArgumentPrefix = string.Empty;
            ArgumentSeparator = DefaultArgumentSeparator;
        }

        /// <summary>
        /// Gets or sets the paths that should be added to the PATH environment variable.
        /// </summary>
        public ITaskItem[] AdditionalEnvironmentPaths
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments for the tool invocation. Items are expected to consist of the argument
        /// name with a meta data item called 'Value' which provides the value for the argument.
        /// </summary>
        [Required]
        public ITaskItem[] Arguments
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the prefix that will be pre-pended to the argument. Defaults to an empty string.
        /// </summary>
        public string ArgumentPrefix
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the separator character that will be used to separate the argument and its value.
        /// Defaults to a space character.
        /// </summary>
        public string ArgumentSeparator
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            var toolFileName = GetFullToolPath(ToolPath);

            var arguments = new List<string>();
            foreach (var item in Arguments)
            {
                var argument = item.ItemSpec;
                if (argument.Any(char.IsWhiteSpace))
                {
                    argument = string.Format(
                        CultureInfo.InvariantCulture,
                        "\"{0}\"",
                        argument);
                }

                var itemValue = item.GetMetadata(ArgumentValueMetadataName);

                var separator = string.Empty;
                var value = string.Empty;
                if (!string.IsNullOrWhiteSpace(itemValue))
                {
                    separator = ArgumentSeparator;
                    value = itemValue;
                    if (value.Any(char.IsWhiteSpace))
                    {
                        value = string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            value);
                    }
                }

                arguments.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}{1}{2}{3}",
                        ArgumentPrefix,
                        argument,
                        separator,
                        value));
            }

            DataReceivedEventHandler standardErrorHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        if (IgnoreErrors)
                        {
                            Log.LogWarning(e.Data);
                        }
                        else
                        {
                            Log.LogError(e.Data);
                        }
                    }
                };
            var exitCode = InvokeCommandLineTool(
                toolFileName,
                arguments,
                GetAbsolutePath(WorkingDirectory),
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                var text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} exited with a non-zero exit code. Exit code was: {1}",
                    toolFileName,
                    exitCode);
                if (IgnoreExitCode)
                {
                    Log.LogWarning(text);
                }
                else
                {
                    Log.LogError(text);
                }
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets a value indicating whether error should be ignored.
        /// </summary>
        public bool IgnoreErrors
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the exit code should be ignored.
        /// </summary>
        public bool IgnoreExitCode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the executable.
        /// </summary>
        [Required]
        public ITaskItem ToolPath
        {
            get;
            set;
        }

        /// <summary>
        /// Updates the environment variables for the application prior to execution.
        /// </summary>
        /// <param name="environmentVariables">
        ///     The environment variables for the application. The environment variables for the process can be
        ///     changed by altering the collection.
        /// </param>
        protected override void UpdateEnvironmentVariables(StringDictionary environmentVariables)
        {
            if (environmentVariables == null)
            {
                return;
            }

            environmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");
            if ((AdditionalEnvironmentPaths != null) && (AdditionalEnvironmentPaths.Length > 0))
            {
                foreach (var path in AdditionalEnvironmentPaths)
                {
                    environmentVariables["PATH"] += ";" + GetAbsolutePath(path);
                }
            }
        }

        /// <summary>
        /// Gets or sets the full path to the working directory.
        /// </summary>
        [Required]
        public ITaskItem WorkingDirectory
        {
            get;
            set;
        }
    }
}
