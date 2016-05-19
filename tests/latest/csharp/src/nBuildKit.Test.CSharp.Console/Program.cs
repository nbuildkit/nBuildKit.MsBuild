using NBuildKit.Test.CSharp.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBuildKit.Test.CSharp.Console
{
    /// <summary>
    /// The entry point for the application.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The entry method for the application.
        /// </summary>
        static void Main()
        {
            var helloWorld = new HelloWorld();
            System.Console.WriteLine(helloWorld.SayHello());
        }
    }
}
