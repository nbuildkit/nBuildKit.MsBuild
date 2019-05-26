//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NBuildKit.MsBuild.Tasks.Properties;

namespace NBuildKit.MsBuild.Tasks.Validation
{
    internal sealed class FxCopInvocationProperties : IEquatable<FxCopInvocationProperties>
    {
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(FxCopInvocationProperties first, FxCopInvocationProperties second)
        {
            // Check if first is a null reference by using ReferenceEquals because
            // we overload the == operator. If first isn't actually null then
            // we get an infinite loop where we're constantly trying to compare to null.
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
            {
                return true;
            }

            var nonNullObject = first;
            var possibleNullObject = second;
            if (ReferenceEquals(first, null))
            {
                nonNullObject = second;
                possibleNullObject = first;
            }

            return nonNullObject.Equals(possibleNullObject);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(FxCopInvocationProperties first, FxCopInvocationProperties second)
        {
            // Check if first is a null reference by using ReferenceEquals because
            // we overload the == operator. If first isn't actually null then
            // we get an infinite loop where we're constantly trying to compare to null.
            if (ReferenceEquals(first, null) && ReferenceEquals(second, null))
            {
                return false;
            }

            var nonNullObject = first;
            var possibleNullObject = second;
            if (ReferenceEquals(first, null))
            {
                nonNullObject = second;
                possibleNullObject = first;
            }

            return !nonNullObject.Equals(possibleNullObject);
        }

        private readonly string _customDictionaryPath;

        private readonly string _ruleSetPath;

        private readonly string _targetFramework;

        /// <summary>
        /// Initializes a new instance of the <see cref="FxCopInvocationProperties"/> class.
        /// </summary>
        /// <param name="targetFramework">The target framework.</param>
        /// <param name="ruleSetPath">The path to the rule set file.</param>
        /// <param name="customDictionaryPath">The path to the custom dictionary XML file.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="targetFramework"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="targetFramework"/> is an empty string.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="ruleSetPath"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="ruleSetPath"/> is an empty string.
        /// </exception>
        public FxCopInvocationProperties(string targetFramework, string ruleSetPath, string customDictionaryPath)
        {
            if (targetFramework == null)
            {
                throw new ArgumentNullException("targetFramework");
            }

            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                throw new ArgumentException(Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString, "targetFramework");
            }

            if (ruleSetPath == null)
            {
                throw new ArgumentNullException("ruleSetPath");
            }

            if (string.IsNullOrWhiteSpace(ruleSetPath))
            {
                throw new ArgumentException(Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString, "ruleSetPath");
            }

            _customDictionaryPath = customDictionaryPath ?? string.Empty;
            _ruleSetPath = ruleSetPath;
            _targetFramework = targetFramework;
        }

        /// <summary>
        /// Gets the path to the custom dictionary XML file.
        /// </summary>
        public string CustomDictionaryFilePath => _customDictionaryPath;

        /// <summary>
        /// Gets the path to the rule set file.
        /// </summary>
        public string RuleSetFilePath => _ruleSetPath;

        /// <summary>
        /// Gets the target framework.
        /// </summary>
        public string TargetFramework => _targetFramework;

        /// <summary>
        /// Determines whether the specified <see cref="FxCopInvocationProperties"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="FxCopInvocationProperties"/> to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true"/> if the specified <see cref="FxCopInvocationProperties"/> is equal to this instance;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.DocumentationRules",
            "SA1628:DocumentationTextMustBeginWithACapitalLetter",
            Justification = "Documentation can start with a language keyword")]
        public bool Equals(FxCopInvocationProperties other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Check if other is a null reference by using ReferenceEquals because
            // we overload the == operator. If other isn't actually null then
            // we get an infinite loop where we're constantly trying to compare to null.
            return !ReferenceEquals(other, null)
                && _customDictionaryPath.Equals(other.CustomDictionaryFilePath)
                && _ruleSetPath.Equals(other.RuleSetFilePath)
                && _targetFramework.Equals(other.TargetFramework);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///     <see langword="true"/> if the specified <see cref="object"/> is equal to this instance; otherwise, <see langword="false"/>.
        /// </returns>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.DocumentationRules",
            "SA1628:DocumentationTextMustBeginWithACapitalLetter",
            Justification = "Documentation can start with a language keyword")]
        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            // Check if other is a null reference by using ReferenceEquals because
            // we overload the == operator. If other isn't actually null then
            // we get an infinite loop where we're constantly trying to compare to null.
            var id = obj as FxCopInvocationProperties;
            return !ReferenceEquals(id, null) && Equals(id);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public sealed override int GetHashCode()
        {
            // As obtained from the Jon Skeet answer to:
            // http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            // And adapted towards the Modified Bernstein (shown here: http://eternallyconfuzzled.com/tuts/algorithms/jsw_tut_hashing.aspx)
            //
            // Overflow is fine, just wrap
            unchecked
            {
                // Pick a random prime number
                int hash = 17;

                // Mash the hash together with yet another random prime number
                hash = (hash * 23) ^ _customDictionaryPath.GetHashCode();
                hash = (hash * 23) ^ _ruleSetPath.GetHashCode();
                hash = (hash * 23) ^ _targetFramework.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "FxCop Properties: [TargetFramework: {0}; RuleSet: {1}; Dictionary: {2}]",
                _targetFramework,
                _ruleSetPath,
                _customDictionaryPath);
        }
    }
}
