//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that executes steps for nBuildKit.
    /// </summary>
    public sealed class InvokeSteps : NBuildKitMsBuildTask
    {
        private static ITaskItem[] LocalPreSteps(ITaskItem step)
        {
            const string MetadataValueTag = "PreSteps";
            var steps = step.GetMetadata(MetadataValueTag);
            return steps.ToLower(CultureInfo.InvariantCulture).Split(';').Select(s => new TaskItem(s)).ToArray();
        }

        private static ITaskItem[] LocalPostSteps(ITaskItem step)
        {
            const string MetadataValueTag = "PostSteps";
            var steps = step.GetMetadata(MetadataValueTag);
            return steps.ToLower(CultureInfo.InvariantCulture).Split(';').Select(s => new TaskItem(s)).ToArray();
        }

        private static IEnumerable<string> StepGroups(ITaskItem step)
        {
            const string MetadataValueTag = "Groups";
            var groups = step.GetMetadata(MetadataValueTag);
            return groups.ToLower(CultureInfo.InvariantCulture).Split(';');
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catching, logging and letting MsBuild deal with the fall out")]
        public override bool Execute()
        {
            if ((Steps == null) || (Steps.Length == 0))
            {
                return true;
            }

            // Get groups and determine which steps should be executed
            var groups = Groups();
            foreach (var step in Steps)
            {
                try
                {
                    if (!ExecuteStep(step, groups))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.LogError(
                        "Execution of steps failed with exception. Exception was: {0}",
                        e);
                }
            }

            if (Log.HasLoggedErrors)
            {
                foreach (var step in FailureSteps)
                {
                    if (!ExecuteFailureStep(step, groups))
                    {
                        break;
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }

        private bool ExecuteFailureStep(ITaskItem step, IEnumerable<string> groups)
        {
            var stepGroups = StepGroups(step);
            if (!ShouldExecuteStep(groups, stepGroups))
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    "Step {0} not included in execution list of {1}.",
                    step.ItemSpec,
                    string.Join(", ", groups));
                return true;
            }

            var result = InvokeBuildEngine(step);
            if (!result && StopOnFirstFailure)
            {
                Log.LogError(
                    "Failed while executing step action from '{0}'",
                    step.ItemSpec);
                return false;
            }

            return true;
        }

        private bool ExecuteStep(ITaskItem step, IEnumerable<string> groups)
        {
            var stepGroups = StepGroups(step);
            if (!ShouldExecuteStep(groups, stepGroups))
            {
                Log.LogMessage(
                    MessageImportance.Low,
                    "Step {0} not included in execution list of {1}.",
                    step.ItemSpec,
                    string.Join(", ", groups));
                return true;
            }

            bool result = true;
            if (PreSteps != null)
            {
                foreach (var globalPreStep in PreSteps)
                {
                    if (!string.IsNullOrEmpty(globalPreStep.ItemSpec))
                    {
                        result = InvokeBuildEngine(globalPreStep);
                        if (!result && StopOnFirstFailure)
                        {
                            Log.LogError(
                                "Failed while executing global pre-step action from '{0}'",
                                globalPreStep.ItemSpec);
                            return false;
                        }
                    }
                }
            }

            var localPreSteps = LocalPreSteps(step);
            if (localPreSteps != null)
            {
                foreach (var localPreStep in localPreSteps)
                {
                    if (!string.IsNullOrEmpty(localPreStep.ItemSpec))
                    {
                        result = InvokeBuildEngine(localPreStep);
                        if (!result && StopOnFirstFailure)
                        {
                            Log.LogError(
                                "Failed while executing step specific pre-step action from '{0}'",
                                localPreStep.ItemSpec);
                            return false;
                        }
                    }
                }
            }

            result = InvokeBuildEngine(step);
            if (!result && StopOnFirstFailure)
            {
                Log.LogError(
                    "Failed while executing step action from '{0}'",
                    step.ItemSpec);
                return false;
            }

            var localPostSteps = LocalPostSteps(step);
            if (localPostSteps != null)
            {
                foreach (var localPostStep in localPostSteps)
                {
                    if (!string.IsNullOrEmpty(localPostStep.ItemSpec))
                    {
                        result = InvokeBuildEngine(localPostStep);
                        if (!result && StopOnFirstFailure)
                        {
                            Log.LogError(
                                "Failed while executing step specific post-step action from '{0}'",
                                localPostStep.ItemSpec);
                            return false;
                        }
                    }
                }
            }

            if (PostSteps != null)
            {
                foreach (var globalPostStep in PostSteps)
                {
                    if (!string.IsNullOrEmpty(globalPostStep.ItemSpec))
                    {
                        result = InvokeBuildEngine(globalPostStep);
                        if (!result && StopOnFirstFailure)
                        {
                            Log.LogError(
                                "Failed while executing global post-step action from '{0}'",
                                globalPostStep.ItemSpec);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets or sets the steps that should be taken if a step fails.
        /// </summary>
        public ITaskItem[] FailureSteps
        {
            get;
            set;
        }

        private IEnumerable<string> Groups()
        {
            return GroupsToExecute.Select(t => t.ItemSpec).ToList();
        }

        /// <summary>
        /// Gets or sets the collection of tags that mark which steps should be executed. If no groups are specified
        /// it is assumed that all valid steps should be executed.
        /// </summary>
        public ITaskItem[] GroupsToExecute
        {
            get;
            set;
        }

        private bool InvokeBuildEngine(ITaskItem project)
        {
            Hashtable propertiesTable;
            if (!PropertyParser.GetTableWithEscaping(Log, "GlobalProperties", "Properties", Properties.Select(t => t.ItemSpec).ToArray(), out propertiesTable))
            {
                return false;
            }

            string projectPath = GetAbsolutePath(project.ItemSpec);
            if (File.Exists(projectPath))
            {
                var toolsVersion = ToolsVersion;
                if (project != null)
                {
                    // Retrieve projectDirectory only the first time. It never changes anyway.
                    var projectDirectory = Path.GetDirectoryName(projectPath);
                    var projectName = project.ItemSpec;

                    // If the user specified additional properties then add those
                    var projectProperties = project.GetMetadata("Properties");
                    if (!string.IsNullOrEmpty(projectProperties))
                    {
                        Hashtable additionalProjectPropertiesTable;
                        if (!PropertyParser.GetTableWithEscaping(Log, "AdditionalProperties", "AdditionalProperties", projectProperties.Split(';'), out additionalProjectPropertiesTable))
                        {
                            return false;
                        }

                        var combinedTable = new Hashtable(StringComparer.OrdinalIgnoreCase);

                        // First copy in the properties from the global table that not in the additional properties table
                        if (propertiesTable != null)
                        {
                            foreach (DictionaryEntry entry in propertiesTable)
                            {
                                if (!additionalProjectPropertiesTable.Contains(entry.Key))
                                {
                                    combinedTable.Add(entry.Key, entry.Value);
                                }
                            }
                        }

                        // Add all the additional properties
                        foreach (DictionaryEntry entry in additionalProjectPropertiesTable)
                        {
                            combinedTable.Add(entry.Key, entry.Value);
                        }

                        propertiesTable = combinedTable;
                    }

                    // If the user specified a different toolsVersion for this project - then override the setting
                    if (!string.IsNullOrEmpty(project.GetMetadata("ToolsVersion")))
                    {
                        toolsVersion = project.GetMetadata("ToolsVersion");
                    }
                }

                // Send the project off to the build engine. By passing in null to the
                // first param, we are indicating that the project to build is the same
                // as the *calling* project file.
                BuildEngineResult result =
                    BuildEngine3.BuildProjectFilesInParallel(
                        new[] { project.ItemSpec },
                        null,
                        new IDictionary[] { propertiesTable },
                        new IList<string>[] { new List<string>() },
                        new[] { toolsVersion },
                        false);

                return result.Result;
            }
            else
            {
                Log.LogError(
                    "MsBuild script file expected to be at '{0}' but could not be found",
                    projectPath);
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the steps that should be executed prior to each step.
        /// </summary>
        public ITaskItem[] PreSteps
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the steps that should be executed after each step.
        /// </summary>
        public ITaskItem[] PostSteps
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the properties for the steps.
        /// </summary>
        public ITaskItem[] Properties
        {
            get;
            set;
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catching, logging and letting MsBuild deal with the fall out")]
        private bool ShouldExecuteStep(IEnumerable<string> groupsToExecute, IEnumerable<string> stepGroups)
        {
            const string AlwaysExecuteGroup = "all";

            try
            {
                return !groupsToExecute.Any()
                    || groupsToExecute.Contains(AlwaysExecuteGroup)
                    || (groupsToExecute.Any() && groupsToExecute.Intersect(stepGroups).Any());
            }
            catch (Exception e)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Failed to determine if the collection contains any of the items. Error was: {0}",
                        e));
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the steps that should be taken for the current process.
        /// </summary>
        [Required]
        public ITaskItem[] Steps
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the process should stop on the first error or continue.
        /// Default is false.
        /// </summary>
        public bool StopOnFirstFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the version of MsBuild and the build tools that should be used.
        /// </summary>
        public string ToolsVersion
        {
            get;
            set;
        }
    }
}
