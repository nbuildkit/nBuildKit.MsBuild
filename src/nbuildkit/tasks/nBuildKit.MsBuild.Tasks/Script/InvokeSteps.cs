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
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that executes steps for nBuildKit.
    /// </summary>
    public sealed class InvokeSteps : MsBuildCommandLineToolTask
    {
        private static Hashtable GetStepMetadata(string stepPath, ITaskItem[] metadata, bool isFirst, bool isLast)
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

            result.Add("IsFirstStep", isFirst.ToString().ToLower(CultureInfo.InvariantCulture));

            result.Add("IsLastStep", isLast.ToString().ToLower(CultureInfo.InvariantCulture));

            return result;
        }

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

        private static string MetadataTableToString(Hashtable metadataTable)
        {
            var builder = new StringBuilder();
            foreach (DictionaryEntry entry in metadataTable)
            {
                builder.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={1};",
                        entry.Key,
                        EscapingUtilities.UnescapeAll(entry.Value as string)));
            }

            return builder.ToString();
        }

        private static IEnumerable<string> StepGroups(ITaskItem step)
        {
            const string MetadataTag = "Groups";
            var groups = step.GetMetadata(MetadataTag);
            return groups.ToLower(CultureInfo.InvariantCulture).Split(';').Select(s => s.Trim());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeSteps"/> class.
        /// </summary>
        public InvokeSteps()
        {
            ShowDetailedSummary = false;
        }

        private void AddStepMetadata(ITaskItem subStep, string stepPath, ITaskItem[] metadata, bool isFirst, bool isLast)
        {
            const string MetadataTag = "Properties";

            var stepMetadata = GetStepMetadata(stepPath, metadata, isFirst, isLast);

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

            subStep.SetMetadata(MetadataTag, MetadataTableToString(stepMetadata));
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catching, logging and letting MsBuild deal with the fall out")]
        public override bool Execute()
        {
            if ((Projects == null) || (Projects.Length == 0))
            {
                return true;
            }

            // Get groups and determine which steps should be executed
            var hasFailed = false;
            var groups = Groups().Select(s => s.ToLower(CultureInfo.InvariantCulture).Trim());

            var stepsToExecute = new List<ITaskItem>();
            foreach (var step in Projects)
            {
                var stepGroups = StepGroups(step);
                if (!ShouldExecuteStep(groups, stepGroups))
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        "Step {0} has tags {1} none of which are included in execution list of {2}.",
                        step.ItemSpec,
                        string.Join(", ", stepGroups),
                        string.Join(", ", groups));
                    continue;
                }

                stepsToExecute.Add(step);
            }

            for (int i = 0; i < stepsToExecute.Count; i++)
            {
                var step = stepsToExecute[i];
                try
                {
                    if (!ExecuteStep(step, i == 0, i == stepsToExecute.Count - 1))
                    {
                        hasFailed = true;
                        if (StopOnFirstFailure)
                        {
                            break;
                        }

                        // Create some additional space in the logs between the stages.
                        Log.LogMessage(string.Empty);
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
                    var failureStepsToExecute = new List<ITaskItem>();
                    foreach (var step in FailureSteps)
                    {
                        if (!string.IsNullOrEmpty(step.ItemSpec))
                        {
                            var stepGroups = StepGroups(step);
                            if (!ShouldExecuteStep(groups, stepGroups))
                            {
                                Log.LogMessage(
                                    MessageImportance.Low,
                                    "Step {0} has tags {1} none of which are included in execution list of {2}.",
                                    step.ItemSpec,
                                    string.Join(", ", stepGroups),
                                    string.Join(", ", groups));
                                continue;
                            }

                            failureStepsToExecute.Add(step);
                        }
                    }

                    for (int i = 0; i < failureStepsToExecute.Count; i++)
                    {
                        var step = failureStepsToExecute[i];
                        if (!ExecuteFailureStep(step))
                        {
                            if (StopOnFirstFailure)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return !Log.HasLoggedErrors && !hasFailed;
        }

        private bool ExecuteFailureStep(ITaskItem step)
        {
            var result = InvokeBuildEngine(step);
            if (!result && StopOnFirstFailure)
            {
                return false;
            }

            return true;
        }

        private bool ExecuteStep(ITaskItem step, bool isFirst, bool isLast)
        {
            var stepResult = true;
            var stepPath = GetAbsolutePath(step.ItemSpec);
            if (PreSteps != null)
            {
                foreach (var globalPreStep in PreSteps)
                {
                    if (!string.IsNullOrEmpty(globalPreStep.ItemSpec))
                    {
                        AddStepMetadata(globalPreStep, stepPath, StepMetadata, isFirst, isLast);
                        var result = InvokeBuildEngine(globalPreStep);
                        if (!result)
                        {
                            if (FailOnPreStepFailure)
                            {
                                stepResult = false;
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
                        AddStepMetadata(localPreStep, stepPath, StepMetadata, isFirst, isLast);
                        var result = InvokeBuildEngine(localPreStep);
                        if (!result)
                        {
                            if (FailOnPreStepFailure)
                            {
                                stepResult = false;
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

            // Get the result but always try to excecute the post-step actions
            var localStepResult = InvokeBuildEngine(step);
            stepResult = stepResult && localStepResult;

            var localPostSteps = LocalPostSteps(step);
            if (localPostSteps != null)
            {
                foreach (var localPostStep in localPostSteps)
                {
                    if (!string.IsNullOrEmpty(localPostStep.ItemSpec))
                    {
                        AddStepMetadata(localPostStep, stepPath, StepMetadata, isFirst, isLast || !stepResult);
                        var result = InvokeBuildEngine(localPostStep);
                        if (!result)
                        {
                            if (FailOnPostStepFailure)
                            {
                                stepResult = false;
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
                        AddStepMetadata(globalPostStep, stepPath, StepMetadata, isFirst, isLast || !stepResult);
                        var result = InvokeBuildEngine(globalPostStep);
                        if (!result && StopOnFirstFailure)
                        {
                            if (FailOnPostStepFailure)
                            {
                                stepResult = false;
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
                                stepResult = false;
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
                if (project != null)
                {
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
                var arguments = new List<string>();
                {
                    foreach (DictionaryEntry entry in propertiesTable)
                    {
                        arguments.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "/P:{0}=\"{1}\"",
                                entry.Key,
                                EscapingUtilities.UnescapeAll(entry.Value as string).TrimEnd(new[] { '\\' })));
                    }

                    arguments.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "\"{0}\"",
                            projectPath));
                }

                Log.LogMessage(
                    "Building project at: {0}",
                    projectPath);
                var exitCode = InvokeMsBuild(arguments);

                // Create some space in the logs between the invocations.
                Log.LogMessage(string.Empty);

                return exitCode == 0;
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
    }
}
