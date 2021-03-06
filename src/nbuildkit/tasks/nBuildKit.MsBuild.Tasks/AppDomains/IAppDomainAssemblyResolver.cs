//-----------------------------------------------------------------------
// <copyright company="TheNucleus">
// Copyright (c) TheNucleus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
                
using System;

namespace NBuildKit.MsBuild.Tasks.AppDomains
{
    /// <summary>
    /// Defines the interface for classes which deal with assembly resolution.
    /// </summary>
    internal interface IAppDomainAssemblyResolver
    {
        /// <summary>
        /// Attaches the assembly resolution method to the <see cref="AppDomain.AssemblyResolve"/>
        /// event.
        /// </summary>
        void Attach();
    }
}

