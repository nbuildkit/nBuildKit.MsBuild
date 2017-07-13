//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using Nuclei;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines the ID of a executable step.
    /// </summary>
    internal sealed class StepId : Id<StepId, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StepId"/> class.
        /// </summary>
        /// <param name="value">The ID of the step.</param>
        public StepId(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Performs the actual act of creating a copy of the current ID number.
        /// </summary>
        /// <param name="value">The internally stored value.</param>
        /// <returns>
        /// A copy of the current ID number.
        /// </returns>
        protected override StepId Clone(string value)
        {
            return new StepId(value);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return InternalValue;
        }
    }
}
