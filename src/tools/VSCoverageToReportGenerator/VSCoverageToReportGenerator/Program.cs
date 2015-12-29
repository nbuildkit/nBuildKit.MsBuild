//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
//     Copyright 2013 nBuildKit. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Coverage.Analysis;
using System.IO;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Mono.Options;
using System.Globalization;
using nBuildKit.Tools.VSCoverageToReportGenerator.Properties;
using Nuclei;
using System.Diagnostics;
using System.Xml.Linq;

namespace nBuildKit.Tools.VSCoverageToReportGenerator
{
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1400:AccessModifierMustBeDeclared",
        Justification = "Access modifiers should not be declared on the entry point for a command line application. See FxCop.")]
    static class Program
    {
        /// <summary>
        /// The exit code used when the application has shown the help information.
        /// </summary>
        private const int HelpShownExitCode = -1;

        /// <summary>
        /// Defines the error code for a normal application exit (i.e without errors).
        /// </summary>
        private const int NormalApplicationExitCode = 0;

        /// <summary>
        /// Defines the error code for an application exit with an unhandled exception.
        /// </summary>
        private const int UnhandledExceptionApplicationExitCode = 1;

        /// <summary>
        /// The exit code used when the application has been provided with one or more invalid
        /// command line parameters.
        /// </summary>
        private const int InvalidCommandLineParametersExitCode = 2;

        /// <summary>
        /// The exit code used when the application has been provided with the path to an
        /// input file that does not exist.
        /// </summary>
        private const int InvalidInputFileExitCode = 4;

        /// <summary>
        /// The exit code used when the application has been provided with the path to the
        /// Visual Studio directory that does not exist.
        /// </summary>
        private const int InvalidVisualStudioDirectoryExitCode = 5;

        /// <summary>
        /// The collection that contains the full paths to the directories which contain the binaries that were 
        /// used when running the tests.
        /// </summary>
        private static readonly List<string> s_BinDirectories = new List<string>();

        /// <summary>
        /// A flag indicating if the help information for the application should be displayed.
        /// </summary>
        private static bool s_ShouldDisplayHelp;

        /// <summary>
        /// The full path to the file that contains the VS code coverage data.
        /// </summary>
        private static string s_InputFile;

        /// <summary>
        /// The full path to the file to which the converted coverage data should be written.
        /// </summary>
        private static string s_OutputFile;

        /// <summary>
        /// The full path to the Visual Studio directory for the version that was used to execute the code coverage.
        /// </summary>
        private static string s_VisualStudioDirectory;

        static int Main(string[] args)
        {
            try
            {
                ShowHeader();

                // Parse the command line options
                var options = CreateOptionSet();
                try
                {
                    options.Parse(args);
                }
                catch (OptionException e)
                {
                    WriteErrorToConsole(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Output_Error_InvalidInputParameters_WithException,
                                e));

                    return InvalidCommandLineParametersExitCode;
                }

                if (s_ShouldDisplayHelp)
                {
                    ShowHelp(options);
                    return HelpShownExitCode;
                }

                if (string.IsNullOrWhiteSpace(s_InputFile))
                {
                    WriteErrorToConsole(Resources.Output_Error_MissingValues_InputFile);
                    ShowHelp(options);
                    return InvalidCommandLineParametersExitCode;
                }

                if (string.IsNullOrWhiteSpace(s_OutputFile))
                {
                    WriteErrorToConsole(Resources.Output_Error_MissingValues_OutputFile);
                    ShowHelp(options);
                    return InvalidCommandLineParametersExitCode;
                }

                if (string.IsNullOrWhiteSpace(s_VisualStudioDirectory))
                {
                    WriteErrorToConsole(Resources.Output_Error_MissingValues_VisualStudioDirectory);
                    ShowHelp(options);
                    return InvalidCommandLineParametersExitCode;
                }

                if (s_BinDirectories.Count == 0)
                {
                    WriteErrorToConsole(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.Output_Error_MissingValues_NoBinDirectorySpecified,
                            s_InputFile));
                    return InvalidCommandLineParametersExitCode;
                }

                if (!File.Exists(s_InputFile))
                {
                    WriteErrorToConsole(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.Output_Error_InputFileDoesNotExist_WithFile,
                            s_InputFile));
                    return InvalidInputFileExitCode;
                }

                foreach (var binDirectory in s_BinDirectories)
                {
                    if (!Directory.Exists(binDirectory))
                    {
                        WriteErrorToConsole(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Output_Error_BinDirectoryDoesNotExist_WithDirectory,
                                binDirectory));
                        return InvalidVisualStudioDirectoryExitCode;
                    }
                }

                if (!Directory.Exists(s_VisualStudioDirectory))
                {
                    WriteErrorToConsole(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.Output_Error_VisualStudioDirectoryDoesNotExist_WithDirectory,
                            s_VisualStudioDirectory));
                    return InvalidVisualStudioDirectoryExitCode;
                }

                var searchPath = Path.Combine(s_VisualStudioDirectory, @"Common7\IDE\PrivateAssemblies");
                SetupAdditionalAssemblySearchPaths(searchPath);
                
                CopySymbolsFile(searchPath);
                ConvertCoverageFile(s_InputFile, s_OutputFile, s_BinDirectories.ToArray());
            }
            catch (Exception)
            {
                return UnhandledExceptionApplicationExitCode;
            }

            return NormalApplicationExitCode;
        }

        private static void ShowHeader()
        {
            System.Console.WriteLine(Resources.Header_ApplicationAndVersion, GetVersion());
            System.Console.WriteLine(GetCopyright());
            System.Console.WriteLine(GetLibraryLicenses());
        }

        private static void ShowHelp(OptionSet argProcessor)
        {
            System.Console.WriteLine(Resources.Help_Usage_Intro);
            System.Console.WriteLine();
            argProcessor.WriteOptionDescriptions(System.Console.Out);
        }

        private static void WriteErrorToConsole(string errorText)
        {
            try
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(errorText);
            }
            finally
            {
                System.Console.ResetColor();
            }
        }

        private static void WriteToConsole(string text)
        {
            System.Console.WriteLine(text);
        }

        private static string GetVersion()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            Debug.Assert(attribute.Length == 1, "There should be a copyright attribute.");

            return (attribute[0] as AssemblyFileVersionAttribute).Version;
        }

        private static string GetCopyright()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(attribute.Length == 1, "There should be a copyright attribute.");

            return (attribute[0] as AssemblyCopyrightAttribute).Copyright;
        }

        private static string GetLibraryLicenses()
        {
            var licenseXml = EmbeddedResourceExtracter.LoadEmbeddedStream(
                Assembly.GetExecutingAssembly(),
                @"nBuildKit.Tools.VSCoverageToReportGenerator.Properties.licenses.xml");
            var doc = XDocument.Load(licenseXml);
            var licenses = from element in doc.Descendants("package")
                           select new
                           {
                               Id = element.Element("id").Value,
                               Version = element.Element("version").Value,
                               Source = (element.Element("url").FirstNode as XCData).Value,
                               License = (element.Element("licenseurl").FirstNode as XCData).Value,
                           };

            var builder = new StringBuilder();
            builder.AppendLine(Resources.Header_OtherPackages_Intro);
            foreach (var license in licenses)
            {
                builder.AppendLine(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Header_OtherPackages_IdAndLicense,
                        license.Id,
                        license.Version,
                        license.Source));
            }

            return builder.ToString();
        }

        private static OptionSet CreateOptionSet()
        {
            var options = new OptionSet 
                {
                    { 
                        Resources.CommandLine_Options_Help_Key, 
                        Resources.CommandLine_Options_Help_Description, 
                        v => s_ShouldDisplayHelp = v != null
                    },
                    {
                        Resources.CommandLine_Param_InputFile_Key,
                        Resources.CommandLine_Param_InputFile_Description,
                        v => s_InputFile = v
                    },
                    {
                        Resources.CommandLine_Param_OutputFile_Key,
                        Resources.CommandLine_Param_OutputFile_Description,
                        v => s_OutputFile = v
                    },
                    {
                        Resources.CommandLine_Param_BinDirectory_Key,
                        Resources.CommandLine_Param_BinDirectory_Description,
                        v => s_BinDirectories.Add(v)
                    },
                    {
                        Resources.CommandLine_Param_VisualStudioDirectory_Key,
                        Resources.CommandLine_Param_VisualStudioDirectory_Description,
                        v => s_VisualStudioDirectory = v
                    },
                };
            return options;
        }


        [SuppressMessage(
            "Microsoft.Reliability", 
            "CA2001:AvoidCallingProblematicMethods", 
            MessageId = "System.Reflection.Assembly.LoadFrom",
            Justification = "Need to load the assembly manually because it normally will not be found.")]
        private static void SetupAdditionalAssemblySearchPaths(string searchPath)
        {
            // The appbase for the MsBuild assembly load is the MsBuild folder (logically) which means
            // the .NET assembly loader won't look in the VS directory for the assembly it needs, eventhough
            // the compiler had no issues searching there.
            //
            // So we'll have to do this the nasty way ...
            AppDomain.CurrentDomain.AssemblyResolve +=
                (sender, e) =>
                {
                    WriteToConsole(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "Entering custom assembly resolve function. Searching for {0}",
                            e.Name));

                    var assemblySearchPath = System.IO.Path.Combine(
                        searchPath,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}.dll",
                            e.Name.Split(',')[0]));
                    if (File.Exists(assemblySearchPath))
                    {
                        WriteToConsole(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                "Found {0} at {1}",
                                e.Name,
                                assemblySearchPath));
                        return Assembly.LoadFrom(assemblySearchPath);
                    }

                    WriteToConsole(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "Did not find {0} at {1}",
                            e.Name,
                            assemblySearchPath));

                    return null;
                };
        }

        private static void CopySymbolsFile(string searchPath)
        {
            var appDirectory = Assembly.GetExecutingAssembly().LocalDirectoryPath();
            var symbolsAssemblyDefaultPath = Path.Combine(searchPath, @"Microsoft.VisualStudio.Coverage.Symbols.dll");

            var symbolsAssemblyLocalPath = Path.Combine(appDirectory, @"Microsoft.VisualStudio.Coverage.Symbols.dll");
            if (!System.IO.File.Exists(symbolsAssemblyLocalPath))
            {
                WriteToConsole(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Copying {0} to {1}",
                        symbolsAssemblyDefaultPath,
                        symbolsAssemblyLocalPath));
                File.Copy(symbolsAssemblyDefaultPath, symbolsAssemblyLocalPath);
            }
        }

        private static void ConvertCoverageFile(string inputFile, string outputFile, string[] binDirectories)
        {
            WriteToConsole(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Converting VsTest coverage file {0} to {1}",
                        inputFile,
                        outputFile));
            using (CoverageInfo info = CoverageInfo.CreateFromFile(
                inputFile,
                binDirectories,
                new string[] { }))
            {
                CoverageDS data = info.BuildDataSet();
                data.WriteXml(outputFile);
            }
        }
    }
}