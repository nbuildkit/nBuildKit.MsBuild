//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Code Analysis results, point to "Suppress Message", and click
// "In Suppression File".
// You do not need to add suppressions to this file manually.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Microsoft.Naming",
    "CA1709:IdentifiersShouldBeCasedCorrectly",
    MessageId = "n",
    Justification = "Actual product name is nBuildKit with a small n")]

[assembly: SuppressMessage(
    "Microsoft.StyleCop.CSharp.NamingRules",
    "SA1300:ElementMustBeginWithUpperCaseLetter",
    MessageId = "n",
    Scope = "namespace",
    Target = "nBuildKit.MsBuild.Tasks",
    Justification = "Keeping the correct capitalization.")]
