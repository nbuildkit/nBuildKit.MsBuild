//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// This class implements static methods to assist with unescaping of %XX codes
    /// in the MSBuild file format.
    /// </summary>
    /// <remarks>
    /// Taken from here: https://github.com/Microsoft/msbuild/blob/master/src/Shared/EscapingUtilities.cs
    /// because it is internal so we can't get to it.
    /// </remarks>
    internal static class EscapingUtilities
    {
        /// <summary>
        /// Special characters that need escaping.
        /// It's VERY important that the percent character is the FIRST on the list - since it's both a character
        /// we escape and use in escape sequences, we can unintentionally escape other escape sequences if we
        /// don't process it first. Of course we'll have a similar problem if we ever decide to escape hex digits
        /// (that would require rewriting the algorithm) but since it seems unlikely that we ever do, this should
        /// be good enough to avoid complicating the algorithm at this point.
        /// </summary>
        private static readonly char[] _charsToEscape = { '%', '*', '?', '@', '$', '(', ')', ';', '\'' };

        /// <summary>
        /// Optional cache of escaped strings for use when needing to escape in performance-critical scenarios with significant
        /// expected string reuse.
        /// </summary>
        private static Dictionary<string, string> _unescapedToEscapedStrings = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Adds instances of %XX in the input string where the char char to be escaped appears
        /// XX is the hex value of the ASCII code for the char.
        /// </summary>
        /// <param name="unescapedString">The string to escape.</param>
        /// <returns>escaped string</returns>
        internal static string Escape(string unescapedString)
        {
            return EscapeWithOptionalCaching(unescapedString, cache: false);
        }

        /// <summary>
        /// Adds instances of %XX in the input string where the char char to be escaped appears
        /// XX is the hex value of the ASCII code for the char.  Caches if requested.
        /// </summary>
        /// <param name="unescapedString">The string to escape.</param>
        /// <param name="cache">
        /// True if the cache should be checked, and if the resultant string
        /// should be cached.
        /// </param>
        /// <returns>The escaped string.</returns>
        private static string EscapeWithOptionalCaching(string unescapedString, bool cache)
        {
            // If there are no special chars, just return the original string immediately.
            // Don't even instantiate the StringBuilder.
            if (string.IsNullOrEmpty(unescapedString) || !ContainsReservedCharacters(unescapedString))
            {
                return unescapedString;
            }

            // next, if we're caching, check to see if it's already there.
            if (cache)
            {
                string cachedEscapedString = null;
                lock (_unescapedToEscapedStrings)
                {
                    if (_unescapedToEscapedStrings.TryGetValue(unescapedString, out cachedEscapedString))
                    {
                        return cachedEscapedString;
                    }
                }
            }

            // This is where we're going to build up the final string to return to the caller.
            StringBuilder escapedStringBuilder = new StringBuilder(unescapedString.Length * 2);

            AppendEscapedString(escapedStringBuilder, unescapedString);

            if (!cache)
            {
                return escapedStringBuilder.ToString();
            }

            string escapedString = escapedStringBuilder.ToString();
            lock (_unescapedToEscapedStrings)
            {
                _unescapedToEscapedStrings[unescapedString] = escapedString;
            }

            return escapedString;
        }

        /// <summary>
        /// Before trying to actually escape the string, it can be useful to call this method to determine
        /// if escaping is necessary at all.  This can save lots of calls to copy around item metadata
        /// that is really the same whether escaped or not.
        /// </summary>
        /// <param name="unescapedString">The unescaped string</param>
        /// <returns>true if the string contains any of the characters that need to be escaped, otherwise false.</returns>
        private static bool ContainsReservedCharacters(string unescapedString)
        {
            return unescapedString.IndexOfAny(_charsToEscape) != -1;
        }

        /// <summary>
        /// Convert the given integer into its hexadecimal representation.
        /// </summary>
        /// <param name="x">The number to convert, which must be non-negative and less than 16</param>
        /// <returns>The character which is the hexadecimal representation of <paramref name="x"/>.</returns>
        private static char HexDigitChar(int x)
        {
            return (char)(x + (x < 10 ? '0' : ('a' - 10)));
        }

        /// <summary>
        /// Append the escaped version of the given character to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to which to append.</param>
        /// <param name="character">The character to escape.</param>
        private static void AppendEscapedChar(StringBuilder builder, char character)
        {
            // Append the escaped version which is a percent sign followed by two hexadecimal digits
            builder.Append('%');
            builder.Append(HexDigitChar(character / 0x10));
            builder.Append(HexDigitChar(character & 0x0F));
        }

        /// <summary>
        /// Append the escaped version of the given string to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to which to append.</param>
        /// <param name="unescapedString">The unescaped string.</param>
        private static void AppendEscapedString(StringBuilder sb, string unescapedString)
        {
            // Replace each unescaped special character with an escape sequence one
            for (int idx = 0; ;)
            {
                int nextIdx = unescapedString.IndexOfAny(_charsToEscape, idx);
                if (nextIdx == -1)
                {
                    sb.Append(unescapedString, idx, unescapedString.Length - idx);
                    break;
                }

                sb.Append(unescapedString, idx, nextIdx - idx);
                AppendEscapedChar(sb, unescapedString[nextIdx]);
                idx = nextIdx + 1;
            }
        }
    }
}
