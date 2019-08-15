//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Validation
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes FxCop on a given set of assemblies.
    /// </summary>
    public sealed class FxCopViaAssemblies : FxCopCommandLineToolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FxCopViaAssemblies"/> class.
        /// </summary>
        public FxCopViaAssemblies()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FxCopViaAssemblies"/> class.
        /// </summary>
        /// <param name="invoker">The object which handles the invocation of the command line applications.</param>
        public FxCopViaAssemblies(IApplicationInvoker invoker)
            : base(invoker)
        {
        }

        private IEnumerable<string> AssembleFxCopArguments(FxCopInvocationProperties invocationProperties, IEnumerable<string> assemblyPaths, int index)
        {
            var arguments = new List<string>();
            {
                var targetFramework = invocationProperties.TargetFramework;
                var outputFile = GetAbsolutePath(OutputFile);
                var outputFilePath = Path.Combine(
                    Path.GetDirectoryName(outputFile),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}_{1}-{2}{3}",
                        Path.GetFileNameWithoutExtension(outputFile),
                        targetFramework.Replace(" ", string.Empty).Replace(".", string.Empty),
                        index,
                        Path.GetExtension(outputFile)));

                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/ruleset:=\"{0}\" ", invocationProperties.RuleSetFilePath.TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/rulesetdirectory:\"{0}\" ", GetAbsolutePath(RuleSetDirectory).TrimEnd('\\')));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/out:\"{0}\" ", outputFilePath));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/ignoregeneratedcode "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/searchgac "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/forceoutput "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/successfile "));
                arguments.Add(string.Format(CultureInfo.InvariantCulture, "/targetframeworkversion:v{0} ", targetFramework));

                var dictionaryFile = GetAbsolutePath(invocationProperties.CustomDictionaryFilePath);
                if (!string.IsNullOrEmpty(dictionaryFile))
                {
                    arguments.Add(string.Format(CultureInfo.InvariantCulture, "/dictionary:\"{0}\" ", dictionaryFile.TrimEnd('\\')));
                }

                if (!string.IsNullOrEmpty(Culture))
                {
                    arguments.Add(string.Format(CultureInfo.InvariantCulture, "/culture:\"{0}\" ", Culture.TrimEnd('\\')));
                }

                if (ReferenceFiles != null)
                {
                    foreach (var referenceFileName in ReferenceFiles)
                    {
                        var referenceFile = GetAbsolutePath(referenceFileName);
                        arguments.Add(string.Format(CultureInfo.InvariantCulture, "/reference:\"{0}\" ", referenceFile.TrimEnd('\\')));
                    }
                }

                if (ReferenceDirectories != null)
                {
                    foreach (var referenceDirectory in ReferenceDirectories)
                    {
                        var referenceDir = GetAbsolutePath(referenceDirectory);
                        arguments.Add(string.Format(CultureInfo.InvariantCulture, "/directory:\"{0}\" ", referenceDir.TrimEnd('\\')));
                    }
                }

                foreach (var inputFileName in assemblyPaths)
                {
                    arguments.Add(string.Format(CultureInfo.InvariantCulture, "/file:\"{0}\" ", inputFileName.TrimEnd('\\')));
                }
            }

            return arguments;
        }

        /// <summary>
        /// Gets or sets the collection of assemblies that should be scanned.
        /// </summary>
        /// <remarks>
        /// Expecting that the taskItems have:
        /// - taskItem.ItemSpec:        Name of the assembly to include in the attribute.
        /// - taskItem.TargetFramework: Name of the group the assemblies belong to.
        /// - taskItem.RuleSet:         File path of the rule set that should be used.
        /// </remarks>
        [Required]
        public ITaskItem[] Assemblies
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the culture that should be used during the FxCop analysis.
        /// </summary>
        public string Culture
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the dictionary file that contains spelling corrections.
        /// </summary>
        public ITaskItem Dictionary
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (Assemblies != null)
            {
                var assemblyPaths = new Dictionary<FxCopInvocationProperties, List<string>>();
                for (int i = 0; i < Assemblies.Length; i++)
                {
                    var taskItem = Assemblies[i];

                    // Expecting that the taskItems have:
                    // - taskItem.ItemSpec:         Name of the assembly to include in the attribute
                    // - taskItem.CustomDictionary: File path of the custom dictionary that should be used
                    // - taskItem.RuleSet:          File path of the rule set that should be used
                    // - taskItem.TargetFramework:  Name of the group the assemblies belong to
                    var path = GetAbsolutePath(taskItem);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var dictionary = taskItem.GetMetadata("CustomDictionary");

                        var ruleSet = taskItem.GetMetadata("RuleSet");
                        if (string.IsNullOrEmpty(ruleSet))
                        {
                            continue;
                        }

                        var targetFramework = taskItem.GetMetadata("TargetFramework");
                        if (string.IsNullOrEmpty(targetFramework))
                        {
                            continue;
                        }

                        var pair = new FxCopInvocationProperties(targetFramework, ruleSet, dictionary);
                        if (!assemblyPaths.ContainsKey(pair))
                        {
                            assemblyPaths.Add(pair, new List<string>());
                        }

                        var list = assemblyPaths[pair];
                        if (!list.Contains(path))
                        {
                            list.Add(path);
                        }
                    }
                }

                for (int i = 0; i < assemblyPaths.Count; i++)
                {
                    var pair = assemblyPaths.ElementAt(i);

                    Log.LogMessage(
                        MessageImportance.Normal,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Analyzing assemblies with {0}",
                            pair.Key));

                    var arguments = AssembleFxCopArguments(pair.Key, pair.Value, i);
                    InvokeFxCop(arguments);
                }
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the collection of reference directories from which FxCop can load additional assemblies.
        /// </summary>
        public ITaskItem[] ReferenceDirectories
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of reference files which FxCop can load if additional referenced assemblies are required.
        /// </summary>
        public ITaskItem[] ReferenceFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path to the directory that contains the FxCop rule sets.
        /// </summary>
        [Required]
        public ITaskItem RuleSetDirectory
        {
            get;
            set;
        }
    }
}
