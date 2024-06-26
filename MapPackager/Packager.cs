﻿using Ionic.Zip;
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
    /// Used to package all required files for a map into a ZIP file
    /// </summary>
    public class Packager
    {
        //TODO: Have this passed in and/or create defaults based on game and have a set path to search for this
        private const string ExclusionFilePath = @"C:\Users\Bill\source\repos\MapAutoPackager\MapPackager\Excluded File Lists\excluded file list - dod.txt";
        private const string ValveExclusionFilePath = @"C:\Users\Bill\source\repos\MapAutoPackager\MapPackager\Excluded File Lists\excluded file list - valve.txt";

        //TODO: move these to a configuration file
        // -g: outputs file list to the console
        // -o: overwrites any existing .res file
        // -f: specifically the map file
        private const string ResGenParameters = " -gok -f \"{0}\"";
        private const string ResGenExecutableFilename = "RESGen.exe";
        private const string StartOfFileListToken = "Creating";
        private const string EndOfFileListSuccessToken = "Done creating res file(s)!";

        // Error messages
        private const string BspNotFound = "BSP file not found in the given location(s)";
        private const string ResGenCreateMessageNotFound = "Never found message saying RESGen.exe started creating the .res file";
        private const string ProcessDidNotStart = "Process Did not Start Succesfully. process.Start() returned false";
        private const string NeverFoundResGenEndMessage = "Never found end message from RESGen.exe saying .res file was done being created.";

        public string[] GameDirectories { get; set; }
        public string ZipOutputDirectory { get; set; }
        public string PathToResGenExecutable { get; set; }
        public bool PutMapsWithErrorsInSubFolders { get; set; }

        /// <summary>
        /// Packages a map file into a ZIP file
        /// </summary>
        /// <param name="bspName">The filename of the map to package</param>
        /// <returns></returns>
        public MapPackageResult Package(string bspName)
        {
            var mapPackageResult = new MapPackageResult();

            // Only remove the .bsp from the end of the string in case .bsp exists elsewhere in the map name
            var indexOfFileExtension = bspName.LastIndexOf(".bsp", StringComparison.InvariantCultureIgnoreCase);
            var mapNameWithoutExtension = bspName.Remove(indexOfFileExtension, bspName.Length - indexOfFileExtension);
            mapPackageResult.MapName = mapNameWithoutExtension;

            // Generate Resource File
            //TODO: Decompress/unpackage the BSP file and generate our own .res file and list of required/optional files without using ResGen
            var resourceFileGenerationResults = GenerateResourceFile(bspName);

            if (resourceFileGenerationResults.SuccessfullyGenerated)
            {
                // Remove entries that are default files that come with the game
                List<string> customFileList = RemoveDefaultFilesFromResourceList(resourceFileGenerationResults.ResGenFileList.ToList(), ExclusionFilePath);

                // Add .bsp since it is not included in .res file
                customFileList.Add($"maps/{bspName}");

                // Create an associated file list
                List<AssociatedFile> associatedFiles = PopulateAssociatedFileList(customFileList);

                // Add files that aren't in the .res file, but are common
                AddOptionalFiles(mapNameWithoutExtension, associatedFiles);

                // Check for folders that contain files exclusively associated with the map
                AddFilesInAssociatedFolders(GameDirectories, mapNameWithoutExtension, associatedFiles);

                // Find the files on the local system. Search in all GameDirectories, but prioritize earlier directories
                FindFiles(GameDirectories, associatedFiles);

                // Create ZIP and save to zipOutputDirectory
                CreateZipFile(associatedFiles, mapNameWithoutExtension, mapPackageResult);
            }
            else
            {
                mapPackageResult.ZipCreationSuccesful = false;
                mapPackageResult.ErrorMessage = resourceFileGenerationResults.ErrorMessage;
            }

            return mapPackageResult;
        }

        /// <remarks>Code examples for running a process:
        /// https://stackoverflow.com/questions/1469764/run-command-prompt-commands
        /// </remarks>
        private ResourceFileResult GenerateResourceFile(string bspName)
        {
            ResourceFileResult resourceFileResult = new ResourceFileResult();
            string mapsFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"maps\" : @"maps/";
            
            string bspPath = null;
            foreach (var gameDirectory in GameDirectories)
            {
                var bspPathToSearch = Path.Combine(gameDirectory, mapsFolder + bspName);
                if (File.Exists(bspPathToSearch))
                {
                    bspPath = bspPathToSearch;
                }
            }
             
            if (String.IsNullOrEmpty(bspPath))
            {
                resourceFileResult.ErrorMessage = BspNotFound;
                resourceFileResult.SuccessfullyGenerated = false;
                return resourceFileResult;
            }

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
                resourceFileResult.ResGenFileList = resourceList.ToString().Trim().ToLowerInvariant().Split(Environment.NewLine);

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
            var excludedFileList = File.ReadAllLines(pathToExcludeList).Select(s => s.ToLowerInvariant()).ToList();
            var valveExcludedFileList = File.ReadAllLines(ValveExclusionFilePath).Select(s => s.ToLowerInvariant()).ToList();

            // Some queries around the files in the Valve folder for debugging issues
            //var filesNotInValve = resGenFileList.Where(t2 => !valveExcludedFileList.Any(t1 => t2.Contains(t1)));
            //var filesInValveThatExistInMap = resGenFileList.Except(filesNotInValve).ToList();
            //var filesInValveThatExistInMapThatArentInDod = resGenFileList.Except(excludedFileList).Where(f => !f.EndsWith(".wad")).ToList();

            excludedFileList.AddRange(valveExcludedFileList);

            resGenFileList.Sort();
            excludedFileList.Sort();

            return resGenFileList.Except(excludedFileList).ToList();
        }

        private List<AssociatedFile> PopulateAssociatedFileList(List<string> customFileList)
        {
            var associatedFiles = new List<AssociatedFile>();

            foreach (var file in customFileList)
            {
                //TODO: Decompress/unpackage WAD files and compare texture list to those used within the BSP to see if the WAD is truly required
                if (file.EndsWith(".bsp", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".wad", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".mdl", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".spr", StringComparison.InvariantCultureIgnoreCase))
                {
                    associatedFiles.Add(new AssociatedFile() { FileName = file, RelativePath = Path.GetDirectoryName(file), FileImportance = FileImportance.Required });
                }
                else
                {
                    // Add all other files in the resource list as "Important". The map will load without the .tga, .wav, etc... files, but they are
                    // an important otherwise the map will look and sound incomplete.
                    associatedFiles.Add(new AssociatedFile() { FileName = file, RelativePath = Path.GetDirectoryName(file), FileImportance = FileImportance.Important });
                }
            }

            return associatedFiles;
        }

        private void AddOptionalFiles(string mapNameWithoutExtension, List<AssociatedFile> associatedFiles)
        {
            if (!associatedFiles.Any(f => f.FileName == $"maps/{mapNameWithoutExtension}.txt")) associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}.txt", RelativePath = "maps", FileImportance = FileImportance.Optional });
            if (!associatedFiles.Any(f => f.FileName == $"maps/{mapNameWithoutExtension}_detail.txt")) associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}_detail.txt", RelativePath = "maps", FileImportance = FileImportance.Optional });
            if (!associatedFiles.Any(f => f.FileName == $"overviews/{mapNameWithoutExtension}.bmp")) associatedFiles.Add(new AssociatedFile() { FileName = $"overviews/{mapNameWithoutExtension}.bmp", RelativePath = "overviews", FileImportance = FileImportance.Important });
            if (!associatedFiles.Any(f => f.FileName == $"overviews/{mapNameWithoutExtension}.txt")) associatedFiles.Add(new AssociatedFile() { FileName = $"overviews/{mapNameWithoutExtension}.txt", RelativePath = "overviews", FileImportance = FileImportance.Important });
            associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}_readme.txt", RelativePath = "maps", FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"{mapNameWithoutExtension}_readme.txt", RelativePath = String.Empty, FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"maps/{mapNameWithoutExtension}.res", RelativePath = "maps", FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"{mapNameWithoutExtension}.cfg", RelativePath = String.Empty, FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"sturmbot/waypoints/{mapNameWithoutExtension}.wpt", RelativePath = "sturmbot/waypoints", FileImportance = FileImportance.Extra });
            associatedFiles.Add(new AssociatedFile() { FileName = $"shrikebot/waypoints/{mapNameWithoutExtension}.wps", RelativePath = "shrikebot/waypoints", FileImportance = FileImportance.Extra });
        }

        private void FindFiles(string[] gameDirectories, List<AssociatedFile> associatedFiles)
        {
            foreach (var gameDirectory in gameDirectories)
            {
                foreach (var file in associatedFiles)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        file.FileName = file.FileName.Replace("/", @"\");
                        file.RelativePath = file.RelativePath.Replace("/", @"\");
                    }

                    string fullPathToFile = Path.Combine(gameDirectory, file.FileName);
                    var fileExistsInGameDirectory = File.Exists(fullPathToFile);

                    if (file.Exists && fileExistsInGameDirectory)
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
                    else if (!file.Exists)
                    {
                        if (fileExistsInGameDirectory)
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

        private void AddFilesInAssociatedFolders(string[] gameDirectories, string mapNameWithoutExtension, List<AssociatedFile> associatedFiles)
        {
            // At the moment /sprites/obj_icons/{mapNameWithoutExtension}/ is the only folder I'm aware of associated with a map
            // If there are more then we can create a list of folders to search for and loop through them here
            // We'd also need to find out if there can be subfolders within these associated folders or not
            var folderToSearchFor = $"sprites/obj_icons/{mapNameWithoutExtension}/";

            foreach (var gameDirectory in gameDirectories)
            {
                var folderToSearchForLocalizedToOs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? folderToSearchFor.Replace("/", @"\") : folderToSearchFor;
                var fullPathToFolder = gameDirectory + folderToSearchForLocalizedToOs;
                if (Directory.Exists(fullPathToFolder))
                {
                    // Assuming no subfolders. If there are subfolders we'll have to recurse through those as well.
                    string[] allSpritesToInclude = Directory.GetFiles(fullPathToFolder, "*.spr");

                    foreach (var spritePath in allSpritesToInclude)
                    {
                        //TODO: Check inside the BSP to see if these sprites are actually used. For now assume they are all required
                        var relativeSpritePath = folderToSearchFor + Path.GetFileName(spritePath);
                        if (!associatedFiles.Any(f => f.FileName == relativeSpritePath))
                        {
                            associatedFiles.Add(new AssociatedFile() { FileName = relativeSpritePath, RelativePath = folderToSearchFor.TrimEnd('/'), FileImportance = FileImportance.Required });
                        }                           
                    }
                }
            }
        }

        private void CreateZipFile(List<AssociatedFile> associatedFiles, string mapNameWithoutExtension, MapPackageResult mapPackageResult)
        {
            mapPackageResult.AssociatedFiles = associatedFiles;
            bool requiredFilesAreMissing = associatedFiles.Any(f => f.Exists == false && f.FileImportance == FileImportance.Required);
            if (requiredFilesAreMissing)
            {
                mapPackageResult.ZipCreationSuccesful = false;
                mapPackageResult.ErrorMessage = "Could not locate all required files in the given game directories";
            }

            string missingFileTypeByPriority = PutMapsWithErrorsInSubFolders ? GetMissingFileType(associatedFiles) : String.Empty;

            // If necessary create the subfolder if it does not exist
            string baseZipPath = Path.Combine(ZipOutputDirectory, missingFileTypeByPriority);
            if (!String.IsNullOrEmpty(missingFileTypeByPriority) && !File.Exists(baseZipPath))
            {
                Directory.CreateDirectory(baseZipPath);
            }

            string zipFile = Path.Combine(baseZipPath, mapNameWithoutExtension + ".zip");
            if (File.Exists(zipFile))
            {
                mapPackageResult.ZipCreationSuccesful = false;
                mapPackageResult.ErrorMessage = "ZIP file already exists. Please delete it first if you wish to create a new one.";
                return;
            }

            try
            {
                // Using DotNetZip to do the archiving: https://github.com/haf/DotNetZip.Semverd
                using (ZipFile zip = new ZipFile())
                {
                    foreach (var file in associatedFiles.Where(af => af.Exists))
                    {
                        zip.AddFile(file.LocalFilePath, file.RelativePath);
                    }
                    
                    if (String.IsNullOrEmpty(mapPackageResult.ErrorMessage))
                    {
                        mapPackageResult.ZipCreationSuccesful = true;
                        mapPackageResult.ZipFilePath = zipFile;
                        zip.Save(zipFile);
                    }
                    else
                    {
                        var indexOfFileExtension = zipFile.LastIndexOf(".zip", StringComparison.InvariantCultureIgnoreCase);
                        //TODO: Add an option to sort ZIPs into folder or by filename based on error type
                        var errorZipPath = zipFile.Remove(indexOfFileExtension, zipFile.Length - indexOfFileExtension) + " - ERROR Needs " + missingFileTypeByPriority + ".zip";
                        mapPackageResult.ZipFilePath = errorZipPath;
                        zip.Save(errorZipPath);
                    }
                }
            }
            catch (Exception ex)
            {
                mapPackageResult.ZipCreationSuccesful = false;
                mapPackageResult.ErrorMessage = ex.Message;
                return;
            }  
        }

        private string GetMissingFileType(List<AssociatedFile> associatedFiles)
        {
            // Priotize models and sprites first as they are required for the map to load and play properly
            if (associatedFiles.Any(file => file.FileName.EndsWith(".mdl") && !file.Exists)) return "Models";
            if (associatedFiles.Any(file => file.FileName.EndsWith(".spr") && !file.Exists)) return "Sprites";

            // Priotize Sounds and Backgrounds second because they are important to map experience, but not required for it to load
            if (associatedFiles.Any(file => file.FileName.EndsWith(".wav") && !file.Exists)) return "Sounds";
            if (associatedFiles.Any(file => file.FileName.EndsWith(".tga") && !file.Exists)) return "Backgrounds";

            // Priotize Overviews third, because while playable without them it takes from the overall experience
            if (associatedFiles.Any(file => file.FileName.Contains("overviews/") && !file.Exists)) return "Overviews";

            // Priotize WADs last because there are a lot of WADs listed in BSPs that aren't used causing false positives
            if (associatedFiles.Any(file => file.FileName.EndsWith(".wad") && !file.Exists)) return "Textures";

            // There are no missing files of importance
            return String.Empty;
        }
    }
}
