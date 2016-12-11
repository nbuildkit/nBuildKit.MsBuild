//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that validates an XML file against an XSD schema file.
    /// </summary>
    public sealed class ValidateXmlAgainstSchema : NBuildKitMsBuildTask
    {
        /// <inheritdoc/>
        public override bool Execute()
        {
            // Set the validation settings.
            var settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;

            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

            settings.Schemas.Add(TargetNamespace, GetAbsolutePath(SchemaFile));

            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

            // Create the XmlReader object.
            Log.LogWarning(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Validating XML in {0} for target namespace '{1}' with schema from '{2}'",
                    InputFile,
                    TargetNamespace,
                    SchemaFile));
            using (XmlReader reader = XmlReader.Create(GetAbsolutePath(InputFile), settings))
            {
                while (reader.Read())
                {
                    // Just reading the file to check for validation errors ...
                }
            }

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the XML file that should be checked.
        /// </summary>
        [Required]
        public ITaskItem InputFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the XSD file.
        /// </summary>
        [Required]
        public ITaskItem SchemaFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the target XML namespace
        /// </summary>
        [Required]
        public string TargetNamespace
        {
            get;
            set;
        }

        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                Log.LogWarning(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "XML validation {0}: {1}",
                        args.Severity,
                        args.Message));
            }
            else
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "XML validation {0}: {1}",
                        args.Severity,
                        args.Message));
            }
        }
    }
}
