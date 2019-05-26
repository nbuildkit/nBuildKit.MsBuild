//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using Flurl;
using Microsoft.Build.Framework;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Web
{
    /// <summary>
    /// Defines a task that deletes one or more files from a remote server.
    /// </summary>
    public sealed class WebDelete : BaseTask
    {
        private const string ErrorIdFailed = "NBuildKit.WebDelete.Failed";
        private const string ErrorIdUrlInvalid = "NBuildKit.WebDelete.UrlInvalid";
        private const string ErrorIdUrlMissing = "NBuildKit.WebDelete.UrlMissing";
        private const string ErrorIdNoFiles = "NBuildKit.WebDelete.NoFiles";

        private readonly Func<IInternalWebClient> _webClientBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDelete"/> class.
        /// </summary>
        public WebDelete()
            : this(() => new InternalWebClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDelete"/> class.
        /// </summary>
        /// <param name="builder">The function that creates <see cref="IInternalWebClient"/> instances.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public WebDelete(Func<IInternalWebClient> builder)
        {
            if (ReferenceEquals(builder, null))
            {
                throw new ArgumentNullException(nameof(builder));
            }

            _webClientBuilder = builder;
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Microsoft.Usage",
            "CA2234:PassSystemUriObjectsInsteadOfStrings",
            Justification = "Cannot turn a file path into a URI.")]
        public override bool Execute()
        {
            if (Source == null)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdUrlMissing),
                    ErrorIdUrlMissing,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No URL was provided");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Source.ItemSpec))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdUrlMissing),
                    ErrorIdUrlMissing,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No URL was provided");
                return false;
            }

            Uri baseUri = null;
            try
            {
                baseUri = new Uri(Source.ItemSpec);
            }
            catch (UriFormatException e)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdUrlInvalid),
                    ErrorIdUrlInvalid,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "The provided URL '{0}' is not valid. The error was: {1}",
                    Source.ItemSpec,
                    e);
                return false;
            }

            if ((Items == null) || (Items.Length == 0))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdNoFiles),
                    ErrorIdNoFiles,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No files to delete provided");
                return false;
            }

            // Make sure that we can establish secure connections. See here: https://stackoverflow.com/a/37572417/539846
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            using (var client = _webClientBuilder())
            {
                if (ReferenceEquals(client, null))
                {
                    Log.LogError(
                        string.Empty,
                        ErrorCodeById(ErrorIdFailed),
                        ErrorIdFailed,
                        string.Empty,
                        0,
                        0,
                        0,
                        0,
                        "Failed to create a new client instance.");
                    return false;
                }

                client.Credentials = GetConfiguredCredentials();
                foreach (var item in Items)
                {
                    if ((item == null) || string.IsNullOrWhiteSpace(item.ItemSpec))
                    {
                        continue;
                    }

                    var itemPath = GetAbsolutePath(item);
                    var targetUri = new Uri(Url.Combine(baseUri.ToString(), Path.GetFileName(itemPath)));
                    try
                    {
                        Log.LogMessage(
                            MessageImportance.Normal,
                            "Deleting from: {0}",
                            targetUri);
                        var response = client.DeleteFile(targetUri);
                        var responseText = System.Text.Encoding.ASCII.GetString(response);
                        Log.LogMessage(
                            MessageImportance.Normal,
                            "Server response: {0}",
                            responseText);
                    }
                    catch (WebException e)
                    {
                        Log.LogError(
                            string.Empty,
                            ErrorCodeById(ErrorIdFailed),
                            ErrorIdFailed,
                            string.Empty,
                            0,
                            0,
                            0,
                            0,
                            "Failed to delete a file from the url: {0}. The error was: {1}",
                            baseUri,
                            e);
                    }
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Determines which credentials to pass with the web request.
        /// </summary>
        /// <returns>The credentials that should be passed to the HTTP resource.</returns>
        private ICredentials GetConfiguredCredentials()
        {
            if (!string.IsNullOrWhiteSpace(UserName))
            {
                return new NetworkCredential(UserName, Password);
            }

            if (UseDefaultCredentials)
            {
                return CredentialCache.DefaultCredentials;
            }

            return null;
        }

        /// <summary>
        /// Gets or sets the items that should be uploaded.
        /// </summary>
        [Required]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "MsBuild does not understand collections")]
        public ITaskItem[] Items
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the location of the file on the local file system that should be uploaded.
        /// </summary>
        [Output]
        public ITaskItem OutputPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the password that should be used to access the HTTP resource.
        /// </summary>
        public string Password
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the URL to which the file should be uploaded.
        /// </summary>
        [Required]
        public ITaskItem Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the default credentials should be passed to the HTTP resource.
        /// </summary>
        public bool UseDefaultCredentials
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the username that should be used to access the HTTP resource.
        /// </summary>
        public string UserName
        {
            get;
            set;
        }
    }
}
