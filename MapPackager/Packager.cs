using System;
using System.Diagnostics;
using System.IO;

namespace MapPackager
{
    /// <summary>
    /// 
    /// </summary>
    public class Packager
    {
        public string[] GameDirectories { get; set; }
        public string ZipOutputDirectory { get; set; }
        public string PathToResGenExecutable { get; set; }

        // -g: outputs file list to the console
        // -o: overwrites any existing .res file
        // -f: specifically the map file
        private const string ResGenParameters = " -go -f \"{0}\"";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bspName"></param>
        /// <returns></returns>
        public string Package(string bspName)
        {
            //TODO:
            // Generate Resource File
            // Parse resource file and load resources into memory
            // Remove entries that are default files that come with the game
            // Store this as a text file, so that it can be changed (maybe even used with other games)
            // use Directory Printer application to create this list
            // Add optional entries for things such .cfg and overview files
            // Search through provided folder structures using the order as priority and generate a list of which files exist and which don't
            // Create ZIP and save to zipOutputDirectory
            GenerateResourceFile(Path.Combine(GameDirectories[0], bspName));
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
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    // The original host website of RESGen.exe no longer appears to be up as of May 5, 2022, but I 
                    // did find the source code for it on GitHub:
                    // https://github.com/kriswema/resgen
                    FileName = Path.Combine(PathToResGenExecutable, "RESGen.exe"),
                    // -k skips the need to press enter to close the program
                    // Add double quotes around file path otherwise a space in the folder path will fail
                    Arguments = String.Format(ResGenParameters, bspPath), 
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    CreateNoWindow = false,
                    WorkingDirectory = PathToResGenExecutable
                }
            };

            ResourceFileResult resourceFileResult = new ResourceFileResult();
            try
            {
                resourceFileResult.SuccessfullyGenerated = proc.Start();
                //TODO: find a way to capture output. add a verbose option?
                //StandardError
                //StandardOutput
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                resourceFileResult.SuccessfullyGenerated = false;
            }

            return resourceFileResult;
        }
    }
}
