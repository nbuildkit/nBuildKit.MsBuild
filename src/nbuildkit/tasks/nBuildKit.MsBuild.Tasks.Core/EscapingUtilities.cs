//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NBuildKit.MsBuild.Tasks.Core
{
    /// <summary>
    /// This class implements static methods to assist with unescaping of %XX codes
    /// in the MSBuild file format.
    /// </summary>
    /// <remarks>
    /// Taken from here: https://github.com/Microsoft/msbuild/blob/master/src/Shared/EscapingUtilities.cs
    /// because it is internal so we can't get to it.
    /// </remarks>
    public static class EscapingUtilities
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
        /// Adds instances of %XX in the input string where the char to be escaped appears
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
        /// Convert the given integer into its hexadecimal representation.
        /// </summary>
        /// <param name="x">The number to convert, which must be non-negative and less than 16</param>
        /// <returns>The character which is the hexadecimal representation of <paramref name="x"/>.</returns>
        private static char HexDigitChar(int x)
        {
            return (char)(x + (x < 10 ? '0' : ('a' - 10)));
        }

        private static bool IsHexDigit(char character)
        {
            return ((character >= '0') && (character <= '9'))
                || ((character >= 'A') && (character <= 'F'))
                || ((character >= 'a') && (character <= 'f'));
        }

        /// <summary>
        /// Replaces all instances of %XX in the input string with the character represented
        /// by the hexadecimal number XX.
        /// </summary>
        /// <param name="escapedString">The string to unescape.</param>
        /// <returns>unescaped string</returns>
        internal static string UnescapeAll(string escapedString)
        {
            bool throwAwayBool;
            return UnescapeAll(escapedString, out throwAwayBool);
        }

        /// <summary>
        /// Replaces all instances of %XX in the input string with the character represented
        /// by the hexadecimal number XX.
        /// </summary>
        /// <param name="escapedString">The string to unescape.</param>
        /// <param name="escapingWasNecessary">Whether any replacements were made.</param>
        /// <returns>unescaped string</returns>
        internal static string UnescapeAll(string escapedString, out bool escapingWasNecessary)
        {
            escapingWasNecessary = false;

            // If the string doesn't contain anything, then by definition it doesn't
            // need unescaping.
            if (string.IsNullOrEmpty(escapedString))
            {
                return escapedString;
            }

            // If there are no percent signs, just return the original string immediately.
            // Don't even instantiate the StringBuilder.
            int indexOfPercent = escapedString.IndexOf('%');
            if (indexOfPercent == -1)
            {
                return escapedString;
            }

            // This is where we're going to build up the final string to return to the caller.
            StringBuilder unescapedString = new StringBuilder(escapedString.Length);

            int currentPosition = 0;

            // Loop until there are no more percent signs in the input string.
            while (indexOfPercent != -1)
            {
                // There must be two hex characters following the percent sign
                // for us to even consider doing anything with this.
                if ((indexOfPercent <= (escapedString.Length - 3)) && IsHexDigit(escapedString[indexOfPercent + 1]) && IsHexDigit(escapedString[indexOfPercent + 2]))
                {
                    // First copy all the characters up to the current percent sign into
                    // the destination.
                    unescapedString.Append(escapedString, currentPosition, indexOfPercent - currentPosition);

                    // Convert the %XX to an actual real character.
                    string hexString = escapedString.Substring(indexOfPercent + 1, 2);
                    char unescapedCharacter = (char)int.Parse(
                        hexString,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture);

                    // if the unescaped character is not on the exception list, append it
                    unescapedString.Append(unescapedCharacter);

                    // Advance the current pointer to reflect the fact that the destination string
                    // is up to date with everything up to and including this escape code we just found.
                    currentPosition = indexOfPercent + 3;

                    escapingWasNecessary = true;
                }

                // Find the next percent sign.
                indexOfPercent = escapedString.IndexOf('%', indexOfPercent + 1);
            }

            // Okay, there are no more percent signs in the input string, so just copy the remaining
            // characters into the destination.
            unescapedString.Append(escapedString, currentPosition, escapedString.Length - currentPosition);

            return unescapedString.ToString();
        }
    }
}
