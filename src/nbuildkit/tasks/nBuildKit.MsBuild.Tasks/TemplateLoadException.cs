//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using NBuildKit.MsBuild.Tasks.Properties;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// An exception thrown when there is a failure to read a template file from the assembly.
    /// </summary>
    [Serializable]
    public sealed class TemplateLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateLoadException"/> class.
        /// </summary>
        public TemplateLoadException()
            : this(Resources.Exceptions_Messages_CouldNotLoadTemplate)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateLoadException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public TemplateLoadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateLoadException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public TemplateLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateLoadException"/> class.
        /// </summary>
        /// <param name="info">
        ///     The <see cref="SerializationInfo"/> that holds the serialized
        ///     object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        ///     The <see cref="StreamingContext"/> that contains contextual
        ///     information about the source or destination.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="info"/> parameter is null.
        /// </exception>
        /// <exception cref="SerializationException">
        /// The class name is null or <see cref="Exception.HResult"/> is zero (0).
        /// </exception>
        private TemplateLoadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
