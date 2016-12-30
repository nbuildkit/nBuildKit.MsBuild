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
using System.Text;
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
            const string MetadataTag = "PreSteps";
            var steps = step.GetMetadata(MetadataTag);
            return steps.ToLower(CultureInfo.InvariantCulture).Split(';').Select(s => new TaskItem(s)).ToArray();
        }

        private static ITaskItem[] LocalPostSteps(ITaskItem step)
        {
            const string MetadataTag = "PostSteps";
            var steps = step.GetMetadata(MetadataTag);
            return steps.ToLower(CultureInfo.InvariantCulture).Split(';').Select(s => new TaskItem(s)).ToArray();
        }

        private static IEnumerable<string> StepGroups(ITaskItem step)
        {
            const string MetadataTag = "Groups";
            var groups = step.GetMetadata(MetadataTag);
            return groups.ToLower(CultureInfo.InvariantCulture).Split(';');
        }

        private void AddStepMetadata(ITaskItem subStep, string stepPath, ITaskItem[] metadata)
        {
            const string MetadataTag = "Properties";

            var stepMetadata = GetStepMetadata(stepPath, metadata);

            var stepProperties = subStep.GetMetadata(MetadataTag);
            if (!string.IsNullOrEmpty(stepProperties))
            {
                Hashtable additionalProjectPropertiesTable = null;
                if (!PropertyParser.GetTableWithEscaping(Log, "AdditionalProperties", "AdditionalProperties", stepProperties.Split(';'), out additionalProjectPropertiesTable))
                {
                    // Ignore it ...
                }

                foreach (DictionaryEntry entry in additionalProjectPropertiesTable)
                {
                    if (!stepMetadata.ContainsKey(entry.Key))
                    {
                        stepMetadata.Add(entry.Key, entry.Value);
                    }
                }
            }

            // Turn the hashtable into a properties string again.
            var builder = new StringBuilder();
            foreach (DictionaryEntry entry in stepMetadata)
            {
                builder.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={1};",
                        entry.Key,
                        EscapingUtilities.UnescapeAll(entry.Value as string)));
            }

            subStep.SetMetadata(MetadataTag, builder.ToString());
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
            var hasFailed = false;
            var groups = Groups();
            foreach (var step in Steps)
            {
                try
                {
                    if (!ExecuteStep(step, groups))
                    {
                        hasFailed = true;
                        if (StopOnFirstFailure)
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    hasFailed = true;
                    Log.LogError(
                        "Execution of steps failed with exception. Exception was: {0}",
                        e);
                }
            }

            if (Log.HasLoggedErrors || hasFailed)
            {
                if (FailureSteps != null)
                {
                    foreach (var step in FailureSteps)
                    {
                        if (!string.IsNullOrEmpty(step.ItemSpec))
                        {
                            if (!ExecuteFailureStep(step, groups))
                            {
                                if (StopOnFirstFailure)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return !Log.HasLoggedErrors && !hasFailed;
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

            var stepPath = GetAbsolutePath(step.ItemSpec);
            if (PreSteps != null)
            {
                foreach (var globalPreStep in PreSteps)
                {
                    if (!string.IsNullOrEmpty(globalPreStep.ItemSpec))
                    {
                        AddStepMetadata(globalPreStep, stepPath, StepMetadata);
                        var result = InvokeBuildEngine(globalPreStep);
                        if (!result)
                        {
                            if (FailOnPreStepFailure)
                            {
                                Log.LogError(
                                    "Failed while executing global pre-step action from '{0}'",
                                    globalPreStep.ItemSpec);
                            }
                            else
                            {
                                Log.LogWarning(
                                    "Failed while executing global pre-step action from '{0}'",
                                    globalPreStep.ItemSpec);
                            }

                            if (StopOnPreStepFailure)
                            {
                                return false;
                            }
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
                        AddStepMetadata(localPreStep, stepPath, StepMetadata);
                        var result = InvokeBuildEngine(localPreStep);
                        if (!result)
                        {
                            if (FailOnPreStepFailure)
                            {
                                Log.LogError(
                                    "Failed while executing step specific pre-step action from '{0}'",
                                    localPreStep.ItemSpec);
                            }
                            else
                            {
                                Log.LogWarning(
                                    "Failed while executing step specific pre-step action from '{0}'",
                                    localPreStep.ItemSpec);
                            }

                            if (StopOnPreStepFailure)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            var stepResult = InvokeBuildEngine(step);
            if (!stepResult && StopOnFirstFailure)
            {
                return false;
            }

            var localPostSteps = LocalPostSteps(step);
            if (localPostSteps != null)
            {
                foreach (var localPostStep in localPostSteps)
                {
                    if (!string.IsNullOrEmpty(localPostStep.ItemSpec))
                    {
                        AddStepMetadata(localPostStep, stepPath, StepMetadata);
                        var result = InvokeBuildEngine(localPostStep);
                        if (!result)
                        {
                            if (FailOnPostStepFailure)
                            {
                                Log.LogError(
                                    "Failed while executing step specific post-step action from '{0}'",
                                    localPostStep.ItemSpec);
                            }
                            else
                            {
                                Log.LogWarning(
                                    "Failed while executing step specific post-step action from '{0}'",
                                    localPostStep.ItemSpec);
                            }

                            if (StopOnPostStepFailure)
                            {
                                return false;
                            }
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
                        AddStepMetadata(globalPostStep, stepPath, StepMetadata);
                        var result = InvokeBuildEngine(globalPostStep);
                        if (!result && StopOnFirstFailure)
                        {
                            if (FailOnPostStepFailure)
                            {
                                Log.LogError(
                                    "Failed while executing global post-step action from '{0}'",
                                    globalPostStep.ItemSpec);
                            }
                            else
                            {
                                Log.LogWarning(
                                    "Failed while executing global post-step action from '{0}'",
                                    globalPostStep.ItemSpec);
                            }

                            if (StopOnPostStepFailure)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return stepResult;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the process should fail if a pre-step fails
        /// </summary>
        public bool FailOnPreStepFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the process should fail if a post-step fails
        /// </summary>
        public bool FailOnPostStepFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the steps that should be taken if a step fails.
        /// </summary>
        public ITaskItem[] FailureSteps
        {
            get;
            set;
        }

        private Hashtable GetStepMetadata(string stepPath, ITaskItem[] metadata)
        {
            const string MetadataTagDescription = "Description";
            const string MetadataTagId = "Id";
            const string MetadataTagName = "Name";

            var stepFileName = Path.GetFileName(stepPath);
            var stepMetadata = metadata.FirstOrDefault(t => string.Equals(stepFileName, t.ItemSpec, StringComparison.OrdinalIgnoreCase));
            var result = new Hashtable(StringComparer.OrdinalIgnoreCase);

            var description = stepMetadata != null
                    ? stepMetadata.GetMetadata(MetadataTagDescription)
                    : string.Empty;
            result.Add("StepDescription", description);

            var id = (stepMetadata != null) && !string.IsNullOrEmpty(stepMetadata.GetMetadata(MetadataTagId))
                    ? stepMetadata.GetMetadata(MetadataTagId)
                    : stepFileName;
            result.Add("StepId", id);

            var name = (stepMetadata != null) && !string.IsNullOrEmpty(stepMetadata.GetMetadata(MetadataTagName))
                    ? stepMetadata.GetMetadata(MetadataTagName)
                    : stepFileName;
            result.Add("StepName", name);

            result.Add("StepPath", stepPath);

            return result;
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
        /// Gets or sets the collection containing the metadata describing the different steps.
        /// </summary>
        [Required]
        public ITaskItem[] StepMetadata
        {
            get;
            set;
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
        /// Gets or sets a value indicating whether the process should stop if a pre-step fails
        /// </summary>
        public bool StopOnPreStepFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the process should stop if a post-step fails
        /// </summary>
        public bool StopOnPostStepFailure
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
