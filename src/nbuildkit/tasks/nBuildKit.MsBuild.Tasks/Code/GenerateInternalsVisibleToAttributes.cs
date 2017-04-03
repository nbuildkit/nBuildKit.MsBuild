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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Code
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that generates one or more <see cref="InternalsVisibleToAttribute"/> instances for inclusion in
    /// a AssemblyInfo file.
    /// </summary>
    public sealed class GenerateInternalsVisibleToAttributes : CommandLineToolTask
    {
        private const string MetadataValueTag = "ReplacementValue";

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateInternalsVisibleToAttributes"/> class.
        /// </summary>
        public GenerateInternalsVisibleToAttributes()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateInternalsVisibleToAttributes"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public GenerateInternalsVisibleToAttributes(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        /// <summary>
        /// Gets or sets the string template for the <see cref="InternalsVisibleToAttribute"/> for assemblies that are strong named.
        /// </summary>
        [Required]
        public string AttributeTemplateForSignedAssemblies
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the string template for the <see cref="InternalsVisibleToAttribute"/> for assemblies that are not strong named.
        /// </summary>
        [Required]
        public string AttributeTemplateForUnsignedAssemblies
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (Items == null)
            {
                Log.LogError("No InternalsVisibleTo attributes to generate!");
                return false;
            }

            var regex = new System.Text.RegularExpressions.Regex(
                "(?<token>\\$\\{(?<identifier>\\w*)\\})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Multiline
                | System.Text.RegularExpressions.RegexOptions.Compiled
                | System.Text.RegularExpressions.RegexOptions.Singleline);

            var builder = new StringBuilder();

            ITaskItem[] processedItems = Items;
            for (int i = 0; i < processedItems.Length; i++)
            {
                ITaskItem taskItem = processedItems[i];

                // Expecting that the taskItems have:
                // - taskItem.ItemSpec:            Name of the assembly to include in the attribute
                // - taskItem.Projects:            Semi-colon separated list of projects for which the assembly should be added to the internals visible to list
                // - taskItem.KeyFile:             The full path to the key file that contains the strong name public key
                // - taskItem.AssemblyFromPackage: The file name of the assembly that should be included, noting that this assembly is found in the packages directory
                // - taskItem.PublicKey:           The full public key of the assembly
                if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                {
                    var projects = taskItem.GetMetadata("Projects");
                    if (string.IsNullOrEmpty(projects))
                    {
                        continue;
                    }

                    var projectsAsArray = projects.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToArray();
                    if (!projectsAsArray.Any(p => string.Equals(p, Project, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    Log.LogMessage(MessageImportance.Normal, "Adding InternalsVisibleTo attribute for: " + taskItem.ItemSpec);

                    var key = string.Empty;

                    var publicKey = taskItem.GetMetadata("PublicKey");
                    if (!string.IsNullOrEmpty(publicKey))
                    {
                        Log.LogMessage(MessageImportance.Normal, "Using provided public key: " + publicKey);
                        key = publicKey;
                    }
                    else
                    {
                        var snExeFileName = Path.GetFileName(GetAbsolutePath(SnExe));

                        var keyFile = taskItem.GetMetadata("KeyFile");
                        if (!string.IsNullOrEmpty(keyFile))
                        {
                            Log.LogMessage(MessageImportance.Normal, "Extracting public key from key file: " + keyFile);

                            var tempDir = GetAbsolutePath(TemporaryDirectory);
                            if (!Directory.Exists(tempDir))
                            {
                                Directory.CreateDirectory(tempDir);
                            }

                            var publicKeyFile = Path.Combine(tempDir, Path.GetRandomFileName());
                            try
                            {
                                {
                                    var arguments = new[] { string.Format(CultureInfo.InvariantCulture, "-p \"{0}\" \"{1}\"", keyFile, publicKeyFile) };
                                    var exitCode = InvokeCommandLineTool(
                                        SnExe,
                                        arguments);

                                    if (exitCode != 0)
                                    {
                                        Log.LogError(
                                            string.Format(
                                                CultureInfo.InvariantCulture,
                                                "{0} exited with a non-zero exit code while trying to extract the public key file from the signing key file. Exit code was: {1}",
                                                snExeFileName,
                                                exitCode));
                                        return false;
                                    }
                                }

                                var text = new StringBuilder();
                                {
                                    var arguments = new[] { string.Format(CultureInfo.InvariantCulture, "-tp \"{0}\"", publicKeyFile) };
                                    DataReceivedEventHandler standardOutputHandler = (s, e) =>
                                        {
                                            text.Append(e.Data);
                                            if (!string.IsNullOrWhiteSpace(e.Data))
                                            {
                                                Log.LogMessage(MessageImportance.Low, e.Data);
                                            }
                                        };
                                    var exitCode = InvokeCommandLineTool(
                                        SnExe,
                                        arguments,
                                        standardOutputHandler: standardOutputHandler);
                                    if (exitCode != 0)
                                    {
                                        Log.LogError(
                                            string.Format(
                                                CultureInfo.InvariantCulture,
                                                "{0} exited with a non-zero exit code while trying to extract the public key information from the public key file. Exit code was: {1}",
                                                snExeFileName,
                                                exitCode));
                                        return false;
                                    }
                                }

                                var publicKeyText = text.ToString();
                                if (string.IsNullOrEmpty(publicKeyText))
                                {
                                    Log.LogError("Failed to extract public key from key file.");
                                    continue;
                                }

                                const string startString = "Public key (hash algorithm: sha1):";
                                const string endString = "Public key token is";
                                var startIndex = publicKeyText.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
                                var endIndex = publicKeyText.IndexOf(endString, StringComparison.OrdinalIgnoreCase);
                                key = publicKeyText.Substring(startIndex + startString.Length, endIndex - (startIndex + startString.Length));
                            }
                            finally
                            {
                                if (File.Exists(publicKeyFile))
                                {
                                    File.Delete(publicKeyFile);
                                }
                            }
                        }
                        else
                        {
                            var assemblyFromPackage = taskItem.GetMetadata("AssemblyFromPackage");
                            if (!string.IsNullOrEmpty(assemblyFromPackage))
                            {
                                Log.LogMessage(MessageImportance.Normal, "Extracting public key from assembly file: " + assemblyFromPackage);

                                var assemblyPath = Directory.EnumerateFiles(GetAbsolutePath(PackagesDirectory), assemblyFromPackage, SearchOption.AllDirectories)
                                    .OrderBy(k => Path.GetDirectoryName(k))
                                    .LastOrDefault();
                                if (string.IsNullOrEmpty(assemblyPath))
                                {
                                    Log.LogError("Failed to find the full path of: " + assemblyFromPackage);
                                    continue;
                                }

                                var text = new StringBuilder();
                                DataReceivedEventHandler standardOutputHandler =
                                    (s, e) =>
                                    {
                                        text.Append(e.Data);
                                        if (!string.IsNullOrWhiteSpace(e.Data))
                                        {
                                            Log.LogMessage(MessageImportance.Low, e.Data);
                                        }
                                    };
                                var exitCode = InvokeCommandLineTool(
                                    SnExe,
                                    new[] { string.Format(CultureInfo.InvariantCulture, "-Tp \"{0}\"", assemblyPath.TrimEnd('\\')) },
                                    standardOutputHandler: standardOutputHandler);

                                if (exitCode != 0)
                                {
                                    Log.LogError(
                                        string.Format(
                                            CultureInfo.InvariantCulture,
                                            "{0} exited with a non-zero exit code while trying to extract the public key from a signed assembly. Exit code was: {1}",
                                            snExeFileName,
                                            exitCode));
                                    return false;
                                }

                                var publicKeyText = text.ToString();
                                if (string.IsNullOrEmpty(publicKeyText))
                                {
                                    Log.LogError("Failed to extract public key from assembly.");
                                    continue;
                                }

                                const string startString = "Public key (hash algorithm: sha1):";
                                const string endString = "Public key token is";
                                var startIndex = publicKeyText.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
                                var endIndex = publicKeyText.IndexOf(endString, StringComparison.OrdinalIgnoreCase);
                                key = publicKeyText.Substring(startIndex + startString.Length, endIndex - (startIndex + startString.Length));
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(key))
                    {
                        var tokenPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            { "AssemblyName", taskItem.ItemSpec },
                                            { "Key", key },
                                        };
                        var attributeText = regex.Replace(
                            AttributeTemplateForSignedAssemblies,
                            m =>
                            {
                                var output = m.Value;
                                if (tokenPairs.ContainsKey(m.Groups[2].Value))
                                {
                                    output = tokenPairs[m.Groups[2].Value];
                                }
                                return output;
                            });
                        builder.AppendLine(attributeText);
                    }
                    else
                    {
                        var tokenPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            { "AssemblyName", taskItem.ItemSpec },
                                        };
                        var attributeText = regex.Replace(
                            AttributeTemplateForUnsignedAssemblies,
                            m =>
                            {
                                var output = m.Value;
                                if (tokenPairs.ContainsKey(m.Groups[2].Value))
                                {
                                    output = tokenPairs[m.Groups[2].Value];
                                }
                                return output;
                            });
                        builder.AppendLine(attributeText);
                    }
                }
            }

            Result = builder.ToString();

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of items that describe which projects should have an <see cref="InternalsVisibleToAttribute"/> added.
        /// </summary>
        [Required]
        public ITaskItem[] Items
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the packages directory into which all NuGet packages are expanded.
        /// </summary>
        [Required]
        public ITaskItem PackagesDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the project for which the <see cref="InternalsVisibleToAttribute"/> should be added to the AssemblyInfo file.
        /// </summary>
        [Required]
        public string Project
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the string containing all the <see cref="InternalsVisibleToAttribute"/> instances.
        /// </summary>
        [Output]
        public string Result
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the strong name tool (sn.exe).
        /// </summary>
        [Required]
        public ITaskItem SnExe
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to a directory into which temporary files may be created.
        /// </summary>
        [Required]
        public ITaskItem TemporaryDirectory
        {
            get;
            set;
        }
    }
}
