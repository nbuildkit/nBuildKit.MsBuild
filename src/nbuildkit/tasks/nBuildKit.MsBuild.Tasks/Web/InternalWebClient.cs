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
    internal class InternalWebClient : WebClient, IInternalWebClient, IDisposable
    {
        private readonly bool _forUploads;

        public InternalWebClient(bool forUploads = false)
        {
            _forUploads = forUploads;
        }

        /// <summary>
        /// Deletes the specific file from the remote file server.
        /// </summary>
        /// <param name="address">The URI of the resource that should be removed. For example http://localhost/samplefile.txt.</param>
        /// <returns>The response of the server.</returns>
        public byte[] DeleteFile(Uri address)
        {
            return UploadData(address, "DELETE", Array.Empty<byte>());
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var webRequest = base.GetWebRequest(uri);
            webRequest.Timeout = 20 * 60 * 1000;

            var httpRequest = webRequest as HttpWebRequest;
            if ((httpRequest != null) && _forUploads)
            {
                httpRequest.SendChunked = true;
                httpRequest.AllowWriteStreamBuffering = false;
            }

            return webRequest;
        }
    }
}
