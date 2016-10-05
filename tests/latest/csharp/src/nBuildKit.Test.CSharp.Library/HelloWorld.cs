using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBuildKit.Test.CSharp.Library
{
    /// <summary>
    /// Provides methods to say hello to the world.
    /// </summary>
    public class HelloWorld
    {
        private readonly string _name = AssemblyName();
        private readonly string _version = AssemblyVersion();

        /// <summary>
        /// Says hello to the world.
        /// </summary>
        /// <returns>A string containing the message.</returns>
        public string SayHello()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Hello world from: {0} [{1}]",
                _name,
                _version);
        }

        private static string AssemblyName()
        {
            var attribute = (AssemblyTitleAttribute)Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyTitleAttribute));
            return attribute.Title;
        }

        private static string AssemblyVersion()
        {
            var attribute = (AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));
            return attribute.InformationalVersion;
        }
    }
}
