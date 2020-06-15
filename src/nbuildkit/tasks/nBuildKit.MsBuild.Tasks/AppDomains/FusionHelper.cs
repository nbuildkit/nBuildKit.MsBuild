//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;

namespace NBuildKit.MsBuild.Tasks.AppDomains
{
    /// <summary>
    /// Contains methods for assisting with the assembly loading process.
    /// </summary>
    /// <remarks>
    /// Note because these methods assist in the assembly loading process it
    /// is not possible to place this class in a separate assembly from the
    /// elements which need to provide assembly loading assistance.
    /// </remarks>
    /// <design>
    /// <para>
    /// The goal of the <c>FusionHelper</c> class is to provide a fallback for the
    /// assembly loading process. The <c>LocateAssemblyOnAssemblyLoadFailure</c> method
    /// is attached to the <c>AppDomain.AssemblyResolve</c> event.
    /// </para>
    /// <para>
    /// The <c>FusionHelper</c> searches through a set of directories or files for assembly files.
    /// The assembly files that are found are checked to see if they match with the requested
    /// assembly file.
    /// </para>
    /// <para>
    /// Note that this class is not threadsafe. This should however not be a problem because we
    /// provide the collections of directories and files before attaching the <c>FusionHelper</c>
    /// object to the <c>AppDomain.AssemblyResolve</c> event. Once attached the event will only
    /// be called from one thread.
    /// </para>
    /// </design>
    internal sealed class FusionHelper
    {
        /// <summary>
        /// The extension of an assembly.
        /// </summary>
        private const string AssemblyExtension = "dll";

        /// <summary>
        /// Extracts the value from a key value pair which is embedded in the assembly full name.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        /// The value part of the key-value pair.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        private static string ExtractValueFromKeyValuePair(string input)
        {
            Debug.Assert(!string.IsNullOrEmpty(input), "The input should not be empty.");

            return input
                .Substring(
                    input.IndexOf(AssemblyNameElements.KeyValueSeparator, StringComparison.OrdinalIgnoreCase)
                    + AssemblyNameElements.KeyValueSeparator.Length)
                .Trim();
        }

