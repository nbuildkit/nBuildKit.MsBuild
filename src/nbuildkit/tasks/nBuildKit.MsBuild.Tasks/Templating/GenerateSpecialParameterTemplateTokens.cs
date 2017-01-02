//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Templating
{
    /// <summary>
    /// Defines a <see cref="ITask"/> which takes user and system defined template tokens that consist of template tokens themselves
    /// and creates a property file for them.
    /// </summary>
    public sealed class GenerateSpecialParameterTemplateTokens : NBuildKitMsBuildTask
    {
        private static void GeneratePropertyFile(string path, IDictionary<string, string> tokens)
        {
            var lines = new List<string>();
            {
                lines.Add("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                lines.Add("<Project xmlns = \"http://schemas.microsoft.com/developer/msbuild/2003\" >");
                lines.Add("    <PropertyGroup>");
                lines.Add("        <ExistsGeneratedTemplateTokensSpecialParameters>true</ExistsGeneratedTemplateTokensSpecialParameters>");
                lines.Add("    </PropertyGroup>");
                lines.Add("    <ItemGroup>");

                foreach (var pair in tokens)
                {
                    lines.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "        <TemplateTokens Include=\"{0}\"><ReplacementValue>{1}</ReplacementValue></TemplateTokens>",
                            pair.Key,
                            pair.Value));
                }

                lines.Add("    </ItemGroup>");
                lines.Add("</Project>");
            }

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        private static string ReplaceTokens(string template, Regex regex, IDictionary<string, string> tokenPairs)
        {
            return regex.Replace(
                template,
                m =>
                {
                    var output = m.Value;
                    if (tokenPairs.ContainsKey(m.Groups[2].Value))
                    {
                        output = tokenPairs[m.Groups[2].Value];
                    }
                    return output;
                });
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            const string MetadataReplacementValueTag = "ReplacementValue";
            const string MetadataTemplateTag = "Template";

            var tokenPairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Tokens != null)
            {
                ITaskItem[] processedTokens = Tokens;
                for (int i = 0; i < processedTokens.Length; i++)
                {
                    ITaskItem taskItem = processedTokens[i];
                    if (!string.IsNullOrEmpty(taskItem.ItemSpec))
                    {
                        tokenPairs.Add(taskItem.ItemSpec, taskItem.GetMetadata(MetadataReplacementValueTag));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(SearchExpression))
            {
                SearchExpression = "(?<token>\\$\\{(?<identifier>\\w*)\\})";
            }

            var regex = new Regex(
                SearchExpression,
                RegexOptions.IgnoreCase
                | RegexOptions.Multiline
                | RegexOptions.Compiled
                | RegexOptions.Singleline);

            var mergedTokens = new Dictionary<string, string>();
            if (UserParameters != null)
            {
                foreach (var item in UserParameters)
                {
                    if (!mergedTokens.ContainsKey(item.ItemSpec))
                    {
                        var template = item.GetMetadata(MetadataTemplateTag);
                        var replacementValue = ReplaceTokens(template, regex, tokenPairs);

                        mergedTokens.Add(item.ItemSpec, replacementValue);
                    }
                }
            }

            if (SystemParameters != null)
            {
                foreach (var item in SystemParameters)
                {
                    if (!mergedTokens.ContainsKey(item.ItemSpec))
                    {
                        var template = item.GetMetadata(MetadataTemplateTag);
                        var replacementValue = ReplaceTokens(template, regex, tokenPairs);

                        mergedTokens.Add(item.ItemSpec, replacementValue);
                    }
                }
            }

            GeneratePropertyFile(GetAbsolutePath(PropertyFile), mergedTokens);

            return true;
        }

        /// <summary>
        /// Gets or sets the full path of the property file that should be generated.
        /// </summary>
        [Required]
        public ITaskItem PropertyFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the regular expression that will be used to locate the template variables.
        /// </summary>
        public string SearchExpression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of items that should be de-tokenized as defined by the system.
        /// </summary>
        [Required]
        public ITaskItem[] SystemParameters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of replacement tokens.
        /// </summary>
        [Required]
        public ITaskItem[] Tokens
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection of items that should be de-tokenized as defined by the user.
        /// </summary>
        public ITaskItem[] UserParameters
        {
            get;
            set;
        }
    }
}
