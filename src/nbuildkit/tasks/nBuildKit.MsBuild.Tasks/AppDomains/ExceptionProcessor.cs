//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace NBuildKit.MsBuild.Tasks.AppDomains
{
    /// <summary>
    /// Defines the delegate used for actions that process unhandled exceptions.
    /// </summary>
    /// <param name="exception">The exception to process.</param>
    internal delegate void ExceptionProcessor(Exception exception);
}
