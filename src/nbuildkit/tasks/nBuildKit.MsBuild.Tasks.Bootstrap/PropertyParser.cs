//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks.Bootstrap
{
    /// <summary>
    /// Defines helper methods for parsing properties from strings.
    /// </summary>
    /// <remarks>
    /// Taken from here: https://github.com/Microsoft/msbuild/blob/master/src/Shared/PropertyParser.cs because it is not
    /// available in MsBuild due to it being internal.
    /// </remarks>
    internal static class PropertyParser
    {
        /// <summary>
        /// Given a string of semi-colon delimited name=value pairs, this method parses it and creates
        /// a hash table containing the property names as keys and the property values as values.
        /// This method escapes any special characters found in the property values, in case they
        /// are going to be passed to a method (such as that expects the appropriate escaping to have happened
        /// already.
        /// </summary>
        /// <param name="log">The logger.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="syntaxName">The name of the syntax element.</param>
        /// <param name="propertyNameValueStrings">The list of properties.</param>
        /// <param name="finalPropertiesTable">The resulting hashtable containing the properties and their values.</param>
        /// <returns>true on success, false on failure.</returns>
        internal static bool GetTableWithEscaping(
            TaskLoggingHelper log,
            string parameterName,
            string syntaxName,
            string[] propertyNameValueStrings,
            out Hashtable finalPropertiesTable)
        {
            finalPropertiesTable = null;

            if (propertyNameValueStrings != null)
            {
                finalPropertiesTable = new Hashtable(StringComparer.OrdinalIgnoreCase);
                List<PropertyNameValuePair> finalPropertiesList = new List<PropertyNameValuePair>();

                // Loop through the array.  Each string in the array should be of the form:
                //          MyPropName=MyPropValue
                foreach (string propertyNameValueString in propertyNameValueStrings)
                {
                    // Find the first '=' sign in the string.
                    int indexOfEqualsSign = propertyNameValueString.IndexOf('=');

                    if (indexOfEqualsSign != -1)
                    {
                        // If we found one, then grab the stuff before it and put it into "propertyName",
                        // and grab the stuff after it and put it into "propertyValue". But trim the
                        // whitespace from beginning and end of both name and value. When authoring a
                        // project/targets file, people like to use whitespace and newlines to pretty up
                        // the file format.
                        string propertyName = propertyNameValueString.Substring(0, indexOfEqualsSign).Trim();
                        string propertyValue = EscapingUtilities.Escape(propertyNameValueString.Substring(indexOfEqualsSign + 1).Trim());

                        // Make sure we have a property name and property value (though the value is allowed to be blank).
                        if (propertyName.Length == 0)
                        {
                            // No property name?  That's no good to us.
                            if (log != null)
                            {
                                log.LogErrorWithCodeFromResources("General.InvalidPropertyError", syntaxName, propertyNameValueString);
                            }

                            return false;
                        }

                        // Store the property in our list.
                        finalPropertiesList.Add(new PropertyNameValuePair(propertyName, propertyValue));
                    }
                    else
                    {
                        // There's no '=' sign in the string.  When this happens, we treat this string as basically
                        // an appendage on the value of the previous property.  For example, if the project file contains
                        //
                        //      <PropertyGroup>
                        //          <WarningsAsErrors>1234;5678;9999</WarningsAsErrors>
                        //      </PropertyGroup>
                        //      <Target Name="Build">
                        //          <MSBuild Projects="ConsoleApplication1.csproj"
                        //                   Properties="WarningsAsErrors=$(WarningsAsErrors)"/>
                        //      </Target>
                        //
                        // , then this method (GetTableWithEscaping) will see this:
                        //
                        //      propertyNameValueStrings[0] = "WarningsAsErrors=1234"
                        //      propertyNameValueStrings[1] = "5678"
                        //      propertyNameValueStrings[2] = "9999"
                        //
                        // And what we actually want to end up with in our final hashtable is this:
                        //
                        //      NAME                    VALUE
                        //      ===================     ================================
                        //      WarningsAsErrors        1234;5678;9999
                        if (finalPropertiesList.Count > 0)
                        {
                            // There was a property definition previous to this one.  Append the current string
                            // to that previous value, using semicolon as a separator.
                            string propertyValue = EscapingUtilities.Escape(propertyNameValueString.Trim());
                            finalPropertiesList[finalPropertiesList.Count - 1].Value.Append(';');
                            finalPropertiesList[finalPropertiesList.Count - 1].Value.Append(propertyValue);
                        }
                        else
                        {
                            // No equals sign in the very first property?  That's a problem.
                            if (log != null)
                            {
                                log.LogErrorWithCodeFromResources("General.InvalidPropertyError", syntaxName, propertyNameValueString);
                            }

                            return false;
                        }
                    }
                }

                // Convert the data in the List to a Hashtable, because that's what the MSBuild task eventually
                // needs to pass onto the engine.
                if (log != null)
                {
                    log.LogMessageFromText(parameterName, MessageImportance.Low);
                }

                foreach (PropertyNameValuePair propertyNameValuePair in finalPropertiesList)
                {
                    string propertyValue = propertyNameValuePair.Value.ToString();
                    finalPropertiesTable[propertyNameValuePair.Name] = propertyValue;
                    if (log != null)
                    {
                        log.LogMessageFromText(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "  {0}={1}",
                                propertyNameValuePair.Name,
                                propertyValue),
                            MessageImportance.Low);
                    }
                }
            }

            return true;
        }

        private class PropertyNameValuePair
        {
            internal string Name
            {
                get;
                set;
            }

            internal StringBuilder Value
            {
                get;
                set;
            }

            internal PropertyNameValuePair(string propertyName, string propertyValue)
            {
                Name = propertyName;

                Value = new StringBuilder();
                Value.Append(propertyValue);
            }
        }
    }
}
