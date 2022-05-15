using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                List<string> customFileList = RemoveDefaultFilesFromResourceList(results.ResGenFileList.ToList(), ExclusionFilePath);

                // Add .bsp since it is not included in .res file
                customFileList.Add($"maps/{bspName}");

                // Create an associated file list
                List<AssociatedFile> associatedFiles = PopulateAssociatedFileList(customFileList);

                // Only remove the .bsp from the end of the string in case .bsp exists elsewhere in the map name
                var mapNameWithoutExtension = bspName.Remove(bspName.LastIndexOf(".bsp", StringComparison.InvariantCultureIgnoreCase), bspName.Length);

                // Add files that aren't in the .res file, but are common
                AddOptionalFiles(mapNameWithoutExtension, associatedFiles);

                // Find the files on the local system. Search in all GameDirectories, but prioritize earlier directories
                FindFiles(GameDirectories, associatedFiles);
                
                //TODO: Check associatedFiles for accuracy

                //TODO:
                // Create ZIP and save to zipOutputDirectory
            }

            //TODO: Create an object to return with details about the process. Let the calling app output a log
            //TODO: include associated files and if ZIP creation was succesful and ZIP path
            return String.Empty;
        }


        /// <remarks>Code examples for running a process:
        /// https://stackoverflow.com/questions/1469764/run-command-prompt-commands
        /// </remarks>
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

        private List<string> RemoveDefaultFilesFromResourceList(List<string> resGenFileList, string pathToExcludeList)
        {
            var excludedFileList = File.ReadAllLines(pathToExcludeList).ToList();
            resGenFileList.Sort();
            //excludedFileList.Sort(); //TODO: Add a config option for assume sorted and skip or run this step accordingly

            return resGenFileList.Except(excludedFileList).ToList();
        }

        private List<AssociatedFile> PopulateAssociatedFileList(List<string> customFileList)
        {
            var associatedFiles = new List<AssociatedFile>();

            foreach (var file in customFileList)
            {
                if (file.EndsWith(".bsp", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".wad", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".mdl", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".spr", StringComparison.InvariantCultureIgnoreCase))
                {
                    associatedFiles.Add(new AssociatedFile() { FileName = file, FileImportance = FileImportance.Required });
                }
                else
                {
                    // The map will load without the .tga, .wav, .txt, etc... files, but they are an important otherwise the map
                    // will look and sound incomplete
                    associatedFiles.Add(new AssociatedFile() { FileName = file, FileImportance = FileImportance.Important });
                }
            }

            return associatedFiles;
        }

        private void AddOptionalFiles(string mapNameWithoutExtension, List<AssociatedFile> associatedFiles)
        {
            associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}.txt", FileImportance = FileImportance.Optional });
            associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}_detail.txt", FileImportance = FileImportance.Optional });
            associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}.res", FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"{mapNameWithoutExtension}.cfg", FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"overviews/{mapNameWithoutExtension}.bmp", FileImportance = FileImportance.Optional });
            associatedFiles.Add(new AssociatedFile() { FileName = $"overviews/{mapNameWithoutExtension}.txt", FileImportance = FileImportance.Optional });
            associatedFiles.Add(new AssociatedFile() { FileName = $"sturmbot/waypoints/{mapNameWithoutExtension}.wpt", FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"shrikebot/waypoints/{mapNameWithoutExtension}.wps", FileImportance = FileImportance.Extra });
        }

        private void FindFiles(string[] gameDirectories, List<AssociatedFile> associatedFiles)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            foreach (var gameDirectory in gameDirectories)
            {
                foreach (var file in associatedFiles)
                {
                    if (isWindows)
                    {
                        file.FileName.Replace("/", @"\");
                    }

                    string fullPathToFile = Path.Combine(gameDirectory, file.FileName);
                    var fileExistsInGameDirectory = File.Exists(fullPathToFile);

                    if (file.Exists)
                    {
                        FileInfo currentfileInfo = new FileInfo(file.LocalFilePath);
                        FileInfo newlyFoundfileInfo = new FileInfo(fullPathToFile);

                        // check checksum or file size
                        //TODO: add an option to check checksum OR file size. one is more accurate and one is faster
                        if (currentfileInfo.Length != newlyFoundfileInfo.Length)
                        {
                            file.DuplicateWithDifferencesFound = true;
                        }
                    }
                    else if (fileExistsInGameDirectory)
                    {
                        file.LocalFilePath = fullPathToFile;
                        file.Exists = true;
                    }
                    else
                    {
                        file.Exists = false;
                    }

                }
            }
        }
    }
}
