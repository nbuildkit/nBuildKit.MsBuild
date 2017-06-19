//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NBuildKit.MsBuild.Tasks.Core;

namespace NBuildKit.MsBuild.Tasks.Web
{
    /// <summary>
    /// Defines a task that downloads one or more files from a remote server.
    /// </summary>
    public sealed class WebDownload : BaseTask
    {
        private const string ErrorIdFailed = "NBuildKit.WebDownload.Failed";
        private const string ErrorIdUrlInvalid = "NBuildKit.WebDownload.UrlInvalid";
        private const string ErrorIdUrlMissing = "NBuildKit.WebDownload.UrlMissing";

        /// <summary>
        /// Gets or sets the directory into which the file should be placed.
        /// </summary>
        [Required]
        public ITaskItem DestinationDirectory
        {
            get;
            set;
        }

        /// <inheritdoc/>
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

            Uri source = null;
            try
            {
                source = new Uri(Source.ItemSpec);
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
                    "The provided URL is not valid. The error was: {0}",
                    e);
                return false;
            }

            if (DestinationDirectory == null)
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdDirectoryNotFound),
                    ErrorIdDirectoryNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No output directory provided");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DestinationDirectory.ItemSpec))
            {
                Log.LogError(
                    string.Empty,
                    ErrorCodeById(ErrorIdDirectoryNotFound),
                    ErrorIdDirectoryNotFound,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    "No output directory provided");
                return false;
            }

            var destinationDirectory = GetAbsolutePath(DestinationDirectory);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            var fileName = !string.IsNullOrWhiteSpace(Name) ? Name : Path.GetFileName(source.AbsolutePath);
            var targetPath = Path.Combine(destinationDirectory, fileName);

            // Make sure that we can establish secure connections. See here: https://stackoverflow.com/a/37572417/539846
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            using (var client = new WebClient())
            {
                client.Credentials = GetConfiguredCredentials();
                try
                {
                    Log.LogMessage(
                        MessageImportance.Normal,
                        "Downloading from: {0}. To: {1}",
                        source,
                        targetPath);
                    client.DownloadFile(source, targetPath);
                    OutputPath = new TaskItem(targetPath);
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
                        "Failed to download a file from the url: {0}. The error was: {1}",
                        source,
                        e);
                }
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Determines which credentials to pass with the web request
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
        /// Gets or sets the optional name of the file in the local file system.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the location of the downloaded file on the local file system.
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
        /// Gets or sets the URL from which the file should be downloaded.
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
