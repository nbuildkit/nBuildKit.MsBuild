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
    /// Defines the interface for object that proxy <see cref="WebClient"/> calls.
    /// </summary>
    public interface IInternalWebClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the network credentials that are sent to the host and used to authenticate the request.
        /// </summary>
        ICredentials Credentials { get; set; }

        /// <summary>
        /// Downloads the resource with the specified URI to a local file.
        /// </summary>
        /// <param name="address">The URI from which to download data.</param>
        /// <param name="fileName">The name of the local file that is to receive the data.</param>
        void DownloadFile(Uri address, string fileName);

        /// <summary>
        /// Deletes the specific file from the remote file server.
        /// </summary>
        /// <param name="address">The URI of the resource that should be removed. For example http://localhost/samplefile.txt.</param>
        /// <returns>The response of the server.</returns>
        byte[] DeleteFile(Uri address);

        /// <summary>
        /// Uploads the specified local file to a resource with the specified URI.
        /// </summary>
        /// <param name="address">The URI of the resource to receive the file. For example, ftp://localhost/samplefile.txt.</param>
        /// <param name="method">The method that should be used for the upload.</param>
        /// <param name="fileName">The file to send to the resource. For example, "samplefile.txt".</param>
        /// <returns>The response of the server.</returns>
        byte[] UploadFile(Uri address, string method, string fileName);
    }
}
