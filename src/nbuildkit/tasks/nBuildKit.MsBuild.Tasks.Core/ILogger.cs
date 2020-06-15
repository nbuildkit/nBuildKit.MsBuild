//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// Defines the interface for objects that log information.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs the specified error.
        /// </summary>
        /// <param name="format">The message format.</param>
        /// <param name="arguments">The message arguments.</param>
        void LogError(string format, params object[] arguments);

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="importance">The message importance.</param>
        /// <param name="format">The message format.</param>
        /// <param name="arguments">The message arguments.</param>
        void LogMessage(MessageImportance importance, string format, params object[] arguments);
    }
}
