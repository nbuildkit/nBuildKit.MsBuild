//-----------------------------------------------------------------------
// <copyright company="nBuildKit">
// Copyright (c) nBuildKit. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;

namespace NBuildKit.MsBuild.Tasks
{
    /// <summary>
    /// Defines a <see cref="ITask"/> that reads a 'zipspec' file and creates the associated ZIP archive.
    /// </summary>
    public sealed class Zip : NBuildKitMsBuildTask
    {
        private void Compress(
            string outputFile,
            IDictionary<string, List<string>> files,
            bool overwriteExistingFile)
        {
            const int BufferSize = 64 * 1024;

            var buffer = new byte[BufferSize];
            var fileMode = overwriteExistingFile ? FileMode.Create : FileMode.CreateNew;

            using (var outputFileStream = new FileStream(outputFile, fileMode))
            {
                using (var archive = new ZipArchive(outputFileStream, ZipArchiveMode.Create))
                {
                    foreach (var pair in files)
                    {
                        var filePath = pair.Key;
                        var list = pair.Value;

                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            foreach (var relativePath in list)
                            {
                                Log.LogMessage(MessageImportance.Low, string.Format("Adding: {0}. Storing as: {1}", filePath, relativePath));
                                var archiveEntry = archive.CreateEntry(relativePath);

                                using (var zipStream = archiveEntry.Open())
                                {
                                    int bytesRead = -1;
                                    while ((bytesRead = fs.Read(buffer, 0, BufferSize)) > 0)
                                    {
                                        zipStream.Write(buffer, 0, bytesRead);
                                    }
                                }

                                fs.Position = 0;
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (File == null)
            {
                Log.LogError("No archive files to create!");
                return false;
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(GetAbsolutePath(File));
            var name = xmlDoc.SelectSingleNode("//archive/name/text()").InnerText;
            var outputFilePath = Path.Combine(GetAbsolutePath(OutputDirectory), string.Format("{0}.zip", name));

            var files = new Dictionary<string, List<string>>();
            var filesNode = xmlDoc.SelectSingleNode("//archive/files");
            foreach (XmlNode child in filesNode.ChildNodes)
            {
                var excludedFiles = new List<string>();
                var excludedAttribute = child.Attributes["exclude"];
                var excluded = (excludedAttribute != null ? excludedAttribute.Value : string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var exclude in excluded)
                {
                    var pathSections = exclude.Split(new[] { "**" }, StringSplitOptions.RemoveEmptyEntries);

                    var directory = pathSections.Length == 1 ? Path.GetDirectoryName(pathSections[0]) : pathSections[0].Trim('\\');
                    var fileFilter = pathSections.Length == 1 ? Path.GetFileName(pathSections[0]) : pathSections[pathSections.Length - 1].Trim('\\');
                    var recurse = pathSections.Length > 1;
                    var filesToExclude = GetFilteredFilePaths(directory, fileFilter, recurse).Select(f => f.FullName);
                    excludedFiles.AddRange(filesToExclude);
                }

                var targetAttribute = child.Attributes["target"];
                var target = targetAttribute != null ? targetAttribute.Value : string.Empty;

                var sourceAttribute = child.Attributes["src"];
                var sources = sourceAttribute.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var source in sources)
                {
                    var pathSections = source.Split(new[] { "**" }, StringSplitOptions.RemoveEmptyEntries);

                    var directory = pathSections.Length == 1 ? Path.GetDirectoryName(pathSections[0]) : pathSections[0].Trim('\\');
                    var fileFilter = pathSections.Length == 1 ? Path.GetFileName(pathSections[0]) : pathSections[pathSections.Length - 1].Trim('\\');
                    var recurse = pathSections.Length > 1;
                    var filesToInclude = GetFilteredFilePaths(directory, fileFilter, recurse)
                        .Where(f => !excludedFiles.Contains(f.FullName))
                        .Select(f => f.FullName);

                    foreach (var file in filesToInclude)
                    {
                        var relativePath = Path.Combine(target, GetRelativePath(file, directory));
                        if (!files.ContainsKey(file))
                        {
                            files.Add(file, new List<string>());
                        }

                        var list = files[file];
                        list.Add(relativePath);
                    }
                }
            }

            Log.LogMessage(MessageImportance.Normal, string.Format("Creating archive at: {0}", outputFilePath));
            Compress(outputFilePath, files, OverwriteExistingFiles);

            // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
            // from a task's constructor or property setter. As long as this task is written to always log an error
            // when it fails, we can reliably return HasLoggedErrors.
            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Gets or sets the full path to the zipspec file.
        /// </summary>
        [Required]
        public ITaskItem File
        {
            get;
            set;
        }

        private IEnumerable<FileInfo> GetFilteredFilePaths(string baseDirectory, string fileFilter, bool recurse)
        {
            var dirInfo = new DirectoryInfo(baseDirectory);
            return dirInfo.EnumerateFiles(fileFilter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Gets or sets the full path to the output directory.
        /// </summary>
        [Required]
        public ITaskItem OutputDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether any existing files should be overwritten.
        /// </summary>
        public bool OverwriteExistingFiles
        {
            get;
            set;
        }
    }
}
