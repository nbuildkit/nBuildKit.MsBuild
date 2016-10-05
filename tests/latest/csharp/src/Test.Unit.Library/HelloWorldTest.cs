using NBuildKit.Test.CSharp.Library;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Reflection;

namespace Test.Unit.Library
{
    [TestFixture]
    public class HelloWorldTest
    {
        private string AssemblyName(Assembly assembly)
        {
            var attribute = (AssemblyTitleAttribute)assembly.GetCustomAttribute(typeof(AssemblyTitleAttribute));
            return attribute.Title;
        }

        private string AssemblyVersion(Assembly assembly)
        {
            var attribute = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));
            return attribute.InformationalVersion;
        }

        [Test]
        public void SayHello()
        {
            var helloWorld = new HelloWorld();
            var text = helloWorld.SayHello();
            var expected = string.Format(
                CultureInfo.InvariantCulture,
                "Hello world from: {0} [{1}]",
                AssemblyName(typeof(HelloWorld).Assembly),
                AssemblyVersion(typeof(HelloWorld).Assembly));
            Assert.AreEqual(expected, text);
        }
    }
}
