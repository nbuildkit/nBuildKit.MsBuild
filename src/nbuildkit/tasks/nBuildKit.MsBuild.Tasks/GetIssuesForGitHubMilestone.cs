//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that extracts all issues with a given state for a given milestone from GitHub.
    /// </summary>
    public sealed class GetIssuesForGitHubMilestone : NBuildKitMsBuildTask
    {
        private const string MetadataTitleTag = "Title";
        private const string MetadataUrlTag = "Url";
        private const string MetadataStateTag = "State";
        private const string MetadataBodyTag = "Body";
        private const string MetadataUserNameTag = "UserName";
        private const string MetadataUserUrlTag = "UserUrl";
        private const string MetadataLabelsTag = "Labels";

        private static WebClient CreateWebClient()
        {
            var userAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)";

            var client = new WebClient();
            client.Headers.Clear();
            client.Headers.Add("user-agent", userAgent);
            client.Headers["accept"] = "application/vnd.github.v3+json";

            return client;
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Catching to log. Letting MsBuild handle the rest.")]
        public override bool Execute()
        {
            var list = new SortedList<int, ITaskItem>();
            try
            {
                Log.LogMessage(
                            MessageImportance.Low,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Searching for milestone with title: {0}. ",
                                MilestoneName));

                var milestone = GetMilestone(MilestoneName);
                if (milestone != null)
                {
                    Log.LogMessage(
                            MessageImportance.Low,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Getting issues for milestone [{0}] - {1}. ",
                                milestone.number,
                                milestone.title));

                    var issues = GetIssuesForMilestone(milestone);
                    foreach (var issue in issues)
                    {
                        if (!list.ContainsKey(issue.number))
                        {
                            var newItem = new TaskItem(issue.number.ToString(CultureInfo.InvariantCulture));

                            newItem.SetMetadata(MetadataTitleTag, issue.title);
                            newItem.SetMetadata(MetadataUrlTag, issue.url);
                            newItem.SetMetadata(MetadataStateTag, issue.state);
                            newItem.SetMetadata(MetadataBodyTag, issue.body);
                            newItem.SetMetadata(MetadataUserNameTag, issue.assignee != null ? issue.assignee.login : string.Empty);
                            newItem.SetMetadata(MetadataUserUrlTag, issue.assignee != null ? issue.assignee.url : string.Empty);
                            newItem.SetMetadata(MetadataLabelsTag, string.Join(";", issue.labels.Select(l => l.name)));

                            list.Add(issue.number, newItem);
                        }
                    }
                }
                else
                {
                    Log.LogError(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Failed to find a milestone with title: {0}",
                            MilestoneName));
                }
            }
            catch (Exception e)
            {
                Log.LogError(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Failed to gather the issues for the given milestone on GitHub. Exception was: {0}",
                        e));
            }

            Issues = list.Values.ToArray();

            return !Log.HasLoggedErrors;
        }

        private List<Issue> GetIssuesForMilestone(Milestone milestone)
        {
            List<Issue> issues = null;
            using (var client = CreateWebClient())
            {
                var state = string.Empty;
                switch (IssueState)
                {
                    case "open":
                        state = "&state=open";
                        break;
                    case "closed":
                        state = "&state=closed";
                        break;
                    default:
                        state = "&state=all";
                        break;
                }

                var uri = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/issues?milestone={1}{2}",
                    GitHubApiUri(),
                    milestone.number,
                    state);

                Log.LogMessage(
                    MessageImportance.Low,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Getting issue information from: {0}. ",
                        uri));

                var content = client.DownloadString(uri);
                var serializer = new DataContractJsonSerializer(typeof(List<Issue>));

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(content)))
                {
                    issues = (List<Issue>)serializer.ReadObject(ms);
                }
            }

            return issues;
        }

        private Milestone GetMilestone(string name)
        {
            Milestone milestone = null;
            using (var client = CreateWebClient())
            {
                var uri = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/milestones",
                    GitHubApiUri());

                Log.LogMessage(
                    MessageImportance.Low,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Getting milestone information from: {0}. ",
                        uri));

                var content = client.DownloadString(uri);
                var serializer = new DataContractJsonSerializer(typeof(List<Milestone>));

                List<Milestone> milestones = null;
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(content)))
                {
                    milestones = (List<Milestone>)serializer.ReadObject(ms);
                }

                milestone = milestones.Find(m => m.title != null && m.title.Equals(name));
            }

            return milestone;
        }

        private string GitHubApiUri()
        {
            return string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.github.com/repos/{0}/{1}",
                    GitHubUserName,
                    GitHubProjectName);
        }

        /// <summary>
        /// Gets or sets the name of the project on GitHub.
        /// </summary>
        [Required]
        public string GitHubProjectName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the user on GitHub.
        /// </summary>
        [Required]
        public string GitHubUserName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the state of the issues which should be returned.
        /// </summary>
        [Required]
        public string IssueState
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collection containing all the issue information.
        /// </summary>
        [Output]
        public ITaskItem[] Issues
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the milestone name.
        /// </summary>
        [Required]
        public string MilestoneName
        {
            get;
            set;
        }

        [DataContract]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by the serializer.")]
        internal sealed class Assignee
        {
            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string login
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string avatar_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string gravatar_id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string html_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string followers_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string following_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string gists_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string starred_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string subscriptions_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string organizations_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string repos_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string received_events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string type
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public bool site_admin
            {
                get;
                set;
            }
        }

        [DataContract]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by the serializer.")]
        internal sealed class Creator
        {
            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string login
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string avatar_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string gravatar_id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string html_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string followers_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string following_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string gists_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string starred_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string subscriptions_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string organizations_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string repos_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string received_events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string type
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public bool site_admin
            {
                get;
                set;
            }
        }

        [DataContract]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by the serializer.")]
        internal sealed class Issue
        {
            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string repository_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string labels_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string comments_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string html_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int number
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string title
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public User user
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public List<Label> labels
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string state
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public bool locked
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public Assignee assignee
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public Milestone milestone
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int comments
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string created_at
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string updated_at
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string closed_at
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string body
            {
                get;
                set;
            }
        }

        [DataContract]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by the serializer.")]
        internal sealed class Label
        {
            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string name
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string color
            {
                get;
                set;
            }
        }

        [DataContract]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by the serializer.")]
        internal sealed class Milestone
        {
            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string html_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string labels_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int number
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string title
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string description
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public Creator creator
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int open_issues
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int closed_issues
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string state
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string created_at
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string updated_at
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public object due_on
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public object closed_at
            {
                get;
                set;
            }
        }

        [DataContract]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1812:AvoidUninstantiatedInternalClasses",
            Justification = "Instantiated by the serializer.")]
        internal sealed class User
        {
            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string login
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public int id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string avatar_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string gravatar_id
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string html_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string followers_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string following_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string gists_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string starred_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string subscriptions_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string organizations_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string repos_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string received_events_url
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public string type
            {
                get;
                set;
            }

            [DataMember]
            [SuppressMessage(
                "Microsoft.StyleCop.CSharp.NamingRules",
                "SA1300:ElementMustBeginWithUpperCaseLetter",
                Justification = "Used in a JSON datacontract. Should stay as is.")]
            public bool site_admin
            {
                get;
                set;
            }
        }
    }
}
