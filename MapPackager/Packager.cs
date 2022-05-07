using System;
using System.Diagnostics;
using System.IO;

namespace MapPackager
{
    public static class Packager
    {
        public static string Package(string bspName, string gameDirectory, string zipOutputDirectory)
        {
            //TODO:
            // Generate Resource File
            // Parse resource file and load resources into memory
            // Remove entries that are default files that come with the game
            // Store this as a text file, so that it can be changed (maybe even used with other games)
            // use Directory Printer application to create this list
            // Add optional entries for things such .cfg and overview files
            // Search through provided folder structure and generate a list of which files exist and which don't
            // Create ZIP and save to zipOutputDirectory

            return String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Code examples for running a process:
        /// https://stackoverflow.com/questions/1469764/run-command-prompt-commands
        /// </remarks>
        /// <param name="pathToBsp"></param>
        /// <returns></returns>
        private static ResourceFileResult GenerateResourceFile(string pathToBsp)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    // The original host website of RESGen.exe no longer appears to be up as of May 5, 2022, but I 
                    // did find the source code for it on GitHub:
                    // https://github.com/kriswema/resgen
                    FileName = @".\ResGen\RESGen.exe",
                    Arguments = String.Join("-k -f ", pathToBsp), //-k skips the need to press enter to close the program
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = @".\ResGen\"
                }
            };

            ResourceFileResult resourceFileResult = new ResourceFileResult();
            try
            {
                resourceFileResult.SuccessfullyGenerated = proc.Start();
            }
            catch
            {
                resourceFileResult.SuccessfullyGenerated = false;
            }

            return resourceFileResult;
        }
    }
}
