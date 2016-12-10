//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that invokes FxCop on a given set of assemblies.
    /// </summary>
    public sealed class FxCopViaAssemblies : CommandLineToolTask
    {
        private IEnumerable<string> AssembleFxCopArguments(string targetFramework, string ruleSetFilePath, IEnumerable<string> assemblyPaths)
        {
            var arguments = new List<string>();
            {
                var outputFile = GetAbsolutePath(OutputFile);
                var outputFilePath = Path.Combine(
                    Path.GetDirectoryName(outputFile),
                    string.Format(
                        "{0}_{1}{2}",
                        Path.GetFileNameWithoutExtension(outputFile),
                        targetFramework.Replace(" ", string.Empty).Replace(".", string.Empty),
                        Path.GetExtension(outputFile)));

                arguments.Add(string.Format("/ruleset:=\"{0}\" ", ruleSetFilePath.TrimEnd('\\')));
                arguments.Add(string.Format("/rulesetdirectory:\"{0}\" ", GetAbsolutePath(RuleSetDirectory).TrimEnd('\\')));
                arguments.Add(string.Format("/out:\"{0}\" ", outputFilePath));
                arguments.Add(string.Format("/ignoregeneratedcode "));
                arguments.Add(string.Format("/searchgac "));
                arguments.Add(string.Format("/forceoutput "));
                arguments.Add(string.Format("/successfile "));
                arguments.Add(string.Format("/targetframeworkversion:v{0} ", targetFramework));

                var dictionaryFile = GetAbsolutePath(Dictionary);
                if (!string.IsNullOrEmpty(dictionaryFile))
                {
                    arguments.Add(string.Format("/dictionary:\"{0}\" ", dictionaryFile.TrimEnd('\\')));
                }

                if (!string.IsNullOrEmpty(Culture))
                {
                    arguments.Add(string.Format("/culture:\"{0}\" ", Culture.TrimEnd('\\')));
                }

                if (ReferenceFiles != null)
                {
                    foreach (var referenceFileName in ReferenceFiles)
                    {
                        var referenceFile = GetAbsolutePath(referenceFileName);
                        arguments.Add(string.Format("/reference:\"{0}\" ", referenceFile.TrimEnd('\\')));
                    }
                }

                if (ReferenceDirectories != null)
                {
                    foreach (var referenceDirectory in ReferenceDirectories)
                    {
                        var referenceDir = GetAbsolutePath(referenceDirectory);
                        arguments.Add(string.Format("/directory:\"{0}\" ", referenceDir.TrimEnd('\\')));
                    }
                }

                foreach (var inputFileName in assemblyPaths)
                {
                    arguments.Add(string.Format("/file:\"{0}\" ", inputFileName.TrimEnd('\\')));
                }
            }

            return arguments;
        }

        /// <summary>
        /// Gets or sets the collection of assemblies that should be scanned.
        /// </summary>
        /// <remarks>
        /// Expecting that the taskItems have:
        /// - taskItem.ItemSpec:        Name of the assembly to include in the attribute
        /// - taskItem.TargetFramework: Name of the group the assemblies belong to
        /// - taskItem.RuleSet:         File path of the rule set that should be used
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
                var assemblyPaths = new Dictionary<Tuple<string, string>, List<string>>();
                for (int i = 0; i < Assemblies.Length; i++)
                {
                    var taskItem = Assemblies[i];

                    // Expecting that the taskItems have:
                    // - taskItem.ItemSpec:        Name of the assembly to include in the attribute
                    // - taskItem.TargetFramework: Name of the group the assemblies belong to
                    // - taskItem.RuleSet:         File path of the rule set that should be used
                    var path = GetAbsolutePath(taskItem);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var targetFramework = taskItem.GetMetadata("TargetFramework");
                        if (string.IsNullOrEmpty(targetFramework))
                        {
                            continue;
                        }

                        var ruleSet = taskItem.GetMetadata("RuleSet");
                        if (string.IsNullOrEmpty(ruleSet))
                        {
                            continue;
                        }

                        var pair = new Tuple<string, string>(targetFramework, ruleSet);
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

                foreach (var map in assemblyPaths)
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        string.Format(
                            "Analyzing assemblies with target framework [{0}] with FxCop",
                            map.Key));

                    var arguments = AssembleFxCopArguments(map.Key.Item1, map.Key.Item2, map.Value);
                    InvokeFxCop(arguments);
                }
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the directory that contains the 'FxCopCmd' executable.
        /// </summary>
        [Required]
        public ITaskItem FxCopDir
        {
            get;
            set;
        }

        private void InvokeFxCop(IEnumerable<string> arguments)
        {
            var exePath = Path.Combine(GetAbsolutePath(FxCopDir), "FxCopCmd.exe");
            DataReceivedEventHandler standardOutputhandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };

            DataReceivedEventHandler standardErrorHandler =
                (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Log.LogError(e.Data);
                    }
                };
            var exitCode = InvokeCommandlineTool(
                exePath,
                arguments,
                standardOutputHandler: standardOutputhandler,
                standardErrorHandler: standardErrorHandler);
            if (exitCode != 0)
            {
                if (!WarningsAsErrors)
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        string.Format(
                            "{0} exited with exit code: {1}. Build will continue because errors are assumed to be warnings. To change this set FxCopWarningsAsErrors to 'true' in the settings file.",
                            Path.GetFileName(exePath),
                            exitCode));
                }
                else
                {
                    Log.LogError(
                        string.Format(
                            "{0} exited with a non-zero exit code. Exit code was: {1}",
                            Path.GetFileName(exePath),
                            exitCode));
                }
            }
        }

        /// <summary>
        /// Gets or sets the full path to the FxCop log file.
        /// </summary>
        [Required]
        public ITaskItem OutputFile
        {
            get;
            set;
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

        /// <summary>
        /// Gets or sets a value indicating whether warnings should be treated as errors.
        /// </summary>
        public bool WarningsAsErrors
        {
            get;
            set;
        }
    }
}
