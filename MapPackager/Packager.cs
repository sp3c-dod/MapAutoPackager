using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MapPackager
{
    /// <summary>
    /// 
    /// </summary>
    public class Packager
    {
        //TODO: Have this passed in and/or create defaults based on game and have a set path to search for this
        private const string ExclusionFilePath = @"C:\Users\Bill\source\repos\MapAutoPackager\MapPackager\Excluded File Lists\excluded file list - dod.txt";

        //TODO: move these to a configuration file
        // -g: outputs file list to the console
        // -o: overwrites any existing .res file
        // -f: specifically the map file
        private const string ResGenParameters = " -gok -f \"{0}\"";
        private const string ResGenExecutableFilename = "RESGen.exe";
        private const string StartOfFileListToken = "Creating";
        private const string EndOfFileListSuccessToken = "Done creating res file(s)!";

        // Error messages
        private const string ResGenCreateMessageNotFound = "Never found message saying RESGen.exe started creating the .res file";
        private const string ProcessDidNotStart = "Process Did not Start Succesfully. process.Start() returned false";
        private const string NeverFoundResGenEndMessage = "Never found end message from RESGen.exe saying .res file was done being created.";

        public string[] GameDirectories { get; set; }
        public string ZipOutputDirectory { get; set; }
        public string PathToResGenExecutable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bspName"></param>
        /// <returns></returns>
        public string Package(string bspName)
        {
            // Generate Resource File
            var results = GenerateResourceFile(Path.Combine(GameDirectories[0], bspName));

            if (results.SuccessfullyGenerated)
            {
                // Remove entries that are default files that come with the game
                IEnumerable<string> customFileList = RemoveDefaultFilesFromResourceList(results.ResGenFileList.ToList(), ExclusionFilePath);

                //TODO:
                // Add optional entries for things such .cfg and overview files
                // Add .bsp is not included in .res file, so make sure that is added
                // Have a flag to store a text file with missing file or just output them to screen. Note which ones are optional
                // Search through provided folder structures using the order as priority and generate a list of which files exist and which don't

                //TODO:
                // Create ZIP and save to zipOutputDirectory
            }

            //TODO: Create an object to return with details about the process. Let the calling app output a log
            return String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Code examples for running a process:
        /// https://stackoverflow.com/questions/1469764/run-command-prompt-commands
        /// </remarks>
        /// <param name="bspPath"></param>
        /// <returns></returns>
        private ResourceFileResult GenerateResourceFile(string bspPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    // The original host website of RESGen.exe no longer appears to be up as of May 5, 2022, but I 
                    // did find the source code for it on GitHub:
                    // https://github.com/kriswema/resgen
                    FileName = Path.Combine(PathToResGenExecutable, ResGenExecutableFilename),
                    Arguments = String.Format(ResGenParameters, bspPath), 
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = PathToResGenExecutable
                }
            };

            ResourceFileResult resourceFileResult = new ResourceFileResult();
            StringBuilder rawResGenOutput = new StringBuilder();
            try
            {
                bool startedWithoutError = process.Start();

                if (!startedWithoutError)
                {
                    resourceFileResult.SuccessfullyGenerated = false;
                    resourceFileResult.ErrorMessage = ProcessDidNotStart;
                    return resourceFileResult;
                }

                StringBuilder resourceList = new StringBuilder();

                string currentLine;
                bool foundStartOfFileList = false;
                bool foundSuccessMessageAtEnd = false;
                while ((currentLine = process.StandardOutput.ReadLine()) != null)
                {
                    rawResGenOutput.AppendLine(currentLine);
                    if (!foundStartOfFileList && currentLine.StartsWith(StartOfFileListToken))
                    {
                        foundStartOfFileList = true;
                        continue;
                    }
                    else if (foundStartOfFileList && !String.IsNullOrWhiteSpace(currentLine))
                    {
                        if (!currentLine.StartsWith(EndOfFileListSuccessToken))
                        {
                            resourceList.AppendLine(currentLine.Trim());
                        }
                        else
                        {
                            foundSuccessMessageAtEnd = true;
                        }
                        
                    }
                }

                process.WaitForExit();
                resourceFileResult.RawResGenOutput = rawResGenOutput.ToString();

                // Trim off the find new line since each line was added with AppendLine including the last
                resourceFileResult.ResGenFileList = resourceList.ToString().Trim().Split(Environment.NewLine);

                if (!foundStartOfFileList)
                {
                    resourceFileResult.ErrorMessage = ResGenCreateMessageNotFound;
                    
                    resourceFileResult.SuccessfullyGenerated = false;
                }
                else if (!foundSuccessMessageAtEnd)
                {
                    resourceFileResult.ErrorMessage = NeverFoundResGenEndMessage;
                    resourceFileResult.SuccessfullyGenerated = false;
                }

                resourceFileResult.SuccessfullyGenerated = true;
            }
            catch(Exception ex)
            {
                resourceFileResult.ErrorMessage = ex.Message;
                resourceFileResult.RawResGenOutput = rawResGenOutput.ToString();
                resourceFileResult.SuccessfullyGenerated = false;
            }

            return resourceFileResult;
        }
        private IEnumerable<string> RemoveDefaultFilesFromResourceList(List<string> resGenFileList, string pathToExcludeList)
        {
            var excludedFileList = File.ReadAllLines(pathToExcludeList).ToList();
            resGenFileList.Sort();
            //excludedFileList.Sort(); //TODO: Add a config option for assume sorted and skip or run this step accordingly

            return resGenFileList.Except(excludedFileList);
        }
    }
}
