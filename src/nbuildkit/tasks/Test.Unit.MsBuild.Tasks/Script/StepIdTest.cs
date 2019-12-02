//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Nuclei.Nunit.Extensions;
using NUnit.Framework;

namespace NBuildKit.MsBuild.Tasks.Script
{
    [TestFixture]
    [SuppressMessage(
        "Microsoft.StyleCop.CSharp.DocumentationRules",
        "SA1600:ElementsMustBeDocumented",
        Justification = "Unit tests do not need documentation.")]
    public sealed class StepIdTest : EqualityContractVerifierTest
    {
        private readonly StepIdHashcodeContractVerfier _hashCodeVerifier = new StepIdHashcodeContractVerfier();

        private readonly StepIdEqualityContractVerifier _equalityVerifier = new StepIdEqualityContractVerifier();

        protected override HashCodeContractVerifier HashContract
        {
            get
            {
                return _hashCodeVerifier;
            }
        }

        protected override IEqualityContractVerifier EqualityContract
        {
            get
            {
                return _equalityVerifier;
            }
        }

        [Test]
        public void LargerThanOperatorWithFirstObjectNull()
        {
            StepId first = null;
            StepId second = new StepId("a");

            Assert.IsFalse(first > second);
        }

        [Test]
        public void LargerThanOperatorWithSecondObjectNull()
        {
            StepId first = new StepId(typeof(string).FullName);
            StepId second = null;

            Assert.IsTrue(first > second);
        }

        [Test]
        public void LargerThanOperatorWithBothObjectsNull()
        {
            StepId first = null;
            StepId second = null;

            Assert.IsFalse(first > second);
        }

        [Test]
        public void LargerThanOperatorWithEqualObjects()
        {
            var first = new StepId(typeof(string).FullName);
            var second = first.Clone();

            Assert.IsFalse(first > second);
        }

        [Test]
        public void LargerThanOperatorWithFirstObjectLarger()
        {
            var first = new StepId("b");
            var second = new StepId("a");

            Assert.IsTrue(first > second);
        }

        [Test]
        public void LargerThanOperatorWithFirstObjectSmaller()
        {
            var first = new StepId("a");
            var second = new StepId("b");

            Assert.IsFalse(first > second);
        }

        [Test]
        public void SmallerThanOperatorWithFirstObjectNull()
        {
            StepId first = null;
            StepId second = new StepId("a");

            Assert.IsTrue(first < second);
        }

        [Test]
        public void SmallerThanOperatorWithSecondObjectNull()
        {
            StepId first = new StepId("a");
            StepId second = null;

            Assert.IsFalse(first < second);
        }

        [Test]
        public void SmallerThanOperatorWithBothObjectsNull()
        {
            StepId first = null;
            StepId second = null;

            Assert.IsFalse(first < second);
        }

        [Test]
        public void SmallerThanOperatorWithEqualObjects()
        {
            var first = new StepId("a");
            var second = first.Clone();

            Assert.IsFalse(first < second);
        }

        [Test]
        public void SmallerThanOperatorWithFirstObjectLarger()
        {
            var first = new StepId("b");
            var second = new StepId("a");

            Assert.IsFalse(first < second);
        }

        [Test]
        public void SmallerThanOperatorWithFirstObjectSmaller()
        {
            var first = new StepId("a");
            var second = new StepId("b");

            Assert.IsTrue(first < second);
        }

        [Test]
        public void Clone()
        {
            StepId first = new StepId("a");
            StepId second = first.Clone();

            Assert.AreEqual(first, second);
        }

        [Test]
        public void CompareToWithNullObject()
        {
            StepId first = new StepId("a");
            object second = null;

            Assert.AreEqual(1, first.CompareTo(second));
        }

        [Test]
        public void CompareToOperatorWithEqualObjects()
        {
            var first = new StepId("a");
            object second = first.Clone();

            Assert.AreEqual(0, first.CompareTo(second));
        }

        [Test]
        public void CompareToWithLargerFirstObject()
        {
            var first = new StepId("b");
            var second = new StepId("a");

            Assert.IsTrue(first.CompareTo(second) > 0);
        }

        [Test]
        public void CompareToWithSmallerFirstObject()
        {
            var first = new StepId("a");
            var second = new StepId("b");

            Assert.IsTrue(first.CompareTo(second) < 0);
        }

        [Test]
        public void CompareToWithUnequalObjectTypes()
        {
            StepId first = new StepId("a");
            object second = new object();

            Assert.Throws<ArgumentException>(() => first.CompareTo(second));
        }

        private sealed class StepIdEqualityContractVerifier : EqualityContractVerifier<StepId>
        {
            private readonly StepId _first = new StepId("a");

            private readonly StepId _second = new StepId("b");

            protected override StepId Copy(StepId original)
            {
                return original.Clone();
            }

            protected override StepId FirstInstance
            {
                get
                {
                    return _first;
                }
            }

            protected override StepId SecondInstance
            {
                get
                {
                    return _second;
                }
            }

            protected override bool HasOperatorOverloads
            {
                get
                {
                    return true;
                }
            }
        }

        private sealed class StepIdHashcodeContractVerfier : HashCodeContractVerifier
        {
            private readonly IEnumerable<StepId> _distinctInstances
                = new List<StepId>
                     {
                        new StepId("a"),
                        new StepId("b"),
                        new StepId("c"),
                        new StepId("d"),
                        new StepId("e"),
                        new StepId("f"),
                        new StepId("g"),
                        new StepId("h"),
                        new StepId("i"),
                        new StepId("j"),
                     };

            protected override IEnumerable<int> GetHashCodes()
            {
                return _distinctInstances.Select(i => i.GetHashCode());
            }
        }
    }
}
