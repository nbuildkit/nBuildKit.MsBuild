//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;

namespace NBuildKit.MsBuild.Tasks.AppDomains
{
    /// <summary>
    /// An exception handler.
    /// </summary>
    /// <design>
    /// This class must be public because we use it in the AppDomainBuilder.
    /// </design>
    [Serializable]
    internal sealed class ExceptionHandler
    {
        /// <summary>
        /// The collection of loggers that must be notified if an exception happens.
        /// </summary>
        private readonly ExceptionProcessor[] _loggers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandler"/> class.
        /// </summary>
        /// <param name="exceptionProcessors">The collection of exception processors that will be used to log any unhandled exception.</param>
        public ExceptionHandler(params ExceptionProcessor[] exceptionProcessors)
        {
            _loggers = exceptionProcessors ?? Array.Empty<ExceptionProcessor>();
        }

        /// <summary>
        /// Used when an unhandled exception occurs in an <see cref="AppDomain"/>.
        /// </summary>
        /// <param name="exception">The exception that was thrown.</param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We're doing exception handling here, we don't really want anything to escape.")]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        public void OnException(Exception exception)
        {
            // Something has gone really wrong here. We need to be very careful
            // when we try to deal with this exception because:
            // - We might be here due to assembly loading issues, so we can't load
            //   any code which is not in the current class or in one of the system
            //   assemblies (that is we assume any code in the GAC is available ...
            //   which obviously may be incorrect).
            // - We might be here because the CLR failed hard (e.g. OutOfMemoryException
            //   and friends). In this case we're toast. We'll try our normal approach
            //   but that will probably fail ...
            //
            // We don't want to throw an exception if we're handling unhandled exceptions ...
            foreach (var logger in _loggers)
            {
                try
                {
                    logger(exception);
                }
                catch (Exception)
                {
                    // Stuffed. Just give up.
                }
            }
        }
    }
}
