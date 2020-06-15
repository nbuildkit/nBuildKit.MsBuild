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

namespace NBuildKit.MsBuild.Tasks.Script
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that executes steps for nBuildKit.
    /// </summary>
    public sealed class InvokeSteps : BaseTask
    {
        private const string ErrorIdInvalidConfiguration = "NBuildKit.Steps.InvalidConfiguration";
        private const string ErrorIdInvalidDependencies = "NBuildKit.Steps.InvalidDependencies";
        private const string ErrorIdPostStepFailure = "NBuildKit.Steps.PostStep";
        private const string ErrorIdPreStepFailure = "NBuildKit.Steps.PreStep";
        private const string ErrorIdStepFailure = "NBuildKit.Steps.Failure";

        private const string StepMetadataDescription = "StepDescription";
        private const string StepMetadataId = "StepId";
        private const string StepMetadataName = "StepName";
        private const string StepMetadataPath = "StepPath";

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
            result.Add(StepMetadataDescription, description);

            var id = (stepMetadata != null) && !string.IsNullOrEmpty(stepMetadata.GetMetadata(MetadataTagId))
                    ? stepMetadata.GetMetadata(MetadataTagId)
                    : stepFileName;
            result.Add(StepMetadataId, id);

            var name = (stepMetadata != null) && !string.IsNullOrEmpty(stepMetadata.GetMetadata(MetadataTagName))
                    ? stepMetadata.GetMetadata(MetadataTagName)
                    : stepFileName;
            result.Add(StepMetadataName, name);

            result.Add(StepMetadataPath, stepPath);

            result.Add("IsFirstStep", isFirst.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));

            result.Add("IsLastStep", isLast.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture));

            return result;
        }

        private static StepId[] ExecuteAfter(ITaskItem step)
        {
            const string MetadataTag = "ExecuteAfter";

            var idsText = (step != null) && !string.IsNullOrEmpty(step.GetMetadata(MetadataTag))
                    ? step.GetMetadata(MetadataTag)
                    : string.Empty;
            var ids = idsText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            return ids.Select(id => new StepId(id)).ToArray();
        }

        private static StepId[] ExecuteBefore(ITaskItem step)
        {
            const string MetadataTag = "ExecuteBefore";

            var idsText = (step != null) && !string.IsNullOrEmpty(step.GetMetadata(MetadataTag))
                    ? step.GetMetadata(MetadataTag)
                    : string.Empty;
            var ids = idsText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            return ids.Select(id => new StepId(id)).ToArray();
        }

        private static ITaskItem[] LocalPreSteps(ITaskItem step)
        {
            const string MetadataTag = "PreSteps";
            var steps = step.GetMetadata(MetadataTag);
            return steps.Split(';').Select(s => new TaskItem(s)).ToArray();
        }

        private static ITaskItem[] LocalPostSteps(ITaskItem step)
        {
            const string MetadataTag = "PostSteps";
            var steps = step.GetMetadata(MetadataTag);
            return steps.Split(';').Select(s => new TaskItem(s)).ToArray();
        }

        private static IEnumerable<string> StepGroups(ITaskItem step)
        {
            const string MetadataTag = "Groups";
            var groups = step.GetMetadata(MetadataTag);
            return groups.ToLower(CultureInfo.InvariantCulture).Split(';');
        }

        private static StepId StepId(ITaskItem step)
        {
            const string MetadataTagId = "Id";

            var id = (step != null) && !string.IsNullOrEmpty(step.GetMetadata(MetadataTagId))
                    ? step.GetMetadata(MetadataTagId)
                    : step.ItemSpec;
            return new StepId(id);
        }

        private void AddStepMetadata(ITaskItem subStep, string stepPath, ITaskItem[] metadata, bool isFirst, bool isLast)
        {
            const string MetadataTag = "Properties";

            var stepMetadata = GetStepMetadata(stepPath, metadata, isFirst, isLast);

            var stepProperties = subStep.GetMetadata(MetadataTag);
            if (!string.IsNullOrEmpty(stepProperties))
            {
                Hashtable additionalProjectPropertiesTable = null;
                if (!PropertyParser.GetTableWithEscaping(new MsBuildLogger(Log), "AdditionalProperties", "AdditionalProperties", stepProperties.Split(';'), out additionalProjectPropertiesTable))
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

            var orderedStepsToExecute = OrderSteps(stepsToExecute);
            if (orderedStepsToExecute == null)
            {
                return false;
            }

            Log.LogMessage(
                MessageImportance.Normal,
                "Executing steps in the following order: ");
            for (int i = 0; i < orderedStepsToExecute.Count; i++)
            {
                var step = orderedStepsToExecute[i];
                var metadata = GetStepMetadata(step.ItemSpec, StepMetadata, false, false);

                Log.LogMessage(
                    MessageImportance.Normal,
                    "{0} - {1}: {2}",
                    i,
                    metadata.ContainsKey(StepMetadataName) ? ((string)metadata[StepMetadataName]).Trim() : step.ItemSpec,
                    metadata.ContainsKey(StepMetadataDescription) ? ((string)metadata[StepMetadataDescription]).Trim() : string.Empty);
            }

            for (int i = 0; i < orderedStepsToExecute.Count; i++)
            {
                var step = orderedStepsToExecute[i];
                try
                {
                    if (!ExecuteStep(step, i == 0, i == orderedStepsToExecute.Count - 1))
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
                        string.Empty,
                        ErrorCodeById(ErrorIdStepFailure),
                        ErrorIdStepFailure,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
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
                                    string.Empty,
                                    ErrorCodeById(ErrorIdPreStepFailure),
                                    ErrorIdPreStepFailure,
                                    string.Empty,
                                    0,
                                    0,
                                    0,
                                    0,
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
                                    string.Empty,
                                    ErrorCodeById(ErrorIdPreStepFailure),
                                    ErrorIdPreStepFailure,
                                    string.Empty,
                                    0,
                                    0,
                                    0,
                                    0,
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
                                    string.Empty,
                                    ErrorCodeById(ErrorIdPostStepFailure),
                                    ErrorIdPostStepFailure,
                                    string.Empty,
                                    0,
                                    0,
                                    0,
                                    0,
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
                                    string.Empty,
                                    ErrorCodeById(ErrorIdPostStepFailure),
                                    ErrorIdPostStepFailure,
                                    string.Empty,
                                    0,
                                    0,
                                    0,
                                    0,
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
        /// Gets or sets a value indicating whether the process should fail if a post-step fails.
        /// </summary>
        public bool FailOnPostStepFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the process should fail if a pre-step fails.
        /// </summary>
        public bool FailOnPreStepFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the steps that should be taken if a step fails.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
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
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] GroupsToExecute
        {
            get;
            set;
        }

        private bool InvokeBuildEngine(ITaskItem project)
        {
            Hashtable propertiesTable;
            if (!PropertyParser.GetTableWithEscaping(new MsBuildLogger(Log), "GlobalProperties", "Properties", Properties.Select(t => t.ItemSpec).ToArray(), out propertiesTable))
            {
                return false;
            }

            string projectPath = GetAbsolutePath(project.ItemSpec);
            if (File.Exists(projectPath))
            {
                var toolsVersion = ToolsVersion;
                if (project != null)
                {
                    // If the user specified additional properties then add those
                    var projectProperties = project.GetMetadata("Properties");
                    if (!string.IsNullOrEmpty(projectProperties))
                    {
                        Hashtable additionalProjectPropertiesTable;
                        if (!PropertyParser.GetTableWithEscaping(new MsBuildLogger(Log), "AdditionalProperties", "AdditionalProperties", projectProperties.Split(';'), out additionalProjectPropertiesTable))
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
                        new[] { projectPath },
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
                    string.Empty,
                    ErrorCodeById(Core.ErrorInformation.ErrorIdFileNotFound),
                    Core.ErrorInformation.ErrorIdFileNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "MsBuild script file expected to be at '{0}' but could not be found",
                    projectPath);
                return false;
            }
        }

        private List<ITaskItem> OrderSteps(IEnumerable<ITaskItem> steps)
        {
            var sortedItems = new List<Tuple<StepId, ITaskItem>>();
            int IndexOf(StepId id)
            {
                return sortedItems.FindIndex(t => t.Item1.Equals(id));
            }

            var unsortedItems = new List<Tuple<StepId, StepId[], StepId[], ITaskItem>>();
            foreach (var item in steps)
            {
                var id = StepId(item);
                var executeAfter = ExecuteAfter(item);
                var executeBefore = ExecuteBefore(item);

                if ((executeBefore.Length > 0) || (executeAfter.Length > 0))
                {
                    unsortedItems.Add(Tuple.Create(id, executeBefore, executeAfter, item));
                }
                else
                {
                    sortedItems.Add(Tuple.Create(id, item));
                }
            }

            var lastCount = steps.Count();
            while ((unsortedItems.Count > 0) && (lastCount > unsortedItems.Count))
            {
                lastCount = unsortedItems.Count;

                var toDelete = new List<Tuple<StepId, StepId[], StepId[], ITaskItem>>();
                foreach (var unsortedItem in unsortedItems)
                {
                    var insertBefore = -1;
                    var executeBefore = unsortedItem.Item2;
                    if (executeBefore.Length > 0)
                    {
                        var indices = executeBefore.Select(id => IndexOf(id)).Where(i => i > -1);
                        if (indices.Count() != executeBefore.Length)
                        {
                            continue;
                        }

                        insertBefore = indices.Min();
                    }

                    var insertAfter = sortedItems.Count;
                    var executeAfter = unsortedItem.Item3;
                    if (executeAfter.Length > 0)
                    {
                        var indices = executeAfter.Select(id => IndexOf(id)).Where(i => i > -1);
                        if (indices.Count() != executeAfter.Length)
                        {
                            continue;
                        }

                        insertAfter = indices.Max();
                    }

                    if ((executeBefore.Length > 0) && (executeAfter.Length > 0) && (insertBefore < insertAfter))
                    {
                        Log.LogError(
                           string.Empty,
                           ErrorCodeById(ErrorIdInvalidDependencies),
                           ErrorIdInvalidDependencies,
                           string.Empty,
                           0,
                           0,
                           0,
                           0,
                           "At least one dependency needs to be inserted both before an earlier item and after a later item. No suitable place for the insertion could be found.");
                        return null;
                    }

                    if (executeBefore.Length > 0)
                    {
                        sortedItems.Insert(insertBefore, Tuple.Create(unsortedItem.Item1, unsortedItem.Item4));
                        toDelete.Add(unsortedItem);
                    }
                    else
                    {
                        sortedItems.Insert(insertAfter + 1, Tuple.Create(unsortedItem.Item1, unsortedItem.Item4));
                        toDelete.Add(unsortedItem);
                    }
                }

                foreach (var item in toDelete)
                {
                    unsortedItems.Remove(item);
                }
            }

            if (unsortedItems.Count > 0)
            {
                Log.LogMessage(
                    MessageImportance.Normal,
                    "Failed to sort all the steps. The sorted steps were: ");
                for (int i = 0; i < sortedItems.Count; i++)
                {
                    var tuple = sortedItems[i];
                    Log.LogMessage(
                        MessageImportance.Normal,
                        "{0} - {1}",
                        i,
                        tuple.Item1);
                }

                Log.LogMessage(
                    MessageImportance.Normal,
                    "The unsorted steps were: ");
                for (int i = 0; i < unsortedItems.Count; i++)
                {
                    var tuple = unsortedItems[i];
                    Log.LogMessage(
                        MessageImportance.Normal,
                        "{0} - {1}",
                        i,
                        tuple.Item1);
                }

                Log.LogError(
                   string.Empty,
                   ErrorCodeById(ErrorIdInvalidDependencies),
                   ErrorIdInvalidDependencies,
                   string.Empty,
                   0,
                   0,
                   0,
                   0,
                   "Was not able to order all the steps.");
                return null;
            }

            return sortedItems.Select(t => t.Item2).ToList();
        }

        /// <summary>
        /// Gets or sets the steps that should be executed after each step.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] PostSteps
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the steps that should be executed prior to each step.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] PreSteps
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the steps that should be taken for the current process.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Projects
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the properties for the steps.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
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
                    string.Empty,
                    ErrorCodeById(ErrorIdInvalidConfiguration),
                    ErrorIdInvalidConfiguration,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "Failed to determine if the collection contains any of the items. Error was: {0}",
                    e);
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the collection containing the metadata describing the different steps.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] StepMetadata
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
        /// Gets or sets a value indicating whether the process should stop if a pre-step fails.
        /// </summary>
        public bool StopOnPreStepFailure
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the process should stop if a post-step fails.
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