        /// <summary>
        /// Determines whether the assembly name fully qualified, i.e. contains the name, version, culture and public key.
        /// </summary>
        /// <param name="assemblyFullName">Full name of the assembly.</param>
        /// <returns>
        ///     <see langword="true"/> if the assembly name is a fully qualified assembly name; otherwise, <see langword="false"/>.
        /// </returns>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.DocumentationRules",
            "SA1628:DocumentationTextMustBeginWithACapitalLetter",
            Justification = "Documentation can start with a language keyword")]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        private static bool IsAssemblyNameFullyQualified(string assemblyFullName)
        {
            Debug.Assert(!string.IsNullOrEmpty(assemblyFullName), "The assembly full name should not be empty.");

            // Assume that assembly file paths do not normally have commas in them
            return assemblyFullName.Contains(",");
        }

        /// <summary>
        /// Determines if the file at the specified <paramref name="filePath"/> is the assembly that the loader is
        /// looking for.
        /// </summary>
        /// <param name="filePath">The absolute file path to the file which might be the desired assembly.</param>
        /// <param name="fileName">The file name and extension for the desired assembly.</param>
        /// <param name="version">The version for the desired assembly.</param>
        /// <param name="culture">The culture for the desired assembly.</param>
        /// <param name="publicKey">The public key token for the desired assembly.</param>
        /// <returns>
        ///     <see langword="true"/> if the filePath points to the desired assembly; otherwise <see langword="false"/>.
        /// </returns>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.DocumentationRules",
            "SA1628:DocumentationTextMustBeginWithACapitalLetter",
            Justification = "Documentation can start with a language keyword")]
        private static bool IsFileTheDesiredAssembly(string filePath, string fileName, string version, string culture, string publicKey)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath), "The assembly file path should not be empty.");
            if (!Path.GetFileName(filePath).Equals(fileName, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if ((!string.IsNullOrEmpty(version)) || (!string.IsNullOrEmpty(culture)) || (!string.IsNullOrEmpty(publicKey)))
            {
                AssemblyName assemblyName;
                try
                {
                    // Load the assembly name but without loading the assembly file into the AppDomain.
                    assemblyName = AssemblyName.GetAssemblyName(filePath);
                }
                catch (ArgumentException)
                {
                    // filePath is invalid, e.g. an assembly with an invalid culture.
                    return false;
                }
                catch (FileNotFoundException)
                {
                    // filePath doesn't point to a valid file or doesn't exist
                    return false;
                }
                catch (SecurityException)
                {
                    // The caller doesn't have discovery permission for the given path
                    return false;
                }
                catch (BadImageFormatException)
                {
                    // The file is not a valid assembly file
                    return false;
                }
                catch (FileLoadException)
                {
                    // the file was already loaded but with a different set of evidence
                    return false;
                }

                if (!string.IsNullOrEmpty(version))
                {
                    var expectedVersion = new Version(version);
                    if (!expectedVersion.Equals(assemblyName.Version))
                    {
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(culture))
                {
                    // The 'Neutral' culture is actually the invariant culture. This is the culture an
                    // assembly gets if no culture was specified so...
                    if (culture.Equals(AssemblyNameElements.InvariantCulture, StringComparison.OrdinalIgnoreCase))
                    {
                        culture = string.Empty;
                    }

                    var expectedCulture = new CultureInfo(culture);
                    if (!expectedCulture.Equals(assemblyName.CultureInfo))
                    {
                        return false;
                    }
                }

                if ((!string.IsNullOrEmpty(publicKey))
                    && (!publicKey.Equals(AssemblyNameElements.NullString, StringComparison.OrdinalIgnoreCase)))
                {
                    var actualPublicKeyToken = assemblyName.GetPublicKeyToken();
                    var str = actualPublicKeyToken.Aggregate(
                        string.Empty,
                        (current, value) => current + value.ToString("x2", CultureInfo.InvariantCulture));

                    return str.Equals(publicKey, StringComparison.OrdinalIgnoreCase);
                }
            }

            return true;
        }

        /// <summary>
        /// Turns the module name into a qualified file name by adding the default assembly extension.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>
        /// The expected name of the assembly file that contains the module.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        private static string MakeModuleNameQualifiedFileName(string moduleName)
        {
            Debug.Assert(!string.IsNullOrEmpty(moduleName), "The assembly file name should not be empty.");

            return (moduleName.IndexOf(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        ".{0}",
                        AssemblyExtension),
                    StringComparison.OrdinalIgnoreCase) < 0)
                ? string.Format(CultureInfo.InvariantCulture, "{0}.{1}", moduleName, AssemblyExtension)
                : moduleName;
        }

        /// <summary>
        /// The delegate which is used to return a file enumerator based on a specific directory.
        /// </summary>
        private readonly Func<IEnumerable<string>> _fileEnumerator;

        /// <summary>
        /// The delegate which is used to load an assembly from a specific file path.
        /// </summary>
        private Func<string, Assembly> _assemblyLoader;

        /// <summary>
        /// Initializes a new instance of the <see cref="FusionHelper"/> class.
        /// </summary>
        /// <param name="fileEnumerator">The enumerator which returns all the files that are potentially of interest.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="fileEnumerator"/> is <see langword="null" />.
        /// </exception>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        public FusionHelper(Func<IEnumerable<string>> fileEnumerator)
        {
            if (fileEnumerator == null)
            {
                throw new ArgumentNullException(nameof(fileEnumerator));
            }

            _fileEnumerator = fileEnumerator;
        }

        /// <summary>
        /// Sets the assembly loader which is used to load assemblies from a specific path.
        /// </summary>
        /// <todo>
        /// The assembly loader should also deal with NGEN-ed assemblies. This means that using
        /// Assembly.LoadFrom is not the best choice.
        /// </todo>
        [SuppressMessage(
            "Microsoft.Reliability",
            "CA2001:AvoidCallingProblematicMethods",
            MessageId = "System.Reflection.Assembly.LoadFrom",
            Justification = "The whole point of this method is to load assemblies which are not on the load path.")]
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        internal Func<string, Assembly> AssemblyLoader
        {
            private get
            {
                return _assemblyLoader ?? (_assemblyLoader = path => Assembly.LoadFrom(path));
            }

            set
            {
                _assemblyLoader = value;
            }
        }

        /// <summary>
        /// Gets the file enumerator which is used to enumerate the files in a specific directory.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        private Func<IEnumerable<string>> FileEnumerator
        {
            get
            {
                return _fileEnumerator;
            }
        }

        /// <summary>
        /// Tries to locate the assembly specified by the assembly name.
        /// </summary>
        /// <param name="assemblyFullName">Full name of the assembly.</param>
        /// <returns>
        /// The desired assembly if is is in the search path; otherwise, <see langword="null"/>.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        private Assembly LocateAssembly(string assemblyFullName)
        {
            Debug.Assert(assemblyFullName != null, "Expected a non-null assembly name string.");
            Debug.Assert(assemblyFullName.Length != 0, "Expected a non-empty assembly name string.");

            // @todo: We should be able to use an AssemblyName because we can just load it from a string.
            // It is not possible to use the AssemblyName class because that attempts to load the
            // assembly. Obviously we are currently trying to find the assembly.
            // So parse the actual assembly name from the name string
            //
            // First check if we have been passed a fully qualified name or only a module name
            string fileName = assemblyFullName;
            string version = string.Empty;
            string culture = string.Empty;
            string publicKey = string.Empty;
            if (IsAssemblyNameFullyQualified(assemblyFullName))
            {
                // Split the assembly name out into different parts. The name
                // normally consists of:
                // - File name
                // - Version
                // - Culture
                // - PublicKeyToken
                // e.g.: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                var nameSections = assemblyFullName.Split(',');
                Debug.Assert(nameSections.Length == 4, "There should be 4 sections in the assembly name.");

                fileName = nameSections[0].Trim();
                version = ExtractValueFromKeyValuePair(nameSections[1]);
                culture = ExtractValueFromKeyValuePair(nameSections[2]);
                publicKey = ExtractValueFromKeyValuePair(nameSections[3]);
            }

            // If the file name already has the '.dll' extension then we don't need to add that, otherwise we do
            fileName = MakeModuleNameQualifiedFileName(fileName);

            var files = FileEnumerator();
            var match = (from filePath in files
                         where IsFileTheDesiredAssembly(filePath, fileName, version, culture, publicKey)
                         select filePath)
                         .FirstOrDefault();

            if (match != null)
            {
                return AssemblyLoader(match);
            }

            return null;
        }

        /// <summary>
        /// An event handler which is invoked when the search for an assembly fails.
        /// </summary>
        /// <param name="sender">The object which raised the event.</param>
        /// <param name="args">
        ///     The <see cref="ResolveEventArgs"/> instance containing the event data.
        /// </param>
        /// <returns>
        ///     An assembly reference if the required assembly can be found; otherwise <see langword="null"/>.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "This class is embedded in an user assembly and called from there. Hence all methods are internal.")]
        public Assembly LocateAssemblyOnAssemblyLoadFailure(object sender, ResolveEventArgs args)
        {
            return LocateAssembly(args.Name);
        }
    }
}
