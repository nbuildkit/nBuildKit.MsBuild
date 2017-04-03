//-----------------------------------------------------------------------
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

namespace NBuildKit.MsBuild.Tasks.Packaging
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that performs a NuGet pack.
    /// </summary>
    public sealed class NuGetPack : NuGetCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPack"/> class.
        /// </summary>
        public NuGetPack()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPack"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public NuGetPack(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataValueTag = "ReplacementValue";
            var propertyText = string.Empty;
            if (Properties != null)
            {
                var properties = new List<string>();

                ITaskItem[] processedProperties = Properties;
                for (int i = 0; i < processedProperties.Length; i++)
                {
                    ITaskItem taskItem = processedProperties[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        var metadataItem = taskItem.GetMetadata(MetadataValueTag);
                        properties.Add(string.Format(CultureInfo.InvariantCulture, "{0}=\"{1}\"", taskItem.ItemSpec, metadataItem.TrimEnd('\\')));
                    }
                }

                propertyText = string.Join(";", properties);
            }

            var arguments = new List<string>();
            {
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "pack \"{0}\" ", GetAbsolutePath(File).TrimEnd('\\')));
                if (ShouldBuildSymbols)
                {
                    arguments.Add("-Symbols ");
                }

                // Make sure we remove the back-slash because if we don't then
                // the closing quote will be eaten by the command line parser. Note that
                // this is only necessary because we're dealing with a directory
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "-OutputDirectory \"{0}\" ", GetAbsolutePath(OutputDirectory).TrimEnd('\\')));

                if (!string.IsNullOrEmpty(propertyText))
                {
                    arguments.Add(string.Format(CultureInfo.InvariantCulture, "-Properties {0} ", propertyText));
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

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the nuspec file.
        /// </summary>
        [Required]
        public ITaskItem File
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory into which the package file should be placed.
        /// </summary>
        [Required]
        public ITaskItem OutputDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of properties that should be passed to NuGet for the packaging process.
        /// </summary>
        public ITaskItem[] Properties
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether a symbol package should also be produced.
        /// </summary>
        public bool ShouldBuildSymbols
        {
            get;
            set;
        }
    }
}
