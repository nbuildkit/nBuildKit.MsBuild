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
        protected override WebRequest GetWebRequest(Uri uri)
        {
            var webRequest = base.GetWebRequest(uri);
            webRequest.Timeout = 20 * 60 * 1000;

            var httpRequest = webRequest as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.SendChunked = true;
                httpRequest.AllowWriteStreamBuffering = false;
            }

            return webRequest;
        }
    }
}
