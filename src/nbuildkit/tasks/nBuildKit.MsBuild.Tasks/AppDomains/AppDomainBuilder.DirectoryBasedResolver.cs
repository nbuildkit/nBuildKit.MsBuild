//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NBuildKit.MsBuild.Tasks.AppDomains
{
    /// <content>
    /// Contains the definition of the <see cref="DirectoryBasedResolver"/> class.
    /// </content>
    internal static partial class AppDomainBuilder
    {
        /// <summary>
        /// Attaches a method to the <see cref="AppDomain.AssemblyResolve"/> event and provides
        /// assembly resolution based on the files available in a set of predefined directories.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1812:Avoid uninstantiated internal classes",
            Justification = "Used by instantiation via type.")]
        private sealed class DirectoryBasedResolver : MarshalByRefObject, IAppDomainAssemblyResolver
        {
            /// <summary>
            /// Stores the directories as a collection of directory paths.
            /// </summary>
            /// <design>
            /// Explicitly store the directory paths in strings because DirectoryInfo objects are eventually
            /// nuked because DirectoryInfo is a MarshalByRefObject and can thus go out of scope.
            /// </design>
            private IEnumerable<string> _directories;

            /// <summary>
            /// Stores the paths to the relevant directories.
            /// </summary>
            /// <param name="directoryPaths">The paths to the relevant directories.</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="directoryPaths"/> is <see langword="null"/>.
            /// </exception>
            public void StoreDirectoryPaths(IEnumerable<string> directoryPaths)
            {
                if (directoryPaths == null)
                {
                    throw new ArgumentNullException(nameof(directoryPaths));
                }

                _directories = directoryPaths;
            }

            /// <summary>
            /// Attaches the assembly resolution method to the <see cref="AppDomain.AssemblyResolve"/>
            /// event of the current <see cref="AppDomain"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// Thrown when <see cref="DirectoryBasedResolver.StoreDirectoryPaths"/> has not been called prior to
            /// attaching the directory resolver to an <see cref="AppDomain"/>.
            /// </exception>
            public void Attach()
            {
                var domain = AppDomain.CurrentDomain;
                {
                    var helper = new FusionHelper(
                        () => _directories.SelectMany(
                            dir => Directory.GetFiles(
                                dir,
                                "*.dll",
                                SearchOption.AllDirectories)));
                    domain.AssemblyResolve += helper.LocateAssemblyOnAssemblyLoadFailure;
                }
            }
        }
    }
}
