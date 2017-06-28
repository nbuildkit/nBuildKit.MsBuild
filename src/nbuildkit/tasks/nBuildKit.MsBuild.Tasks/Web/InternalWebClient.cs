//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Net;

namespace NBuildKit.MsBuild.Tasks.Web
{
    /// <summary>
    /// Defines a proxy for <see cref="WebClient"/> methods.
    /// </summary>
    internal class InternalWebClient : IInternalWebClient, IDisposable
    {
        private readonly WebClient _webClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalWebClient"/> class.
        /// </summary>
        /// <param name="webClient">The <see cref="WebClient"/> to which method calls will be forwarded.</param>
        public InternalWebClient(WebClient webClient)
        {
            _webClient = webClient;
        }

        /// <summary>
        /// Gets or sets the network credentials that are sent to the host and used to authenticate the request.
        /// </summary>
        public ICredentials Credentials
        {
            get => _webClient.Credentials;
            set => _webClient.Credentials = value;
        }

        public void Dispose()
        {
            if (_webClient != null)
            {
                _webClient.Dispose();
            }
        }

        /// <summary>
        /// Downloads the resource with the specified URI to a local file.
        /// </summary>
        /// <param name="address">The URI from which to download data.</param>
        /// <param name="fileName">The name of the local file that is to receive the data.</param>
        public void DownloadFile(Uri address, string fileName)
        {
            _webClient.DownloadFile(address, fileName);
        }

        /// <summary>
        /// Uploads the specified local file to a resource with the specified URI.
        /// </summary>
        /// <param name="address">The URI of the resource to receive the file. For example, ftp://localhost/samplefile.txt.</param>
        /// <param name="method">The method that should be used for the upload.</param>
        /// <param name="fileName">The file to send to the resource. For example, "samplefile.txt".</param>
        public void UploadFile(Uri address, string method, string fileName)
        {
            _webClient.UploadFile(address, method, fileName);
        }
    }
}
